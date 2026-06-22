// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OfficeCli.Help;

/// <summary>
/// Flat, grep-friendly dump of every (format, element, property) row across the
/// schema corpus. One self-contained line per record so external tools like
/// grep / awk / fzf can match against the full record without context loss.
/// Two row tags: ELEM (element summary) and PROP (property detail).
///
/// Each PROP row carries name/type/ops/aliases/enum-values plus description
/// and first example, so semantic grep ("indent level", "force recalculation")
/// works against the same dump as name/alias grep.
///
/// Example:
///   docx paragraph     ELEM  ops:[asgqr]  paths:/body/p[@paraId=ID];/body/p[N]
///   docx paragraph     PROP  align        enum    ops:[asg]  values:left|center|...  aliases:alignment  one of values  ex:--prop align=center
/// </summary>
internal static class SchemaHelpFlatRenderer
{
    private static readonly string[] Verbs = { "add", "set", "get", "query", "remove" };

    /// <summary>
    /// Render the flat dump. When <paramref name="onlyFormat"/> is non-null,
    /// the dump is restricted to that single format (e.g. "docx") so callers
    /// can do `help <fmt> all | grep ...` without piping through `grep ^fmt `.
    /// The caller is responsible for passing a canonical format string.
    /// </summary>
    internal static string RenderAll(string? onlyFormat = null)
    {
        var sb = new StringBuilder();
        if (onlyFormat == null)
        {
            sb.AppendLine("# officecli help all — grep-friendly schema dump");
        }
        else
        {
            sb.AppendLine($"# officecli help {onlyFormat} all — grep-friendly schema dump (filtered to {onlyFormat})");
        }
        sb.AppendLine("# Columns: <format> <element> <ELEM|PROP> <name> <type> ops:[asgqr] <details> <description> ex:<example>");
        sb.AppendLine("# ops letters: a=add s=set g=get q=query r=remove (- = not supported)");
        sb.AppendLine("# Add/Set form: officecli <fmt> add <path> --type <element> --prop key=value [--prop ...]");
        sb.AppendLine("#   (the <element> token here is the value in column 2; the per-row ex:--prop ... shows one valid --prop for that row)");
            sb.AppendLine("# Machine-readable: append --jsonl for one JSON record per line (for jq / scripts).");
        // Tips below intentionally use the literal column tokens (PROP / ELEM)
        // so users can copy-paste them. The leading '#' makes them easy to
        // strip with `grep -v '^#'` if the self-match line is unwanted.
        sb.AppendLine("# Tips: grep '^docx paragraph'  |  grep '  PROP  '  |  grep align  |  grep aliases:alignment");
        sb.AppendLine();

        foreach (var format in SchemaHelpLoader.ListFormats())
        {
            if (onlyFormat != null && !string.Equals(format, onlyFormat, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var element in SchemaHelpLoader.ListElements(format))
            {
                JsonDocument doc;
                try { doc = SchemaHelpLoader.LoadSchema(format, element); }
                catch { continue; }

                using (doc)
                {
                    AppendElementRow(sb, format, element, doc);
                    AppendPropertyRows(sb, format, element, doc);
                }
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// NDJSON variant of <see cref="RenderAll"/>: one JSON object per line, no
    /// outer array, no envelope, no header comments. Each line is independently
    /// parseable so consumers can stream through `while read line; jq ...` or
    /// load straight into a JSONL-aware tool. Schema (per record):
    ///   {"format":...,"element":...,"kind":"ELEM","ops":"asgqr","paths":[...]}
    ///   {"format":...,"element":...,"kind":"PROP","name":...,"type":...,
    ///    "ops":"as-g-","values":[...],"aliases":[...],"description":...,"example":...}
    /// `ops` keeps the 5-char asgqr/- string from the text variant so consumers
    /// only have to learn one ops vocabulary across both renderers.
    /// </summary>
    internal static string RenderAllJsonl(string? onlyFormat = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BuildMetaRecord().ToJsonString(JsonlOptions));
        foreach (var record in EnumerateRecords(onlyFormat))
            sb.AppendLine(record.ToJsonString(JsonlOptions));
        return sb.ToString();
    }

    private static JsonObject BuildMetaRecord() => new()
    {
        ["kind"] = "meta",
        ["ops_legend"] = new JsonObject
        {
            ["a"] = "add",
            ["s"] = "set",
            ["g"] = "get",
            ["q"] = "query",
            ["r"] = "remove",
            ["-"] = "not supported",
        },
    };

    /// <summary>
    /// JSON-array variant: returns the same per-record schema as
    /// <see cref="RenderAllJsonl"/> but as a single JSON array so the output
    /// is one parseable document. Pair with OutputFormatter.WrapEnvelope to
    /// match the {success, data, warnings} envelope used by other --json
    /// commands. Use --jsonl when streaming is preferable; --json when one
    /// JSON.parse call is.
    /// </summary>
    internal static string RenderAllJsonArray(string? onlyFormat = null)
    {
        var arr = new JsonArray();
        foreach (var record in EnumerateRecords(onlyFormat))
            arr.Add((JsonNode)record);
        return arr.ToJsonString(JsonlOptions);
    }

    private static IEnumerable<JsonObject> EnumerateRecords(string? onlyFormat)
    {
        foreach (var format in SchemaHelpLoader.ListFormats())
        {
            if (onlyFormat != null && !string.Equals(format, onlyFormat, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var element in SchemaHelpLoader.ListElements(format))
            {
                JsonDocument doc;
                try { doc = SchemaHelpLoader.LoadSchema(format, element); }
                catch { continue; }

                using (doc)
                {
                    yield return BuildElementRecord(format, element, doc);
                    foreach (var prop in BuildPropertyRecords(format, element, doc))
                        yield return prop;
                }
            }
        }
    }

    private static readonly JsonSerializerOptions JsonlOptions = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static JsonObject BuildElementRecord(string format, string element, JsonDocument doc)
    {
        var root = doc.RootElement;
        var obj = new JsonObject
        {
            ["format"] = format,
            ["element"] = element,
            ["kind"] = "ELEM",
            ["ops"] = FormatOps(root),
        };

        var paths = CollectPaths(root);
        if (paths.Count > 0)
        {
            var arr = new JsonArray();
            foreach (var p in paths) arr.Add((JsonNode?)JsonValue.Create(p));
            obj["paths"] = arr;
        }
        return obj;
    }

    private static IEnumerable<JsonObject> BuildPropertyRecords(string format, string element, JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("properties", out var props)
            || props.ValueKind != JsonValueKind.Object) yield break;

        foreach (var prop in props.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Object) continue;

            var obj = new JsonObject
            {
                ["format"] = format,
                ["element"] = element,
                ["kind"] = "PROP",
                ["name"] = prop.Name,
                ["type"] = TryGetString(prop.Value, "type") ?? "any",
                ["ops"] = FormatOps(prop.Value),
            };

            if (prop.Value.TryGetProperty("values", out var values)
                && values.ValueKind == JsonValueKind.Array)
            {
                var arr = new JsonArray();
                foreach (var v in values.EnumerateArray())
                    if (v.ValueKind == JsonValueKind.String) arr.Add((JsonNode?)JsonValue.Create(v.GetString()));
                if (arr.Count > 0) obj["values"] = arr;
            }

            if (prop.Value.TryGetProperty("aliases", out var aliases)
                && aliases.ValueKind == JsonValueKind.Array)
            {
                var arr = new JsonArray();
                foreach (var a in aliases.EnumerateArray())
                    if (a.ValueKind == JsonValueKind.String) arr.Add((JsonNode?)JsonValue.Create(a.GetString()));
                if (arr.Count > 0) obj["aliases"] = arr;
            }

            var desc = TryGetString(prop.Value, "description")
                       ?? TryGetString(prop.Value, "readback");
            if (!string.IsNullOrEmpty(desc))
                obj["description"] = SingleLine(desc!, int.MaxValue);

            if (prop.Value.TryGetProperty("examples", out var examples)
                && examples.ValueKind == JsonValueKind.Array)
            {
                var first = examples.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.String)
                    obj["example"] = SingleLine(first.GetString()!, 80);
            }

            yield return obj;
        }
    }

    private static List<string> CollectPaths(JsonElement root)
    {
        var parts = new List<string>();
        if (root.TryGetProperty("paths", out var paths)
            && paths.ValueKind == JsonValueKind.Object)
        {
            foreach (var kind in new[] { "stable", "positional" })
            {
                if (paths.TryGetProperty(kind, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    foreach (var p in arr.EnumerateArray())
                        if (p.ValueKind == JsonValueKind.String) parts.Add(p.GetString()!);
            }
        }
        // Some elements (e.g. chart-axis) express their path form via
        // addressing.pathForm rather than paths.stable/positional. Surface it
        // alongside paths so consumers don't have to special-case the schema
        // shape.
        if (root.TryGetProperty("addressing", out var addressing)
            && addressing.ValueKind == JsonValueKind.Object
            && addressing.TryGetProperty("pathForm", out var pathForm)
            && pathForm.ValueKind == JsonValueKind.String)
        {
            var pf = pathForm.GetString();
            if (!string.IsNullOrEmpty(pf) && !parts.Contains(pf!)) parts.Add(pf!);
        }
        return parts;
    }

    private static void AppendElementRow(StringBuilder sb, string format, string element, JsonDocument doc)
    {
        var root = doc.RootElement;
        var ops = FormatOps(root);
        var paths = FormatPaths(root);

        // <format> <element-padded> ELEM ops:[...] paths:...
        sb.Append(format).Append(' ');
        sb.Append(PadRight(element, 16)).Append("  ELEM  ");
        sb.Append("ops:[").Append(ops).Append(']');
        if (!string.IsNullOrEmpty(paths))
            sb.Append("  paths:").Append(paths);
        sb.AppendLine();
    }

    private static void AppendPropertyRows(StringBuilder sb, string format, string element, JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("properties", out var props)
            || props.ValueKind != JsonValueKind.Object) return;

        foreach (var prop in props.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Object) continue;

            var name = prop.Name;
            var type = TryGetString(prop.Value, "type") ?? "any";
            var ops = FormatOps(prop.Value);

            sb.Append(format).Append(' ');
            sb.Append(PadRight(element, 16)).Append("  PROP  ");
            sb.Append(PadRight(name, 22)).Append(' ');
            sb.Append(PadRight(type, 8)).Append(' ');
            sb.Append("ops:[").Append(ops).Append(']');

            // type-specific detail
            if (string.Equals(type, "enum", StringComparison.OrdinalIgnoreCase)
                && prop.Value.TryGetProperty("values", out var values)
                && values.ValueKind == JsonValueKind.Array)
            {
                sb.Append("  values:");
                bool first = true;
                foreach (var v in values.EnumerateArray())
                {
                    if (!first) sb.Append('|');
                    sb.Append(v.GetString());
                    first = false;
                }
            }

            // aliases (a frequent search target — surface inline)
            if (prop.Value.TryGetProperty("aliases", out var aliases)
                && aliases.ValueKind == JsonValueKind.Array)
            {
                sb.Append("  aliases:");
                bool first = true;
                foreach (var a in aliases.EnumerateArray())
                {
                    if (!first) sb.Append(',');
                    sb.Append(a.GetString());
                    first = false;
                }
            }

            // description (truncated, single-line) or readback as fallback —
            // these are the targets of semantic grep ("indent level",
            // "force recalculation"), not just decoration.
            var desc = TryGetString(prop.Value, "description")
                       ?? TryGetString(prop.Value, "readback");
            if (!string.IsNullOrEmpty(desc))
            {
                sb.Append("  ");
                sb.Append(SingleLine(desc!, 120));
            }

            // first example
            if (prop.Value.TryGetProperty("examples", out var examples)
                && examples.ValueKind == JsonValueKind.Array)
            {
                var first = examples.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.String)
                {
                    sb.Append("  ex:");
                    sb.Append(SingleLine(first.GetString()!, 80));
                }
            }

            sb.AppendLine();
        }
    }

    private static string FormatOps(JsonElement scope)
    {
        // Supports either top-level "operations" object (element) or per-property
        // boolean flags named after the verbs (property).
        var sb = new StringBuilder(5);
        JsonElement opsObj = default;
        bool hasOpsObj = scope.ValueKind == JsonValueKind.Object
                         && scope.TryGetProperty("operations", out opsObj)
                         && opsObj.ValueKind == JsonValueKind.Object;

        foreach (var v in Verbs)
        {
            bool supported = false;
            if (hasOpsObj && opsObj.TryGetProperty(v, out var bv) && bv.ValueKind == JsonValueKind.True)
                supported = true;
            else if (!hasOpsObj && scope.TryGetProperty(v, out var pv) && pv.ValueKind == JsonValueKind.True)
                supported = true;
            sb.Append(supported ? v[0] : '-');
        }
        return sb.ToString();
    }

    private static string FormatPaths(JsonElement root)
    {
        var parts = CollectPaths(root);
        return string.Join(";", parts);
    }

    private static string? TryGetString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    private static string SingleLine(string s, int max)
    {
        var collapsed = s.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        while (collapsed.Contains("  ")) collapsed = collapsed.Replace("  ", " ");
        collapsed = collapsed.Trim();
        return collapsed.Length <= max ? collapsed : collapsed.Substring(0, max - 1) + "…";
    }

    private static string PadRight(string s, int width) =>
        s.Length >= width ? s : s + new string(' ', width - s.Length);
}

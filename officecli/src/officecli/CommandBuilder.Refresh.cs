// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.CommandLine;
using OfficeCli.Core;

namespace OfficeCli;

static partial class CommandBuilder
{
    private static Command BuildRefreshCommand(Option<bool> jsonOption)
    {
        var fileArg = new Argument<FileInfo>("file") { Description = "Office document path" };

        var cmd = new Command("refresh", "Recalculate derived field values (TOC page numbers, PAGE/NUMPAGES, cross-references). Word + Windows required for .docx.");
        cmd.Add(fileArg);
        cmd.Add(jsonOption);

        cmd.SetAction(result => { var json = result.GetValue(jsonOption); return SafeRun(() =>
        {
            var file = result.GetValue(fileArg)!;

            if (TryResident(file.FullName, req =>
            {
                req.Command = "refresh";
                req.Json = json;
            }, json) is { } rc) return rc;

            var ext = Path.GetExtension(file.FullName).ToLowerInvariant();
            if (ext != ".docx" && ext != ".docm")
                throw new CliException($"refresh currently only supports .docx files (got {ext}).")
                { Code = "unsupported_type" };

            bool ok = false;
            string backend = "";
            if (OperatingSystem.IsWindows())
            {
                ok = WordPdfBackend.RefreshFields(file.FullName);
                if (ok) backend = "word";
            }
            if (!ok)
            {
                ok = WordHtmlRefresh.RefreshViaHtml(file.FullName);
                if (ok) backend = "html";
            }
            if (!ok)
                throw new CliException("refresh failed (Word backend unavailable and HTML fallback failed — no headless browser found).")
                { Code = "refresh_failed" };

            var msg = $"Refreshed: {file.FullName} (backend: {backend})";
            if (backend == "html")
                Console.Error.WriteLine("Note: HTML fallback used. TOC page numbers reflect officecli's HTML pagination, which may differ from Word's layout.");
            if (json) Console.WriteLine(OutputFormatter.WrapEnvelopeText(msg));
            else Console.WriteLine(msg);
            return 0;
        }, json); });

        return cmd;
    }
}

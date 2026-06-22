// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace OfficeCli.Core;

/// <summary>
/// Represents a node in the document DOM tree.
/// This is the universal abstraction across Word/Excel/PowerPoint.
/// </summary>
public class DocumentNode
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    [JsonPropertyName("preview")]
    public string? Preview { get; set; }
    [JsonPropertyName("style")]
    public string? Style { get; set; }
    [JsonPropertyName("childCount")]
    public int ChildCount { get; set; }
    [JsonPropertyName("format")]
    public Dictionary<string, object?> Format { get; set; } = new();
    [JsonPropertyName("children")]
    public List<DocumentNode> Children { get; set; } = new();

    /// <summary>
    /// Internal round-trip metadata that intentionally does not surface in
    /// user-facing Format (CLI Get output, JSON envelopes). Used to carry
    /// verbatim OOXML fragments (e.g. axisTitle.pPr, catTitle.pPr, series-
    /// level spPr) between the chart Reader and the batch emitter without
    /// polluting the public DocumentNode shape. Consumers that need these
    /// values read from InternalFormat directly.
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, object?> InternalFormat { get; set; } = new();
}

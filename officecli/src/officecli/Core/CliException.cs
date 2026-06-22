// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

namespace OfficeCli.Core;

/// <summary>
/// Exception that carries structured error info for AI-friendly JSON output.
/// </summary>
public class CliException : Exception
{
    /// <summary>Suggested correction (e.g. correct property name).</summary>
    public string? Suggestion { get; init; }

    /// <summary>Help command the caller can run for more info.</summary>
    public string? Help { get; init; }

    /// <summary>Machine-readable error code (e.g. "not_found", "invalid_value", "unsupported_property").</summary>
    public string? Code { get; init; }

    /// <summary>Available valid values when the error is about an invalid choice.</summary>
    public string[]? ValidValues { get; init; }

    public CliException(string message) : base(message) { }

    public CliException(string message, Exception innerException) : base(message, innerException) { }
}

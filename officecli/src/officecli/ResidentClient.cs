// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;

namespace OfficeCli;

public static class ResidentClient
{
    /// <summary>
    /// Check if a resident is running for this file (without consuming a connection).
    /// Just tries to connect briefly.
    /// </summary>
    public static bool TryConnect(string filePath, out string pipeName)
    {
        pipeName = ResidentServer.GetPipeName(filePath);
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName + "-ping", PipeDirection.InOut);
            client.Connect(100); // 100ms timeout

            // Ping to verify it's the right file
            var pingRequest = new ResidentRequest { Command = "__ping__" };
            var json = System.Text.Json.JsonSerializer.Serialize(pingRequest, ResidentJsonContext.Default.ResidentRequest);
            PipeWriteLine(client, json);

            var responseLine = PipeReadLine(client);
            if (responseLine == null) return false;

            var response = System.Text.Json.JsonSerializer.Deserialize<ResidentResponse>(responseLine, ResidentJsonContext.Default.ResidentResponse);
            if (response == null) return false;

            // Stdout contains the file path when responding to ping
            if (string.IsNullOrEmpty(response.Stdout)) return false;
            var residentFilePath = Path.GetFullPath(response.Stdout);
            var requestedFilePath = Path.GetFullPath(filePath);
            return string.Equals(residentFilePath, requestedFilePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Send a command to the resident server in a single connection.
    /// Returns null if no resident is running or the file doesn't match.
    /// </summary>
    /// <param name="connectTimeoutMs">
    /// How long to wait for the server to accept the pipe connection. Default
    /// 100ms suits the "is a resident listening at all?" fast-fail path; when
    /// the caller has already confirmed the resident is alive (e.g. via
    /// <see cref="TryConnect"/>), pass a longer value (seconds) so the command
    /// waits for its turn in the serialized command queue instead of silently
    /// dropping under load.
    /// </param>
    public static ResidentResponse? TrySend(string filePath, ResidentRequest request, int maxRetries = 0, int connectTimeoutMs = 100)
    {
        var pipeName = ResidentServer.GetPipeName(filePath);
        var json = System.Text.Json.JsonSerializer.Serialize(request, ResidentJsonContext.Default.ResidentRequest);
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            try
            {
                // --- Connect phase: the ONLY safe place to retry. ---
                // A failed connect means the command was never delivered, so
                // re-attempting cannot double-apply a mutation the resident never
                // received. This is the busy / "wait for my turn in the serialized
                // command queue" path the retry policy exists for.
                try
                {
                    client.Connect(connectTimeoutMs);
                }
                catch
                {
                    if (attempt == maxRetries) return null;
                    Thread.Sleep(50 * (attempt + 1)); // backoff, then re-attempt the CONNECT only
                    continue;
                }

                // --- Post-connect: single attempt, NEVER loop back to re-send. ---
                // Once connected we write the command, after which the resident may
                // have APPLIED it. Any failure now — an empty reply (it closed /
                // crashed before answering), a broken read, or bad JSON — must NOT
                // trigger a re-send: that would double-apply non-idempotent ops
                // (add/remove/move/swap/batch) and silently corrupt the document.
                // We surface "not delivered" (null); the caller (CommandBuilder)
                // turns that into a busy error so the user can retry or close+reopen.
                // At-most-once over at-least-once: a not-applied command surfaces as
                // a visible error the caller can re-issue; a double-apply does not.
                // (Mirrors sdk/python/officecli.py `_rpc`, whose retry likewise
                // covers only the connect phase.)
                try
                {
                    PipeWriteLine(client, json);

                    var responseLine = PipeReadLine(client);
                    if (responseLine == null) return null;

                    return System.Text.Json.JsonSerializer.Deserialize<ResidentResponse>(responseLine, ResidentJsonContext.Default.ResidentResponse);
                }
                catch
                {
                    return null;
                }
            }
            finally
            {
                client.Dispose();
            }
        }
        return null;
    }

    /// <summary>
    /// Ask a running resident to change its idle timeout. Used by `open`
    /// to upgrade a short-lived resident that `create` auto-started
    /// (60s) up to the normal 12min interactive window. Served by the
    /// ping pipe, so it succeeds even while the main pipe is busy.
    /// Returns false if the resident isn't running, the value is out
    /// of range, or the RPC failed.
    /// </summary>
    public static bool SendSetIdleTimeout(string filePath, int seconds)
    {
        var pipeName = ResidentServer.GetPipeName(filePath) + "-ping";
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            client.Connect(200);

            var request = new ResidentRequest { Command = "__set-idle-timeout__" };
            request.Args["seconds"] = seconds.ToString();
            var json = System.Text.Json.JsonSerializer.Serialize(request, ResidentJsonContext.Default.ResidentRequest);
            PipeWriteLine(client, json);

            var responseLine = PipeReadLine(client);
            if (responseLine == null) return false;

            var response = System.Text.Json.JsonSerializer.Deserialize<ResidentResponse>(responseLine, ResidentJsonContext.Default.ResidentResponse);
            return response != null && response.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Ask a running resident to flush its in-memory document to disk
    /// (handler.Save). Used by `merge` so a template held open by a resident
    /// with unsaved part-level edits is current on disk before File.Copy reads
    /// it — the resident saves itself; the caller never touches the handler.
    /// Returns false when no resident is running or the RPC failed (the caller
    /// then just reads whatever is already on disk, same as before).
    /// </summary>
    public static bool SendSave(string filePath)
    {
        if (!TryConnect(filePath, out _)) return false;
        var response = TrySend(filePath, new ResidentRequest { Command = "save" });
        return response != null && response.ExitCode == 0;
    }

    /// <summary>
    /// Send a close command to the resident server.
    /// </summary>
    public static bool SendClose(string filePath)
    {
        return SendCloseWithResponse(filePath, out _);
    }

    /// <summary>
    /// Send a close command and surface the resident's response so callers
    /// can distinguish "no resident running" (return false) from "resident
    /// shut down but reported an error during teardown" (return true with
    /// non-zero ExitCode + Stderr — see BUG-BT-R26-2 file-vanished case).
    /// </summary>
    public static bool SendCloseWithResponse(string filePath, out ResidentResponse? response)
    {
        response = null;
        // Send close via the dedicated ping pipe (always responsive)
        var pipeName = ResidentServer.GetPipeName(filePath) + "-ping";
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            client.Connect(200);

            var request = new ResidentRequest { Command = "__close__" };
            var json = System.Text.Json.JsonSerializer.Serialize(request, ResidentJsonContext.Default.ResidentRequest);
            PipeWriteLine(client, json);

            var responseLine = PipeReadLine(client);
            if (responseLine == null) return false;

            response = System.Text.Json.JsonSerializer.Deserialize<ResidentResponse>(responseLine, ResidentJsonContext.Default.ResidentResponse);
            // Reaching the resident at all (any deserializable response) means
            // a resident was running — even if it reported teardown errors.
            return response != null;
        }
        catch
        {
            return false;
        }
    }

    // ==================== Pipe I/O helpers ====================
    //
    // On Windows, StreamReader/StreamWriter deadlock on named pipes under .NET 11
    // preview — the managed stream wrapper's internal buffering stalls reads even
    // when bytes are available on the wire.  Raw byte I/O avoids the issue.
    //
    // On Linux/macOS, StreamReader/StreamWriter work fine and are faster (buffered
    // reads), so we keep using them.

    // Generous upper bound on a single pipe message. Far above any realistic
    // command reply; it exists only to bound memory against a runaway/garbage
    // stream. When exceeded we raise an explicit error instead of truncating to
    // null — a silent truncate would be misreported up the stack as "command
    // not delivered".
    private const int MaxMessageLength = 512 * 1024 * 1024; // 512 MB

    private static void PipeWriteLine(Stream pipe, string line)
    {
        if (!OperatingSystem.IsWindows())
        {
            using var writer = new StreamWriter(pipe, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
            writer.WriteLine(line);
            return;
        }
        var bytes = Encoding.UTF8.GetBytes(line + "\n");
        pipe.Write(bytes, 0, bytes.Length);
        pipe.Flush();
    }

    private static string? PipeReadLine(Stream pipe)
    {
        if (!OperatingSystem.IsWindows())
        {
            using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);
            return reader.ReadLine();
        }
        // Windows: read in whole chunks until the newline terminator. The payload
        // is a single line of compact JSON (string contents have their newlines
        // escaped), so the first '\n' ends the message and nothing follows it on a
        // one-command-per-connection pipe. The newline is located with a
        // vectorized span scan and chunks are appended in bulk — no per-byte work.
        // A reply that fits in one read (the common case for small commands) is
        // decoded straight from the read buffer with zero intermediate
        // allocation. There is no length cap that silently drops an over-size
        // reply; a generous ceiling only guards against a runaway stream and
        // raises rather than truncating.
        var chunk = new byte[65536];
        List<byte>? acc = null; // allocated only when a reply spans multiple reads
        while (true)
        {
            var bytesRead = pipe.Read(chunk, 0, chunk.Length);
            if (bytesRead == 0)
                return acc is { Count: > 0 } ? DecodeLine(CollectionsMarshal.AsSpan(acc)) : null;

            var span = chunk.AsSpan(0, bytesRead);
            var nl = span.IndexOf((byte)'\n');
            if (nl >= 0)
            {
                var tail = span[..nl];
                if (acc is null || acc.Count == 0)
                    return DecodeLine(tail); // single-read fast path: no List, no copy
                acc.AddRange(tail);
                return DecodeLine(CollectionsMarshal.AsSpan(acc));
            }

            acc ??= new List<byte>(bytesRead * 2);
            acc.AddRange(span);
            if (acc.Count > MaxMessageLength)
                throw new IOException($"Resident pipe message exceeded {MaxMessageLength} bytes.");
        }
    }

    private static string DecodeLine(ReadOnlySpan<byte> line)
    {
        if (line.Length > 0 && line[^1] == (byte)'\r')
            line = line[..^1];
        return Encoding.UTF8.GetString(line);
    }
}

// Copyright 2026 OfficeCLI (https://OfficeCLI.AI)
// SPDX-License-Identifier: Apache-2.0

using System.CommandLine;
using OfficeCli.Core;

namespace OfficeCli;

static partial class CommandBuilder
{
    // ==================== save command ====================
    //
    // Flush the resident's in-memory document to disk WITHOUT ending the
    // session. Only meaningful inside a running resident — agents that build
    // a workbook incrementally and need mid-build snapshots (e.g. for a
    // third-party Excel viewer that ingests the .xlsx package directly) use
    // this to recover the parse-amortization benefit of resident mode while
    // still publishing snapshots on demand.
    //
    // Non-resident mode is rejected on purpose: each non-resident command
    // already opens-mutates-closes (close = save), so there's no pending
    // in-memory state to flush. A no-op success would invite confused
    // "save just in case" code; an error tells the user they're in the
    // wrong mode.
    private static Command BuildSaveCommand(Option<bool> jsonOption)
    {
        var saveFileArg = new Argument<FileInfo>("file") { Description = "Office document path" };
        var saveCommand = new Command("save", "Flush the resident's in-memory document to disk without ending the session (requires an active resident; start one with `open`)");
        saveCommand.Add(saveFileArg);
        saveCommand.Add(jsonOption);

        saveCommand.SetAction(result => { var json = result.GetValue(jsonOption); return SafeRun(() =>
        {
            var file = result.GetValue(saveFileArg)!;
            var filePath = file.FullName;

            // Probe without auto-starting. `save` is meaningless without a
            // pre-existing in-memory session, so we deliberately skip the
            // TryResident auto-start path that other verbs use.
            if (!ResidentClient.TryConnect(filePath, out _))
            {
                var msg = $"No resident running for {file.Name}. Start one with 'officecli open {file.Name}' before calling save.";
                if (json)
                    Console.WriteLine(OutputFormatter.WrapEnvelopeError(msg));
                else
                    Console.Error.WriteLine($"Error: {msg}");
                return 1;
            }

            var request = new ResidentRequest { Command = "save", Json = json };
            var response = ResidentClient.TrySend(
                filePath, request,
                maxRetries: ResidentBusyMaxRetries,
                connectTimeoutMs: ResidentBusyConnectTimeoutMs);
            if (response == null)
            {
                var msg = $"Resident for {file.Name} is running but the save command could not be delivered (main pipe busy or unresponsive).";
                if (json)
                    Console.WriteLine(OutputFormatter.WrapEnvelopeError(msg));
                else
                    Console.Error.WriteLine($"Error: {msg}");
                return 3;
            }

            if (!string.IsNullOrEmpty(response.Stdout))
                Console.WriteLine(response.Stdout);
            if (!string.IsNullOrEmpty(response.Stderr))
                Console.Error.WriteLine(response.Stderr);
            return response.ExitCode;
        }, json); });

        return saveCommand;
    }
}

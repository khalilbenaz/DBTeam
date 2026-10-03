using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DBTeam.Core.Infrastructure;

public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);

/// <summary>
/// Lance un processus sans shell : les arguments passent par <see cref="ProcessStartInfo.ArgumentList"/>
/// (aucune concaténation, donc aucun problème de guillemets/$/retours ligne), stdout et stderr sont lus
/// en parallèle et de façon asynchrone (pas d'interblocage de tampons), et rien ne bloque le thread appelant.
/// </summary>
public static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName, IEnumerable<string> arguments, string? workingDirectory = null, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        if (workingDirectory is not null) psi.WorkingDirectory = workingDirectory;
        foreach (var a in arguments) psi.ArgumentList.Add(a);

        using var p = new Process { StartInfo = psi };
        p.Start();
        p.StandardInput.Close(); // jamais d'invite interactive qui attendrait l'entrée

        var stdout = p.StandardOutput.ReadToEndAsync(ct);
        var stderr = p.StandardError.ReadToEndAsync(ct);
        try
        {
            await p.WaitForExitAsync(ct).ConfigureAwait(false);
            return new ProcessResult(p.ExitCode,
                await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { /* déjà terminé */ }
            throw;
        }
    }
}

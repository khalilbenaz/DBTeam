using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DBTeam.Core.Infrastructure;

/// <summary>
/// Écriture atomique : le contenu est écrit dans un fichier temporaire voisin, flushé sur disque,
/// puis le fichier cible est remplacé en une opération. Un crash pendant l'écriture laisse donc
/// l'ancien fichier intact au lieu d'un fichier tronqué.
/// </summary>
public static class AtomicFile
{
    public static async Task WriteAsync(string path, Func<Stream, CancellationToken, Task> write, CancellationToken ct = default)
    {
        var tmp = TempPathFor(path);
        try
        {
            await using (var fs = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await write(fs, ct).ConfigureAwait(false);
                await fs.FlushAsync(ct).ConfigureAwait(false);
                fs.Flush(flushToDisk: true);
            }
            Replace(tmp, path);
        }
        catch
        {
            TryDelete(tmp);
            throw;
        }
    }

    public static Task WriteAllTextAsync(string path, string content, CancellationToken ct = default) =>
        WriteAsync(path, async (s, token) =>
        {
            var bytes = new UTF8Encoding(false).GetBytes(content);
            await s.WriteAsync(bytes, token).ConfigureAwait(false);
        }, ct);

    public static void WriteAllText(string path, string content)
    {
        var tmp = TempPathFor(path);
        try
        {
            using (var fs = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = new UTF8Encoding(false).GetBytes(content);
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(flushToDisk: true);
            }
            Replace(tmp, path);
        }
        catch
        {
            TryDelete(tmp);
            throw;
        }
    }

    private static string TempPathFor(string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".";
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
    }

    private static void Replace(string tmp, string path)
    {
        if (File.Exists(path)) File.Replace(tmp, path, destinationBackupFileName: null);
        else File.Move(tmp, path);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* nettoyage au mieux */ }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

internal static class OneFileLauncher
{
    private static readonly byte[] Marker = Encoding.ASCII.GetBytes("MYSTIC_SQUARE_PAYLOAD_V1");
    private const long Version = 100;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string extractionRoot = ExtractPayload();
            string gamePath = Path.Combine(extractionRoot, "Build", "MysticSquare.exe");
            if (!File.Exists(gamePath))
                throw new FileNotFoundException("完整游戏文件未找到。", gamePath);

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = gamePath,
                WorkingDirectory = Path.GetDirectoryName(gamePath),
                UseShellExecute = false,
                Arguments = JoinArguments(args)
            };
            using (Process game = Process.Start(start))
            {
                game.WaitForExit();
                return game.ExitCode;
            }
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(
                "东方怪绮谈启动失败：\n\n" + ex.Message,
                "东方怪绮谈",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
            return 1;
        }
    }

    private static string ExtractPayload()
    {
        string self = Process.GetCurrentProcess().MainModule.FileName;
        byte[] file = File.ReadAllBytes(self);
        int trailerLength = sizeof(long);
        if (file.Length < Marker.Length + trailerLength)
            throw new InvalidDataException("单文件包不完整。");

        long payloadLength = BitConverter.ToInt64(file, file.Length - trailerLength);
        long payloadStart = file.Length - trailerLength - payloadLength;
        if (payloadLength <= 0 || payloadStart < Marker.Length || payloadStart > file.Length)
            throw new InvalidDataException("单文件包长度无效。");
        byte[] foundMarker = new byte[Marker.Length];
        Buffer.BlockCopy(file, (int)(payloadStart - Marker.Length), foundMarker, 0, Marker.Length);
        if (!Marker.SequenceEqual(foundMarker))
            throw new InvalidDataException("未找到单文件包数据。");

        byte[] payload = new byte[payloadLength];
        Buffer.BlockCopy(file, (int)payloadStart, payload, 0, (int)payloadLength);
        string hash;
        using (SHA256 sha = SHA256.Create())
            hash = BitConverter.ToString(sha.ComputeHash(payload)).Replace("-", "").Substring(0, 16).ToLowerInvariant();
        string root = Path.Combine(Path.GetTempPath(), "MysticSquare-1.0.0-" + hash);
        string gamePath = Path.Combine(root, "Build", "MysticSquare.exe");
        if (!File.Exists(gamePath))
        {
            Directory.CreateDirectory(root);
            string archive = Path.Combine(root, "payload.zip");
            File.WriteAllBytes(archive, payload);
            ZipFile.ExtractToDirectory(archive, root);
            File.Delete(archive);
        }
        return root;
    }

    private static string JoinArguments(string[] args)
    {
        return string.Join(" ", args.Select(QuoteArgument));
    }

    private static string QuoteArgument(string arg)
    {
        if (arg.Length == 0) return "\"\"";
        if (!arg.Any(char.IsWhiteSpace) && !arg.Contains('"')) return arg;
        return "\"" + arg.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}

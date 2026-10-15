namespace Client;

public static class ClientLogger
{
    private static readonly object Lock = new();
    private static readonly string LogPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ImageProcessor", "client.log");

    static ClientLogger()
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(LogPath)!);
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        var line = $"[{level}] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        lock (Lock)
        {
            try { System.IO.File.AppendAllText(LogPath, line + Environment.NewLine); }
            catch { /* ignore */ }
        }
    }
}

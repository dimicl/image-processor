namespace Server.Core;

public static class Logger
{
    private static readonly object Lock = new();
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "server.log");

    public static void Info(string message) => Write("INFO", message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        var line = $"[{level}] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        Console.WriteLine(line);
        lock (Lock)
        {
            try { File.AppendAllText(LogPath, line + Environment.NewLine); }
            catch {  }
        }
    }
}

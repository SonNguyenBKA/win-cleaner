namespace WinCleaner.Core;

/// <summary>Ghi mọi thao tác dọn vào file log. Lỗi ghi log không được làm hỏng thao tác dọn.</summary>
public sealed class ActionLogger(string filePath)
{
    public static ActionLogger Default { get; } = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinCleaner", "actions.log"));

    private readonly Lock _lock = new();

    public string FilePath { get; } = filePath;

    public void Log(string action, string message)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-ddTHH:mm:sszzz} {action} {message}{Environment.NewLine}";
        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.AppendAllText(FilePath, line);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }
}

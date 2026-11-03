namespace WinCleaner.Core.Tests;

/// <summary>Thư mục tạm riêng cho mỗi test, tự xoá khi Dispose.</summary>
public sealed class TempDir : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("wincleaner-test-").FullName;

    public string Dir(string relative) => Directory.CreateDirectory(Combine(relative)).FullName;

    public string File(string relative, int bytes = 0)
    {
        var path = Combine(relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllBytes(path, new byte[bytes]);
        return path;
    }

    public string Combine(string relative) => System.IO.Path.Combine(Path, relative);

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch (IOException) { }
    }
}

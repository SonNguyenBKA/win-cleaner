namespace WinCleaner.Core;

public static class SizeCalculator
{
    // Không bỏ qua file ẩn/hệ thống (mặc định của .NET), nhưng không đi theo symlink/junction.
    private static readonly EnumerationOptions Options = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
    };

    /// <summary>Tổng dung lượng file/thư mục. Link tính là 0 vì dọn link chỉ xoá link.</summary>
    public static Task<long> SizeAsync(string path, CancellationToken ct = default) => Task.Run(() =>
    {
        var info = new FileInfo(path);
        if (info.LinkTarget is not null) return 0L;
        if (info.Exists) return info.Length;
        if (!Directory.Exists(path)) return 0L;

        long total = 0;
        foreach (var file in new DirectoryInfo(path).EnumerateFiles("*", Options))
        {
            ct.ThrowIfCancellationRequested();
            total += file.Length;
        }
        return total;
    }, ct);
}

namespace WinCleaner.Core;

public sealed record CleanItem(string Path, long Size, string? Detail = null)
{
    public string Name => System.IO.Path.GetFileName(Path);
}

public sealed class CleanResult
{
    public List<CleanItem> Cleaned { get; } = [];
    public List<(CleanItem Item, string Reason)> Failures { get; } = [];
    public long FreedBytes => Cleaned.Sum(i => i.Size);
}

/// <summary>Chuyển file/thư mục vào Thùng rác. Bản Windows nằm ở WinCleaner.Platform.</summary>
public interface IRecycleBin
{
    void Recycle(string path);
}

public sealed class Cleaner(IRecycleBin recycleBin, ActionLogger logger)
{
    /// <summary>Dọn các item đã chọn. Mỗi item phải qua <paramref name="guard"/>; item lỗi được ghi lại, không dừng cả lượt.</summary>
    public CleanResult Clean(IEnumerable<CleanItem> items, PathGuard guard)
    {
        var result = new CleanResult();
        foreach (var item in items)
        {
            try
            {
                var target = guard.Validate(item.Path);
                recycleBin.Recycle(target);
                logger.Log("TRASH", $"{target} ({item.Size} bytes)");
                result.Cleaned.Add(item);
            }
            catch (Exception e) when (e is PathGuardException or IOException or UnauthorizedAccessException)
            {
                logger.Log("FAIL", $"{item.Path}: {e.Message}");
                result.Failures.Add((item, e.Message));
            }
        }
        return result;
    }
}

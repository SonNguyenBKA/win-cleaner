namespace WinCleaner.Core.Tests;

public class CleanerTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private readonly FakeRecycleBin _bin = new();
    private readonly ActionLogger _logger;

    public CleanerTests() => _logger = new ActionLogger(_tmp.Combine("logs/actions.log"));

    public void Dispose() => _tmp.Dispose();

    private string Log => File.ReadAllText(_logger.FilePath);

    [Fact]
    public void Recycles_allowed_items_and_logs_them()
    {
        var root = _tmp.Dir("cache");
        var a = _tmp.File("cache/a.bin", 100);
        var b = _tmp.Dir("cache/sub");

        var result = new Cleaner(_bin, _logger).Clean([new(a, 100), new(b, 20)], new PathGuard([root], SystemPaths.Current));

        Assert.Equal(2, result.Cleaned.Count);
        Assert.Empty(result.Failures);
        Assert.Equal(120, result.FreedBytes);
        Assert.Equal(2, _bin.Recycled.Count);
        Assert.Contains("TRASH", Log);
        Assert.Contains("a.bin", Log);
    }

    [Fact]
    public void Skips_items_rejected_by_guard()
    {
        var root = _tmp.Dir("cache");
        var outside = _tmp.File("outside/keep.txt", 5);

        var result = new Cleaner(_bin, _logger).Clean([new(outside, 5)], new PathGuard([root], SystemPaths.Current));

        Assert.Empty(result.Cleaned);
        Assert.Single(result.Failures);
        Assert.Empty(_bin.Recycled);
        Assert.True(File.Exists(outside));
        Assert.Contains("FAIL", Log);
        Assert.Contains("keep.txt", Log);
    }

    [Fact]
    public void Records_recycle_errors_and_continues()
    {
        var root = _tmp.Dir("cache");
        var locked = _tmp.File("cache/locked.bin", 1);
        var ok = _tmp.File("cache/ok.bin", 2);
        _bin.FailOn = "locked.bin";

        var result = new Cleaner(_bin, _logger).Clean([new(locked, 1), new(ok, 2)], new PathGuard([root], SystemPaths.Current));

        Assert.Equal("locked.bin", Assert.Single(result.Failures).Item.Name);
        Assert.Equal("ok.bin", Assert.Single(result.Cleaned).Name);
    }

    private sealed class FakeRecycleBin : IRecycleBin
    {
        public List<string> Recycled { get; } = [];
        public string? FailOn { get; set; }

        public void Recycle(string path)
        {
            if (FailOn is not null && path.EndsWith(FailOn)) throw new IOException("File đang được dùng");
            Recycled.Add(path);
        }
    }
}

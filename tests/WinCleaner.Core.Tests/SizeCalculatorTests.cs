namespace WinCleaner.Core.Tests;

public class SizeCalculatorTests : IDisposable
{
    private readonly TempDir _tmp = new();

    public void Dispose() => _tmp.Dispose();

    [Fact]
    public async Task Sums_nested_and_hidden_files()
    {
        var root = _tmp.Dir("cache");
        _tmp.File("cache/a.bin", 100);
        _tmp.File("cache/sub/deep/b.bin", 250);
        var hidden = _tmp.File("cache/.hidden", 50);
        File.SetAttributes(hidden, File.GetAttributes(hidden) | FileAttributes.Hidden);

        Assert.Equal(400, await SizeCalculator.SizeAsync(root));
    }

    [Fact]
    public async Task Size_of_a_single_file()
    {
        Assert.Equal(123, await SizeCalculator.SizeAsync(_tmp.File("one.bin", 123)));
    }

    [Fact]
    public async Task Does_not_follow_links()
    {
        var root = _tmp.Dir("cache");
        _tmp.File("cache/a.bin", 10);
        var outside = _tmp.Dir("outside");
        _tmp.File("outside/big.bin", 5000);
        Directory.CreateSymbolicLink(Path.Combine(root, "dirlink"), outside);
        File.CreateSymbolicLink(Path.Combine(root, "filelink"), Path.Combine(outside, "big.bin"));

        Assert.Equal(10, await SizeCalculator.SizeAsync(root));
        Assert.Equal(0, await SizeCalculator.SizeAsync(Path.Combine(root, "dirlink")));
    }

    [Fact]
    public async Task Missing_path_is_zero()
    {
        Assert.Equal(0, await SizeCalculator.SizeAsync(_tmp.Combine("missing")));
    }

    [Fact]
    public async Task Can_be_cancelled()
    {
        var root = _tmp.Dir("cache");
        _tmp.File("cache/a.bin", 10);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SizeCalculator.SizeAsync(root, cts.Token));
    }
}

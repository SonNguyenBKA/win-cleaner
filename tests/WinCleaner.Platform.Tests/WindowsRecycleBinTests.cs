using System.Runtime.Versioning;
using WinCleaner.Core;

namespace WinCleaner.Platform.Tests;

/// <summary>Chỉ chạy trên Windows (CI windows-latest); trên Mac thì Skip.</summary>
public sealed class WindowsFactAttribute : FactAttribute
{
    public WindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows()) Skip = "Chỉ chạy trên Windows";
    }
}

[SupportedOSPlatform("windows")]
public class WindowsRecycleBinTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("wincleaner-test-").FullName;

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    [WindowsFact]
    public void Cleaner_moves_file_to_recycle_bin_and_logs_it()
    {
        // Không có đuôi file để tên trong Thùng rác không phụ thuộc tuỳ chọn "ẩn đuôi file" của Explorer.
        var name = $"wincleaner-{Guid.NewGuid():N}";
        var file = Path.Combine(_root, name);
        File.WriteAllBytes(file, new byte[64]);
        var logger = new ActionLogger(Path.Combine(_root, "actions.log"));

        var result = new Cleaner(new WindowsRecycleBin(), logger)
            .Clean([new(file, 64)], new PathGuard([_root], SystemPaths.Current));

        Assert.Empty(result.Failures);
        Assert.False(File.Exists(file));
        Assert.Contains(name, RecycleBinNames());
        Assert.Contains($"TRASH {file}", File.ReadAllText(logger.FilePath));
    }

    [WindowsFact]
    public void Missing_file_throws()
    {
        Assert.Throws<IOException>(() => new WindowsRecycleBin().Recycle(Path.Combine(_root, "missing")));
    }

    private static List<string> RecycleBinNames()
    {
        dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application")!)!;
        dynamic bin = shell.NameSpace(10); // 10 = Recycle Bin
        var names = new List<string>();
        foreach (dynamic item in bin.Items()) names.Add((string)item.Name);
        return names;
    }
}

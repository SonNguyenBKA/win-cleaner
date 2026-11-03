namespace WinCleaner.Core.Tests;

public class PathGuardTests : IDisposable
{
    private readonly TempDir _tmp = new();
    private static readonly SystemPaths NoSystem = new([], [], []);

    public void Dispose() => _tmp.Dispose();

    private PathGuard Guard(params string[] roots) => new(roots, NoSystem);

    private static PathGuardReason Reject(PathGuard guard, string path) =>
        Assert.Throws<PathGuardException>(() => guard.Validate(path)).Reason;

    [Fact]
    public void Allows_file_and_folder_inside_root()
    {
        var root = _tmp.Dir("cache");
        var file = _tmp.File("cache/a.bin", 10);
        var dir = _tmp.Dir("cache/sub");

        Assert.EndsWith("a.bin", Guard(root).Validate(file));
        Assert.EndsWith("sub", Guard(root).Validate(dir));
    }

    [Fact]
    public void Rejects_the_root_itself()
    {
        var root = _tmp.Dir("cache");
        Assert.Equal(PathGuardReason.OutsideAllowedRoots, Reject(Guard(root), root));
    }

    [Fact]
    public void Rejects_path_outside_roots()
    {
        var root = _tmp.Dir("cache");
        var other = _tmp.File("other/b.bin");
        Assert.Equal(PathGuardReason.OutsideAllowedRoots, Reject(Guard(root), other));
    }

    [Fact]
    public void Rejects_dot_dot_escape()
    {
        var root = _tmp.Dir("cache");
        _tmp.File("other/b.bin");
        var sneaky = Path.Combine(root, "..", "other", "b.bin");
        Assert.Equal(PathGuardReason.OutsideAllowedRoots, Reject(Guard(root), sneaky));
    }

    [Fact]
    public void Rejects_sibling_with_same_prefix()
    {
        var root = _tmp.Dir("cache");
        var sibling = _tmp.File("cache2/b.bin");
        Assert.Equal(PathGuardReason.OutsideAllowedRoots, Reject(Guard(root), sibling));
    }

    [Fact]
    public void Rejects_missing_path()
    {
        var root = _tmp.Dir("cache");
        Assert.Equal(PathGuardReason.NotFound, Reject(Guard(root), Path.Combine(root, "missing")));
    }

    [Fact]
    public void Rejects_going_through_a_link_that_points_outside()
    {
        var root = _tmp.Dir("cache");
        var outside = _tmp.Dir("outside");
        _tmp.File("outside/secret.txt", 5);
        Directory.CreateSymbolicLink(Path.Combine(root, "link"), outside);

        var viaLink = Path.Combine(root, "link", "secret.txt");
        Assert.Equal(PathGuardReason.OutsideAllowedRoots, Reject(Guard(root), viaLink));
    }

    [Fact]
    public void Allows_the_link_itself_because_only_the_link_is_removed()
    {
        var root = _tmp.Dir("cache");
        var outside = _tmp.Dir("outside");
        var link = Path.Combine(root, "link");
        Directory.CreateSymbolicLink(link, outside);

        Assert.EndsWith("link", Guard(root).Validate(link));
    }

    [Fact]
    public void Allows_broken_link_inside_root()
    {
        var root = _tmp.Dir("cache");
        var link = Path.Combine(root, "broken");
        File.CreateSymbolicLink(link, Path.Combine(_tmp.Path, "gone.txt"));

        Assert.EndsWith("broken", Guard(root).Validate(link));
    }

    [Fact]
    public void Rejects_forbidden_folder_even_when_it_is_an_allowed_root()
    {
        var windows = _tmp.Dir("Windows");
        var dll = _tmp.File("Windows/System32/kernel32.dll");
        var guard = new PathGuard([windows], new SystemPaths([], [windows], []));

        Assert.Equal(PathGuardReason.Forbidden, Reject(guard, dll));
    }

    [Fact]
    public void Allows_exception_folder_inside_forbidden_folder()
    {
        var windows = _tmp.Dir("Windows");
        var temp = _tmp.Dir("Windows/Temp");
        var junk = _tmp.File("Windows/Temp/old.tmp");
        var guard = new PathGuard([temp], new SystemPaths([], [windows], [temp]));

        Assert.EndsWith("old.tmp", guard.Validate(junk));
    }

    [Fact]
    public void Rejects_protected_folder()
    {
        var home = _tmp.Dir("Users/son");
        var guard = new PathGuard([_tmp.Path], new SystemPaths([home], [], []));

        Assert.Equal(PathGuardReason.Forbidden, Reject(guard, home));
    }

    [Fact]
    public void Rejects_drive_root()
    {
        var driveRoot = Path.GetPathRoot(_tmp.Path)!;
        Assert.Equal(PathGuardReason.Forbidden, Reject(Guard(driveRoot), driveRoot));
    }

    [Fact]
    public void Real_system_folders_are_forbidden_on_windows()
    {
        if (!OperatingSystem.IsWindows()) return;
        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var drive = Path.GetPathRoot(system32)!;
        var guard = new PathGuard([drive], SystemPaths.Current);

        Assert.Equal(PathGuardReason.Forbidden, Reject(guard, Path.Combine(system32, "kernel32.dll")));
        Assert.Equal(PathGuardReason.Forbidden, Reject(guard, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)));
        Assert.Equal(PathGuardReason.Forbidden, Reject(guard, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
    }
}

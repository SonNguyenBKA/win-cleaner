namespace WinCleaner.Core;

public enum PathGuardReason { NotFound, Forbidden, OutsideAllowedRoots }

public sealed class PathGuardException(PathGuardReason reason, string path) : Exception(Describe(reason, path))
{
    public PathGuardReason Reason { get; } = reason;
    public string Path { get; } = path;

    private static string Describe(PathGuardReason reason, string path) => reason switch
    {
        PathGuardReason.NotFound => $"Không tìm thấy: {path}",
        PathGuardReason.Forbidden => $"Đường dẫn hệ thống, không được xoá: {path}",
        _ => $"Nằm ngoài vùng được phép dọn: {path}",
    };
}

/// <summary>
/// Thư mục hệ thống không bao giờ được xoá.
/// Protected: chặn đúng thư mục đó. Forbidden: chặn cả thư mục lẫn mọi thứ bên trong,
/// trừ những gì nằm hẳn bên trong một thư mục Exceptions (vd. C:\Windows\Temp).
/// </summary>
public sealed record SystemPaths(IReadOnlyList<string> Protected, IReadOnlyList<string> Forbidden, IReadOnlyList<string> Exceptions)
{
    public static SystemPaths Current { get; } = CreateCurrent();

    private static SystemPaths CreateCurrent()
    {
        static string F(Environment.SpecialFolder f) => Environment.GetFolderPath(f);
        var windows = F(Environment.SpecialFolder.Windows);
        var profile = F(Environment.SpecialFolder.UserProfile);

        string[] protectedPaths =
        [
            profile, System.IO.Path.GetDirectoryName(profile) ?? "",
            F(Environment.SpecialFolder.Desktop), F(Environment.SpecialFolder.MyDocuments),
            System.IO.Path.Combine(profile, "Downloads"),
            F(Environment.SpecialFolder.ApplicationData), F(Environment.SpecialFolder.LocalApplicationData),
            System.IO.Path.GetTempPath(),
        ];
        string[] forbidden =
        [
            windows, F(Environment.SpecialFolder.ProgramFiles), F(Environment.SpecialFolder.ProgramFilesX86),
            F(Environment.SpecialFolder.CommonApplicationData),
        ];
        string[] exceptions = windows == ""
            ? []
            : [System.IO.Path.Combine(windows, "Temp"), System.IO.Path.Combine(windows, "SoftwareDistribution", "Download")];

        static string[] Clean(string[] paths) => paths.Where(p => p != "").ToArray();
        return new(Clean(protectedPaths), Clean(forbidden), Clean(exceptions));
    }
}

/// <summary>Chốt chặn cuối trước khi xoá: chỉ cho phép path nằm hẳn bên trong một allowed root.</summary>
public sealed class PathGuard
{
    // Windows không phân biệt hoa thường; app chỉ nhắm Windows nên so sánh không phân biệt ở mọi nơi.
    private const StringComparison Cmp = StringComparison.OrdinalIgnoreCase;

    private readonly string[] _roots;
    private readonly string[] _protected;
    private readonly string[] _forbidden;
    private readonly string[] _exceptions;

    public PathGuard(IEnumerable<string> allowedRoots, SystemPaths system)
    {
        _roots = Resolve(allowedRoots);
        _protected = Resolve(system.Protected);
        _forbidden = Resolve(system.Forbidden);
        _exceptions = Resolve(system.Exceptions);
    }

    /// <summary>Trả về path đã giải link ở thư mục cha; ném <see cref="PathGuardException"/> nếu không được xoá.</summary>
    public string Validate(string path)
    {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        var parentRaw = Path.GetDirectoryName(full);
        if (parentRaw is null) throw new PathGuardException(PathGuardReason.Forbidden, full); // gốc ổ đĩa

        // Giải link ở thư mục cha (chặn đi vòng qua link/junction), giữ nguyên bản thân item:
        // xoá một link chỉ xoá link, không đụng tới đích.
        var parent = RealPath(parentRaw);
        if (parent is null || !Exists(full)) throw new PathGuardException(PathGuardReason.NotFound, full);
        var target = Path.Combine(parent, Path.GetFileName(full));

        var isForbidden = _protected.Any(p => string.Equals(target, p, Cmp))
            || (_forbidden.Any(f => IsInside(target, f, orEqual: true)) && !_exceptions.Any(e => IsInside(target, e, orEqual: false)));
        if (isForbidden) throw new PathGuardException(PathGuardReason.Forbidden, target);

        if (!_roots.Any(r => IsInside(target, r, orEqual: false)))
            throw new PathGuardException(PathGuardReason.OutsideAllowedRoots, target);
        return target;
    }

    private static bool IsInside(string path, string root, bool orEqual)
    {
        if (string.Equals(path, root, Cmp)) return orEqual;
        var prefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, Cmp);
    }

    private static bool Exists(string path) =>
        File.Exists(path) || Directory.Exists(path) || new FileInfo(path).LinkTarget is not null;

    private static string[] Resolve(IEnumerable<string> paths) =>
        paths.Select(p => RealPath(p) ?? Path.TrimEndingDirectorySeparator(Path.GetFullPath(p))).ToArray();

    /// <summary>Đường dẫn thật của một thư mục: giải mọi symlink/junction từ gốc ổ đĩa xuống. Null nếu không tồn tại.</summary>
    private static string? RealPath(string dir)
    {
        var full = Path.GetFullPath(dir);
        var current = Path.GetPathRoot(full)!;
        foreach (var part in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            var info = new DirectoryInfo(current);
            if (info.LinkTarget is not null)
                current = info.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? current;
            if (!Directory.Exists(current)) return null;
        }
        return Path.TrimEndingDirectorySeparator(current);
    }
}

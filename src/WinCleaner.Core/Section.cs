namespace WinCleaner.Core;

/// <summary>Một mục trên sidebar. Glyph là mã icon trong font Segoe Fluent Icons.</summary>
public sealed record Section(string Id, string Title, string Glyph)
{
    public static readonly IReadOnlyList<Section> All =
    [
        new("overview", "Tổng quan", ""),
        new("system", "Rác hệ thống", ""),
        new("dev", "Rác dev", ""),
        new("large", "File lớn & cũ", ""),
        new("process", "Tiến trình", ""),
        new("fan", "Quạt & nhiệt độ", ""),
    ];
}

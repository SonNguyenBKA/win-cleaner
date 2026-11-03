using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using WinCleaner.Core;

namespace WinCleaner.Platform;

[SupportedOSPlatform("windows")]
public sealed class WindowsRecycleBin : IRecycleBin
{
    private const uint FO_DELETE = 3;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    // Item không vào được Thùng rác (ổ mạng, quá lớn…) thì hỏi trước thay vì lặng lẽ xoá vĩnh viễn.
    private const ushort FOF_WANTNUKEWARNING = 0x4000;

    public void Recycle(string path)
    {
        // Kiểm trước: path không tồn tại thì SHFileOperation bật hộp thoại lỗi thay vì trả mã lỗi.
        if (!File.Exists(path) && !Directory.Exists(path) && new FileInfo(path).LinkTarget is null)
            throw new FileNotFoundException($"Không tìm thấy: {path}", path);

        var op = new SHFILEOPSTRUCT
        {
            wFunc = FO_DELETE,
            pFrom = Path.GetFullPath(path) + "\0\0", // danh sách kết thúc bằng 2 ký tự null
            fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_WANTNUKEWARNING,
        };
        var code = SHFileOperation(ref op);
        if (code != 0) throw new IOException($"Không chuyển được vào Thùng rác (mã 0x{code:X}): {path}");
        if (op.fAnyOperationsAborted) throw new IOException($"Đã huỷ chuyển vào Thùng rác: {path}");
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);
}

# Win Cleaner

App Windows native (C# WPF, .NET 10) dùng cá nhân, bản Windows của [Mac Cleaner](https://github.com/SonNguyenBKA/Mac-cleans): dọn rác, tìm file lớn, theo dõi tiến trình nặng, nhiệt độ và điều khiển quạt.

> **Đang phát triển.** Đã xong: khung app (sidebar 6 mục) và lõi an toàn xoá file. Các màn hình dọn rác đang làm — xem tiến độ ở [`plans/win-cleaner.md`](plans/win-cleaner.md).

## Tính năng (dự kiến)

| Mục | Nội dung |
|---|---|
| **Tổng quan (Smart Scan)** | Quét song song rác hệ thống, rác dev, file lớn; hiện tổng dung lượng có thể dọn |
| **Rác hệ thống** | `%TEMP%` cũ > 3 ngày, `C:\Windows\Temp`, crash dump, Windows Error Reporting, thumbnail cache, Windows Update cache, Thùng rác |
| **Rác dev (React Native)** | Gradle, npm, Yarn, NuGet, Metro cache, Android `system-images` không dùng, `node_modules` / `android\app\build` của project không đụng > 30 ngày |
| **File lớn & cũ** | File ≥ 500 MB, hoặc ≥ 50 MB và không mở > 180 ngày; mở trong Explorer |
| **Tiến trình** | CPU/RAM cập nhật mỗi 2s, cảnh báo tiến trình ăn CPU lâu, nút Kết thúc / Buộc dừng |
| **Quạt & nhiệt độ** | Nhiệt độ CPU/GPU/ổ đĩa, RPM; điều khiển quạt (Auto / % cố định / theo cảm biến / đường cong) trên máy hỗ trợ |

## An toàn

- Không bao giờ tự xoá: luôn Quét → xem danh sách → tích chọn → bấm Dọn → xác nhận.
- Xoá = **chuyển vào Thùng rác**. Item không vào được Thùng rác (ổ mạng, quá lớn) thì Windows hỏi trước, không lặng lẽ xoá vĩnh viễn.
- Chỉ xoá được đường dẫn nằm trong danh sách thư mục cho phép (`PathGuard`); chặn link/junction trỏ ra ngoài, gốc ổ đĩa, `C:\Windows` (trừ `Windows\Temp`), Program Files, ProgramData, thư mục người dùng.
- Mọi thao tác dọn được ghi vào `%LOCALAPPDATA%\WinCleaner\actions.log`.
- Quạt: điều khiển qua Windows Service; quạt tự về mặc định khi app thoát / crash / treo > 15s, khi máy sleep, khi service dừng. Không bao giờ đặt dưới 30%.

## Tải và chạy

Chưa có bản phát hành. File `.exe` được GitHub Actions build sau mỗi lần push:

1. Vào [Actions](https://github.com/SonNguyenBKA/win-cleaner/actions) (cần đăng nhập GitHub), mở lần chạy mới nhất có dấu ✓.
2. Tải artifact **WinCleaner-x64**, giải nén ra `WinCleaner.exe` (chạy luôn, không cần cài .NET).
3. App chưa ký số nên lần đầu Windows hiện SmartScreen: bấm **More info → Run anyway**.

Hoặc bằng lệnh:

```bash
gh run download -R SonNguyenBKA/win-cleaner -n WinCleaner-x64
```

Yêu cầu: Windows 10/11 x64.

## Build từ mã nguồn

Yêu cầu: [.NET 10 SDK](https://dotnet.microsoft.com/download) (macOS: `brew install dotnet`).

```bash
dotnet build WinCleaner.slnx    # build được cả trên Mac (chỉ chạy được trên Windows)
dotnet test WinCleaner.slnx     # test Core chạy trên Mac; test Windows tự bỏ qua ngoài Windows
dotnet publish src/WinCleaner.App -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -o publish
```

## Cấu trúc

```
src/WinCleaner.Core/        logic quét / dọn / an toàn (net10.0, test được trên Mac)
src/WinCleaner.Platform/    API riêng của Windows: Thùng rác, WMI, cảm biến
src/WinCleaner.App/         giao diện WPF
src/WinCleaner.FanService/  Windows Service điều khiển quạt
tests/                      xUnit: Core.Tests (mọi OS), Platform.Tests (chỉ Windows)
plans/                      kế hoạch và tiến độ
.github/workflows/          CI: test + build WinCleaner.exe trên windows-latest
```

# Win Cleaner — app dọn rác Windows (C# WPF)

Bản Windows của Mac Cleaner (`/Users/Work/mac-cleaner`), dùng cá nhân: dọn rác, rác dev React Native, file lớn & cũ, tiến trình nặng, nhiệt độ + điều khiển quạt.

## Quyết định thiết kế
- **Stack**: C# + WPF, **.NET 10 LTS** (đề xuất thay .NET 8 vì .NET 8 hết hỗ trợ 10/11/2026). Windows 10/11 x64.
- **Cấu trúc**: `src/WinCleaner.Core` (logic, `net10.0` thuần → test chạy được trên Mac), `src/WinCleaner.Platform` (API Windows: Thùng rác, WMI, quạt — `net10.0-windows`), `src/WinCleaner.App` (WPF), `src/WinCleaner.FanService` (Windows Service), `tests/`.
- **Build/test**: viết code trên Mac (`brew install dotnet`), `dotnet test` phần Core trên Mac; GitHub Actions `windows-latest` build + test toàn bộ, xuất artifact `WinCleaner-x64.zip` (single-file self-contained) để anh tải về máy Windows chạy.
- **Xoá = chuyển vào Recycle Bin** (`Microsoft.VisualBasic.FileIO.FileSystem` + `SendToRecycleBin`). Ngoại lệ: nút "Dọn Thùng rác" (`SHEmptyRecycleBin`) có xác nhận riêng.
- **An toàn** (giữ y như bản Mac):
  - Không tự xoá: Quét → danh sách checkbox → Dọn → xác nhận.
  - `PathGuard`: chỉ xoá trong whitelist; chặn junction/symlink trỏ ra ngoài, `C:\Windows` (trừ `Windows\Temp`), `Program Files`, `System32`, gốc ổ đĩa (có test).
  - Log mọi thao tác: `%LOCALAPPDATA%\WinCleaner\actions.log`.
- **Quyền**: app chạy quyền thường. Mục cần admin (`C:\Windows\Temp`, Windows Update cache) có nút "Chạy lại với quyền admin". Quạt do service chạy LocalSystem đảm nhiệm.

### Nhóm rác
| Nhóm | Nguồn |
|---|---|
| Hệ thống | `%TEMP%` cũ > 3 ngày, `C:\Windows\Temp` (admin), `%LOCALAPPDATA%\CrashDumps`, Windows Error Reporting, thumbnail cache, `SoftwareDistribution\Download` (admin), Recycle Bin |
| Dev RN | `~\.gradle\caches`, `%LOCALAPPDATA%\npm-cache`, Yarn cache, `~\.nuget\packages`, Metro cache trong `%TEMP%`, Android SDK `system-images` không có AVD dùng, `node_modules` / `android\app\build` / `android\.gradle` trong project không đụng > 30 ngày (thư mục gốc do anh chọn) |
| File lớn & cũ | Thư mục chọn (mặc định `%USERPROFILE%`): ≥ 500 MB, hoặc ≥ 50 MB và không mở > 180 ngày; bỏ `AppData`, `.git` |
| Tiến trình | `Process` + đo CPU mỗi 2s, top CPU/RAM, cảnh báo > 80% CPU liên tục > 5 phút; Kết thúc (`CloseMainWindow`) / Buộc dừng (`Kill`); không cho kill tiến trình hệ thống / của SYSTEM |
| Quạt & nhiệt | Xem mục riêng bên dưới |

### Quạt & nhiệt độ — rủi ro cao, nói trước
- Thư viện **LibreHardwareMonitorLib** (NuGet): đọc nhiệt CPU/GPU/ổ đĩa, RPM; điều khiển quạt theo **%** (không theo RPM như Mac).
- Cần driver kernel: bản LHM mới dùng **PawnIO** (driver ký, cài riêng một lần); WinRing0 cũ bị Defender chặn → không dùng. (Giả định — xác minh ở bước 8.)
- **Chỉ chạy được trên một số máy**: mainboard desktop có chip Super I/O (Nuvoton/ITE…) điều khiển tốt; **laptop phần lớn không** (quạt do EC của hãng quản). App phải tự phát hiện quạt nào điều khiển được, còn lại chỉ hiển thị.
- An toàn: điều khiển nằm trong **Windows Service** (LocalSystem), app nói chuyện qua named pipe (chỉ nhận user hiện tại). Service tự trả quạt về mặc định khi: app thoát/crash, mất heartbeat > 15s, máy sleep (resume thì áp lại), service dừng. Không bao giờ đặt dưới mức tối thiểu (mặc định 30%).
- **Không test được trên CI** (runner không có quạt) → bước 9–10 phải test trên máy thật của anh.

## Các bước

- [ ] **1. Khung project + CI** — solution, 5 project, `.gitignore`, workflow `.github/workflows/build.yml` (build, test, publish zip artifact). Cửa sổ WPF có sidebar 6 mục trống.
  Model: `sonnet`
  → verify: `dotnet test tests/WinCleaner.Core.Tests` pass trên Mac; Actions chạy xanh, tải được zip; anh mở `.exe` trên Windows thấy sidebar 6 mục.

- [ ] **2. Core an toàn + đo dung lượng** — `PathGuard`, `SizeCalculator` (async, huỷ được), `Cleaner` (gọi `IRecycleBin`, ghi log), model `CleanItem`.
  Model: `opus` (an toàn xoá file)
  → verify: xUnit với thư mục tạm: ngoài whitelist / junction ra ngoài / `C:\Windows\System32` / `C:\` bị từ chối; size thư mục mẫu đúng; test Windows-only trên CI: file vào Recycle Bin + có dòng log.

- [ ] **3. Rác hệ thống** — `SystemJunkScanner` + màn hình; nút Dọn Thùng rác riêng; nút chạy lại quyền admin.
  Model: `sonnet`
  → verify: test cây thư mục giả (file `%TEMP%` < 3 ngày không bị liệt kê); trên máy thật: tổng `%TEMP%` khớp Explorer (sai < 5%).

- [ ] **4. Rác dev RN** — `DevJunkScanner` + màn hình + chọn thư mục gốc project.
  Model: `sonnet`
  → verify: test project giả (`node_modules` mtime 40 ngày → có, 10 ngày → không); máy thật: dọn Gradle cache xong project Android vẫn build lại được.

- [ ] **5. File lớn & cũ** — `LargeFileScanner` + màn hình (sort, "Mở trong Explorer").
  Model: `sonnet`
  → verify: test file sparse 600 MB + file atime 200 ngày → có, file nhỏ mới → không; quét `%USERPROFILE%` có progress, Cancel được, UI không treo.

- [ ] **6. Tiến trình** — `ProcessMonitor` + màn hình + cảnh báo + Kết thúc/Buộc dừng.
  Model: `sonnet`
  → verify: test tính % CPU từ 2 mẫu giả; máy thật: chạy script vòng lặp vô hạn → cảnh báo (hạ ngưỡng khi test) → Buộc dừng được; tiến trình SYSTEM không có nút kill.

- [ ] **7. Tổng quan (Smart Scan)** — chạy 3 scanner song song, tổng dung lượng, Dọn tất cả đã chọn; icon; tray icon.
  Model: `sonnet`
  → verify: tổng Smart Scan = tổng 3 nhóm; CI xanh.

- [ ] **8. Đọc nhiệt độ & quạt (chỉ đọc)** — tích hợp LHM trong service, app hiển thị cảm biến + RPM + biểu đồ 10 phút; xác minh PawnIO.
  Model: `opus`
  → verify: trên máy anh, nhiệt CPU lệch < 3°C so với HWiNFO/LHM gốc; liệt kê quạt nào điều khiển được.

- [ ] **9. Điều khiển quạt** — chế độ Auto / % cố định / theo cảm biến / đường cong; preset; watchdog + trả về mặc định.
  Model: `opus`
  → verify (máy thật, làm cùng anh): đặt 60% → RPM đổi; kill app trong Task Manager → ≤ 15s quạt về mặc định; dừng service → về mặc định; sleep/wake → áp lại preset; không đặt được < 30%.

- [ ] **10. Đóng gói** — installer (Inno Setup) cài app + service, gỡ cài đặt sạch; README.
  Model: `sonnet`
  → verify: cài/gỡ trên máy Windows, service biến mất sau khi gỡ, quạt về mặc định.

## Lưu ý
- Không ký code (không có chứng chỉ Windows) → SmartScreen sẽ cảnh báo lần đầu ("More info → Run anyway").
- Repo GitHub mới (`SonNguyenBKA/win-cleaner`?) cần để chạy Actions — tạo repo và mỗi lần push đều hỏi anh trước.
- Ngoài phạm vi bản đầu: gỡ app, dọn registry, dọn RAM, chạy nền tự động.

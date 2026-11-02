// Service điều khiển quạt (LocalSystem). Logic quạt + watchdog thêm ở bước 8–9.
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "WinCleaner Fan Service");

builder.Build().Run();

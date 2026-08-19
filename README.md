# GradCountdown

> **A tiny Windows desktop widget counting down the days of graduate school.**  
> 一个轻量、简洁的 Windows 研究生毕业倒计时桌面小组件。

---

## 📸 Screenshots

### Graduation Countdown
![Graduation Countdown](screenshots/graduation-countdown.png)

### Elapsed Graduate-School Days
![Elapsed Days](screenshots/elapsed-days.png)

### Customization
![Settings](screenshots/settings.png)

### System Tray
![System Tray](screenshots/tray-menu.png)

---

## ✨ Features

- 🎯 **Graduation Countdown & Elapsed Days**:
  - Mode A: Graduation Countdown with remaining days and target date (`2028-07-01`).
  - Mode B: Graduate-school elapsed days with start date (`2025-09-01`).
  - Click anywhere on the card to instantly switch between modes.
- 📅 **Accurate Date Boundary Handling**:
  - Shows "尚未入学" (Not enrolled yet) before the start date.
  - Shows `0` on graduation day.
  - Shows "已毕业" (Graduated) after graduation day (no negative numbers).
- 🔍 **Proportional Continuous UI Scaling**:
  - Scale from **60% to 130%** (step 5%, default 85%).
  - Scales fonts, corner radius, icons, and layout simultaneously without distortion.
- 🎨 **Adjustable Transparency**:
  - Opacity from **60% to 100%** (step 5%, default 95%) with one-click reset.
- 🔒 **Position Lock**:
  - Prevent accidental dragging while preserving click-to-switch interaction.
- 📌 **Always on Top**:
  - Optional topmost pinning (default OFF; other windows can freely cover the widget).
- 🚀 **Optional Windows Auto-Start**:
  - Optional user-level registry auto-start (`HKCU`, default OFF, no admin rights required).
- 🔔 **System Tray & Quick Controls**:
  - Clean system tray icon with Show / Hide / Topmost / Auto-Start / Exit options.
  - Double-click tray icon or desktop shortcut to restore and activate the widget.
- 🛡️ **Single-Instance Protection**:
  - Duplicate launches automatically restore and foreground the active widget.
- 💾 **Local Settings Persistence**:
  - Window position, scale, opacity, mode, and switches are persisted locally to `%LOCALAPPDATA%\GradCountdown\settings.json`.
- 🔒 **100% Offline & Private**:
  - No internet connection required, zero telemetry, zero privacy tracking.

---

## 📅 Default Dates & Customization

The default configuration in source code:

- **Start Date**: `2025-09-01`
- **Graduation Date**: `2028-07-01`

You can easily adapt this project for your own goals (College graduation, Postgrad entrance exam, Anniversaries, Project deadlines) by updating the dates in `GradCountdown/Services/CountdownService.cs`.

---

## 🖥️ System Requirements & Environment

- **Operating System**: Developed and tested on Windows 10 22H2 (Build 19045), compatible with Windows x64.
- **Framework**: .NET 10 + C# + WPF
- **Distribution**: Self-contained Windows x64 executable (no pre-installed .NET runtime required).

---

## 📦 Build & Publish

### Run Automated Tests
```powershell
dotnet run --project GradCountdown.Tests\GradCountdown.Tests.csproj -c Release
```

### Self-Contained Windows x64 Publish
```powershell
dotnet publish GradCountdown\GradCountdown.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o .\publish
```

The output standalone executable will be located at `publish\GradCountdown.exe`.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

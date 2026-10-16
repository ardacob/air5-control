# Air5 Control

**A Windows tray companion for SOUNDPEATS Air5 Pro.**

[Türkçe](README.md) · [Download](https://github.com/ardacob/air5-control/releases/latest) · [Protocol](docs/PROTOCOL.md)

Control ANC, transparency, normal listening and game mode; view left/right/case battery levels; start with Windows and reconnect automatically. The application sends no EQ or gain commands. This is an independent project, not affiliated with SOUNDPEATS.

![Main window, waiting for a connection](docs/images/main-window.png)

## Get started

Download the portable ZIP from [Releases](https://github.com/ardacob/air5-control/releases/latest), extract it to a permanent folder, pair your earbuds in Windows Bluetooth settings, and run **Air5-Control.exe**. No administrator rights are required. The executable is unsigned.

The application starts in the notification area. Double-click its icon to open the window; right-click for modes, game mode, battery, refresh, reconnect, startup and Exit. Closing or minimizing the window hides it to the tray.

On its first normal run, Windows startup is enabled by default for the current user. Disable it through **Windows ile başlat**. Your preference is retained. After moving the executable, run it once from its new location to refresh an enabled startup entry.

## Audio-first reconnection

The BLE control channel and the Windows audio connection are separate. Version 1.6 waits for a matching active Windows render endpoint, observes the same endpoint for at least eight seconds, then opens BLE. If the audio endpoint disappears, BLE resources are released. Manual reconnect follows the same gate.

The application checks every three seconds, waits at least fifteen seconds after failed operations, and refreshes connected battery/mode information about every thirty seconds. It does not change pairing, the Bluetooth radio, the Windows default audio device or EQ. It does not reapply ANC or game mode on reconnect.

## Scope

- Windows, .NET Framework 4.8, and Windows Bluetooth support.
- Developed for the exact paired name **SOUNDPEATS Air5 Pro**. Other models, including Pro+, are not supported by assumption.
- Case battery is the last value reported by the earbuds, not an independent live measurement.
- Game mode is enabled only after a valid state reply and verified by querying again after a change. No latency value has been measured or promised.
- Adaptive EQ is not read, backed up or rewritten. The normal/ANC bass difference was also observed with the earbuds' touch controls.
- An active render endpoint does not prove that it is selected as default or that a listener can hear playback.

## Build and test

Use Windows PowerShell 5.1:

~~~powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Derle.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test.ps1
~~~

The build uses .NET Framework CodeDOM, WPF/Windows Forms and Windows WinMetadata. It requires no NuGet packages. It creates Air5-Control.exe in the repository root with the UI, icon and manifest embedded.

The automated checks exercise the audio gate, endpoint filtering, retry timing, command allowlist and response decoding. Hardware validation is separate. Version 1.6 was user-tested on Air5 Pro firmware S1_20250410_V2.2, including power-cycle reconnection, audible video playback and game mode.

## Data and removal

No application network requests or telemetry. Diagnostics are local: connection.log records audio/control transitions and game-mode results; air5.log contains exception details. Review logs before sharing them.

Startup uses HKCU\Software\Microsoft\Windows\CurrentVersion\Run (Air5Control), with a preference under HKCU\Software\Air5Control. Turn startup off, choose Exit and delete the folder to remove the application. The small preference key can remain.

## Repository

- src/ — C# implementation, WPF XAML, icon and manifest.
- Derle.ps1 — build entry point.
- scripts/Test.ps1 — automated checks without Bluetooth or startup registration.
- docs/ — protocol, architecture and troubleshooting.
- .github/workflows/build.yml — Windows build/test workflow.

[MIT License](LICENSE). SOUNDPEATS and product names belong to their respective owners.

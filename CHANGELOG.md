# Changelog

## 1.6 — 2026-10-17

- Wait for an active Air5 Pro Windows audio endpoint before opening BLE.
- Require at least eight seconds of endpoint stability; reset on disconnect.
- Release BLE resources when audio disappears.
- Show Windows audio and control connection states separately.
- Add query-verified game mode controls in the window and tray.
- Preserve ANC/transparency/normal, battery, startup, reconnection and local logs.
- User validated power-cycle audio reconnection, audible video playback and game mode.

## Earlier development

- 1.5.1: connection lifecycle diagnostics.
- 1.5: automatic retry while the earbuds are off or disconnected.
- 1.4: per-user Windows startup and hidden launch.
- 1.3: system tray controls and battery status.
- 1.2: normal mode restored after comparing earbud touch controls.
- 1.0–1.1: initial portable WPF application and mode/battery controls.

EQ experiments are excluded from the shipped application.

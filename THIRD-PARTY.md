# Third-party software

## Magic Trackpad 2 driver (not included)

Trackpad Companion does not work without a driver that exposes the trackpad to Windows as a Precision Touchpad.
That driver is a separate project and is **not part of this repository**. The installer downloads it from its
author's release page and installs it with `pnputil`.

- Driver: [vitoplantamura/MagicTrackpad2ForWindows](https://github.com/vitoplantamura/MagicTrackpad2ForWindows), GPL-2.0
- Based on: [imbushuo/mac-precision-touchpad](https://github.com/imbushuo/mac-precision-touchpad) (the USB driver is GPL-2.0)

Trackpad Companion talks to the driver the same way the driver's own control panel does: a control device
(`\\.\AmtPtpControlDeviceUm`) for the battery level and settings reload, and registry values under the driver's
`Parameters` key. It contains no code from the driver or its control panel.

## Apple Boot Camp drivers (not included, not needed)

Apple's Boot Camp driver packages are proprietary and must not be redistributed. They are not required.
If you installed Boot Camp, Magic Utilities, Trackpad++ or an older precision-touchpad driver for this trackpad,
remove them before installing the driver above.

## Build dependencies

| Package | License |
| --- | --- |
| [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) | MIT |
| System.Drawing.Common | MIT |
| [Inno Setup](https://jrsoftware.org/isinfo.php) (installer build only) | Inno Setup License |

# RoundedTB
Add margins, rounded corners and segments to your taskbars! 

![RoundedTB](https://cdn.discordapp.com/attachments/272509873479221249/891555515799318568/unknown.png)

## How do I get it?
The easiest way to download RoundedTB is from the [Microsoft Store](https://www.microsoft.com/store/productId/9MTFTXSJ9M7F). You can also download the latest version from the Releases tab, unzip it and run `RoundedTB.exe`. If you're a madman, you can compile it yourself or check out the latest [Canary build](https://nightly.link/torchgm/RoundedTB/workflows/ci/master/artifacts.zip) (note these can be very unfinished, buggy and unstable).

## To use
### Basic options
The simplest way to use RoundedTB is by simply entering a margin and corner radius.
 - **Margin** - controls how many pixels to remove from each side of the taskbar, creating a margin around it that you can see and click through.
 -  **Corner Radius** - adjusts how round the corners of the taskbar should be.

### Advanced options
The advanced options allow for further customisation, at the cost of some user-friendliness.
- **Independent Margins** - in the advanced settings, a <kbd>...</kbd> button appears on the margin box. Click it to enable independent margins, which allow you to specify the margin for each side of the taskbar. You can also use negative values to hide the rounded corners for some sides, allowing you to "attach" the taskbar to different sides of the monitor.
- **Dynamic Mode (Windows 11)** - dynamic mode automatically resizes the taskbars to accomodate the number of icons in it, making the taskbar behave similarly to macOS' Dock.
- **Split Mode (Windows 10)** - split mode is a simplified version of dynamic mode for Windows 10. Due to a more limited taskbar, dynamically resizing the taskbar isn't possible. However after some setup, split mode allows you to separate the taskbar from the system tray and resize it at will. I admit it's certainly not as cool as dynamic mode but for now it's better than nothing 🥺
- **Show System Tray** - this toggles whether or not the system tray, clock etc. is displayed in dynamic/split mode. It can be toggled at any time by pressing <kbd>Win</kbd>+<kbd>F2</kbd>.
- **TranslucentTB Compatibility** - due to a bug in Windows, apps that alter the composition of the taskbar don't allow RoundedTB's changes to show up automatically. Whilst I'm currently not aware of a fix, I've worked closely with [Sylveon](https://github.com/sylveon) to enable some level of compatibility between [TranslucentTB](https://github.com/TranslucentTB/TranslucentTB) and RoundedTB. This is experimental and *will* flicker slightly. It requires TranslucentTB version 2021.5 to function.
- **About RoundedTB** - provides information about the current version of RoundedTB. The "Debug" section lets you open the config and log files.

## Known issues
 - Auto-hiding is still incredibly experimental and may lead to a lot of flickering, especially with TranslucentTB compatibility or dynamic/split mode enabled.
 - Rounded corners are not antialiased due to a Windows limitation.
 - Dynamic mode/split mode only work correctly when the taskbar is horizontal at the top/bottom of the screen.
 - Split mode on Windows 10 only supports the main taskbar, secondary taskbars will not be split.
 - When using dynamic mode, the taskbar may occasionally become too large, too small or not update. This can usually be fixed by moving a window to or from that monitor or briefly changing the taskbar alignment. These issues will be reduced in upcoming updates, don't worry! I just need to refactor a lot of code first.
 - Compatibility with taskbar mods outside of TranslucentTB version 2021.5 is not currently guaranteed.

## Other info
RoundedTB is just a hobby of mine, and I'm certainly not an expert in this field, so I'm really sorry if you encounter a bug! If anything breaks catastrophically, press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Esc</kbd> to open Task Manager, end RoundedTB and then restart Explorer. At worst, just reboot your PC. RoundedTB makes no permanent changes (though it will run on startup if you enable it from the tray icon), so restarting should clear any issues.

Feel free to let me know about any bugs by filing an issue so I can look into it. Alternatively if you want to discuss RoundedTB, get some insider sneak-peeks, need some assistance or just want to see what I'm up to, then feel free to join the [Discord server](https://discord.gg/wYQJd8VGSB).

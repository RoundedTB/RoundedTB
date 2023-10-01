![RoundedTB](https://cdn.discordapp.com/attachments/272509873479221249/891555515799318568/unknown.png)

# RoundedTB
#### Add margins, rounded corners and segments to your taskbars!



![image](https://user-images.githubusercontent.com/31840547/134795141-76349eaf-12da-40f8-b2a0-d7b7c268d152.png)

## How do I get it?

You can download the latest version from the [release page](https://github.com/Gniang/RoundedTB/releases).

The Microsoft Store's RoundedTB is not mine. It's the [original version](https://github.com/RoundedTB/RoundedTB).


## Known issues
- Auto-hiding compatibillity. (Widnows taskbar setting)
  - not working. 
- Windows 10.
  - Windows 10 has not been verified. Probably will no longer be supported.
- TranslucentTB compatibility.
- Rounded corners are not antialiased.
  - Due to a Windows limitation. ([#4](https://github.com/torchgm/RoundedTB/issues/4))
- Dynamic mode won't hide the left side of the taskbar if fisrst started(the taskbar alignment has never been changed).
  - This can be worked around by changing the alignment to Left and back to Center. ([#98](https://github.com/torchgm/RoundedTB/issues/98)) 
- When using dynamic mode, the taskbar may occasionally become too large, too small or not update.
  - This can usually be fixed by moving a window to or from that monitor or briefly changing the taskbar alignment.
- Dynamic mode may not be released when RoundedTB is exited.
  - Restarting RoundedTB doesn't help, try restarting the PC.
- The top of the taskbar is not rounded.
  - Set the top margin to a larger value (30-50?) , you can adjust the appearance in that session. But it may be a different value when restarted the Windows.

## Other info

RoundedTB was created by torchgm. thanks.

https://github.com/RoundedTB/RoundedTB

torchgm says "RoundedTB is just a hobby of mine, and I'm certainly not an expert in this field, so I'm really sorry if you encounter a bug!".
me too.

If anything breaks catastrophically, press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Esc</kbd> to open Task Manager, end RoundedTB and then restart Explorer. At worst, just reboot your PC. RoundedTB makes no permanent changes (though it will run on startup if you enable it from the tray icon), so restarting should clear any issues.




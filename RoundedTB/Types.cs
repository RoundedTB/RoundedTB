using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoundedTB
{
    public class Types
    {
        public class Taskbar
        {
            public IntPtr TaskbarHwnd { get; set; } // Handle to the taskbar
            public IntPtr TrayHwnd { get; set; } // Handle to the tray on the taskbar (if present)
            public IntPtr AppListHwnd { get; set; } // Handle to the list of open/pinned apps on the taskbar
            public LocalPInvoke.RECT TaskbarRect { get; set; } // Bounding box for the taskbar
            public LocalPInvoke.RECT TrayRect { get; set; }  // Bounding box for the tray (dynamic)
            public LocalPInvoke.RECT AppListRect { get; set; } // Bounding box for the list of pinned & open apps (dynamic)
            public IntPtr RecoveryHrgn { get; set; } // Pointer to the recovery region for any given taskbar. Defaults to IntPtr.Zero
            public double ScaleFactor { get; set; } // The scale factor of the monitor the taskbar is on
            public string TaskbarRes { get; set; } // Resolution of the taskbar as text
            public int FailCount { get; set; } // Number of times the taskbar has had an "erroneous" size at applytime
            public bool Ignored { get; set; } // Specifies if the taskbar should be ignored when applying changes

            public int AppListWidth { get; set; }
            public TaskbarEffect TaskbarEffectWindow { get; set; }
        }

        public class Settings
        {
            public int CornerRadius { get; set; }
            public int MarginBottom { get; set; }
            public int MarginLeft { get; set; }
            public int MarginRight { get; set; }
            public int MarginTop { get; set; }
            public bool IsDynamic { get; set; }
            public bool IsCentred { get; set; }
            public bool ShowTray { get; set; }
            public bool CompositionCompat { get; set; }
        }

        public class TaskbarEffectiveRegion
        {
            public int EffectiveCornerRadius { get; set; }
            public int EffectiveTop { get; set; }
            public int EffectiveLeft { get; set; }
            public int EffectiveWidth { get; set; }
            public int EffectiveHeight { get; set; }
        }

        public enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }
    }
}

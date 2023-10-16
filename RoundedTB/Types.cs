using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Interop.UIAutomationClient;


namespace RoundedTB
{
    public class Types
    {
        public class Taskbar : IDisposable
        {
            public AppListXaml AppListXaml { get; set; }
            public IntPtr TaskbarHwnd { get; set; } // Handle to the taskbar
            public IntPtr TrayHwnd { get; set; } // Handle to the tray on the taskbar (if present)
            public IntPtr AppListHwnd { get; set; } // Handle to the list of open/pinned apps on the taskbar
            public LocalPInvoke.RECT TaskbarRect { get; set; } // Bounding box for the taskbar
            public LocalPInvoke.RECT TrayRect { get; set; }  // Bounding box for the tray (dynamic)
            public LocalPInvoke.RECT AppListRect { get; set; } // Bounding box for the list of pinned & open apps (dynamic)
            public IntPtr RecoveryHrgn { get; set; } // Pointer to the recovery region for any given taskbar. Defaults to IntPtr.Zero
            public double ScaleFactor { get; set; } // The scale factor of the monitor the taskbar is on
            public string TaskbarRes { get; set; } // Resolution of the taskbar as text
            public bool Ignored { get; set; } // Specifies if the taskbar should be ignored when applying changes
            public bool TaskbarHidden { get; set; } // Specifies if this taskbar is currently hidden by RTB
            public bool TrayHidden { get; set; } // Specifies if the tray is currently hidden by RTB on this taskbar
            public int AppListWidth { get; set; } // Specifies the width of the app list
            public TaskbarEffect TaskbarEffectWindow { get; set; } // Unused clone to apply effects to the taskbar
            public bool IsSecondary { get; set; }

            public void Dispose()
            {
                AppListXaml.Dispose();
            }
        }

#nullable enable
        public class AppListXaml : IDisposable
        {
            private IUIAutomationElement? _taskbarFrame;
            private IUIAutomation? _uia;
            private readonly IntPtr _hwndTaskbarMain;

            // singleton checker
            private static bool appListXamlAlreadyExists = false;

            public bool ReloadRequired => (AppListXaml.appListXamlAlreadyExists && this._taskbarFrame == null);

            public AppListXaml(IntPtr hwndTaskbarMain)
            {
                this._hwndTaskbarMain = hwndTaskbarMain;
                this._uia = new CUIAutomation();
                _taskbarFrame = GetTaskbarFrameElement(this._hwndTaskbarMain, this._uia);
            }

            private static IUIAutomationElement? GetTaskbarFrameElement(IntPtr hwndTaskbarMain, IUIAutomation uia)
            {
                IntPtr hwndDesktopXamlSrc = LocalPInvoke.FindWindowExA(hwndTaskbarMain, IntPtr.Zero, "Windows.UI.Composition.DesktopWindowContentBridge", null);
                if (hwndDesktopXamlSrc == IntPtr.Zero)
                {
                    return null;
                }
                IntPtr hwndWindowCls = LocalPInvoke.FindWindowExA(hwndDesktopXamlSrc, IntPtr.Zero, "Windows.UI.Input.InputSite.WindowClass", null);
                if (hwndWindowCls == IntPtr.Zero)
                {
                    return null;
                }
                IUIAutomationElement taskEle = uia.ElementFromHandle(hwndWindowCls);
                IUIAutomationCondition con = uia.CreatePropertyCondition(UIA_PropertyIds.UIA_AutomationIdPropertyId, "TaskbarFrame");
                IUIAutomationElement taskFrameEle = taskEle.FindFirst(Interop.UIAutomationClient.TreeScope.TreeScope_Children, con);

                Marshal.ReleaseComObject(con);
                Marshal.ReleaseComObject(taskEle);
                AppListXaml.appListXamlAlreadyExists = true;
                return taskFrameEle;
            }


            public void ReloadTaskbarFrameElement()
            {
                // When the taskbar is restarted, there's a possibility that XAML elements may not exist when the taskbar handle is created.
                // Therefore, if XAML elements have been previously acquired, it's considered a restart, and we attempt to retrieve XAML again.
                if (_uia == null)
                {
                    return;
                }
                _taskbarFrame = GetTaskbarFrameElement(_hwndTaskbarMain, _uia);
            }

            public LocalPInvoke.RECT? GetWindowRect()
            {
                if (_taskbarFrame == null || _uia == null)
                {
                    return null;
                }
                if (!LocalPInvoke.IsWindow(_hwndTaskbarMain))
                {
                    return null;
                }

                IUIAutomationElementArray? children = null;
                IUIAutomationElement? child = null;
                try
                {
                    children = _taskbarFrame.FindAll(
                        Interop.UIAutomationClient.TreeScope.TreeScope_Children,
                        _uia.CreateTrueCondition());
                    tagRECT? leftRect = null;
                    tagRECT? rightRect = null;
                    int len = children.Length;
                    if (len == 0)
                    {
                        return null;
                    }

                    for (int i = 0; i < len; i++)
                    {
                        child = children.GetElement(i);
                        tagRECT r = child.CurrentBoundingRectangle;
                        if (leftRect == null || r.left < leftRect.Value.left)
                        {
                            leftRect = r;
                        }
                        if (rightRect == null || rightRect.Value.right < r.right)
                        {
                            rightRect = r;
                        }
                        Marshal.ReleaseComObject(child);
                        child = null;
                    }
                    if (leftRect == null || rightRect == null)
                    {
                        return null;
                    }

                    LocalPInvoke.RECT rect = new()
                    {
                        Left = (int)leftRect.Value.left,
                        Top = (int)leftRect.Value.top,
                        Right = (int)rightRect.Value.right,
                        Bottom = (int)leftRect.Value.bottom,
                    };
                    return rect;
                }
                catch (Exception)
                {
                    // TODO: write log.
                    // An error occurs at here, the AppListXaml object will be recreated, so not reqire actions.
                    return null;
                }
                finally
                {
                    if (child != null)
                    {
                        Marshal.ReleaseComObject(child);
                    }
                    if (children != null)
                    {
                        Marshal.ReleaseComObject(children);
                    }
                }
            }

            public void Dispose()
            {
                if (_taskbarFrame != null)
                {
                    Marshal.ReleaseComObject(_taskbarFrame);
                    _taskbarFrame = null;
                }
                if (_uia != null)
                {
                    Marshal.ReleaseComObject(_uia);
                    _uia = null;
                }
            }
        }
#nullable restore

        public class Settings
        {
            public int Version { get; set; }
            public SegmentSettings SimpleTaskbarLayout { get; set; }
            public SegmentSettings DynamicAppListLayout { get; set; }
            public SegmentSettings DynamicTrayLayout { get; set; }
            public SegmentSettings DynamicWidgetsLayout { get; set; }
            public SegmentSettings DynamicSecondaryClockLayout { get; set; }
            public int WidgetsWidth { get; set; }
            public int ClockWidth { get; set; }
            public bool IsDynamic { get; set; }
            public bool IsCentred { get; set; }
            public bool IsWindows11 { get; set; }
            public bool ShowTray { get; set; }
            public bool ShowWidgets { get; set; }
            public bool ShowSecondaryClock { get; set; }
            public bool CompositionCompat { get; set; }
            public bool IsNotFirstLaunch { get; set; }
            public bool FillOnMaximise { get; set; }
            public bool FillOnTaskSwitch { get; set; }
            public bool ShowSegmentsOnHover { get; set; }
            public int AutoHide { get; set; }
        }

        public class EffectiveRegion
        {
            public int CornerRadius { get; set; }
            public int Top { get; set; }
            public int Left { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public class SegmentSettings
        {
            public int CornerRadius { get; set; }
            public int MarginTop { get; set; }
            public int MarginLeft { get; set; }
            public int MarginBottom { get; set; }
            public int MarginRight { get; set; }
        }

        public enum TrayMode
        {
            Show = 0,
            Hide = 1,
            AutoHide = 2,
        }

        public enum CompositionMode
        {
            None = 0,
            TranslucentTB = 1,
            Legacy = 2,
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

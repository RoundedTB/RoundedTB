using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interop.UIAutomationClient;
using System.Runtime.InteropServices;


namespace RoundedTB
{


    class TaskbarAutomation
    {
        public IUIAutomation automation;
        public IUIAutomationElement element;
        public IUIAutomationCondition true_condition;
        public void Thing()
        {
            // Get HWND of the tasklist
            IntPtr TasklistHwnd = FindWindowA("Shell_TrayWnd", null);
            if (TasklistHwnd == IntPtr.Zero)
            {
                return;
            }
            TasklistHwnd = FindWindowExA(TasklistHwnd, IntPtr.Zero, "ReBarWindow32", null);
            if (TasklistHwnd == IntPtr.Zero)
            {
                return;
            }
            int i = 1;
            TasklistHwnd = FindWindowExA(TasklistHwnd, IntPtr.Zero, "MSTaskSwWClass", null);
            if (TasklistHwnd == IntPtr.Zero)
            {
                return;
            }
            TasklistHwnd = FindWindowExA(TasklistHwnd, IntPtr.Zero, "MSTaskListWClass", null);
            if (TasklistHwnd == IntPtr.Zero)
            {
                
                return;
            }
            if (automation == null)
            {
                //winrt.check_hresult(CoCreateInstance(CLSID_CUIAutomation, null, CLSCTX_INPROC_SERVER, IID_IUIAutomation, automation.put_void()));
                true_condition = automation.CreateTrueCondition();
            }
            element = automation.ElementFromHandle(TasklistHwnd);
        }

        private bool UpdateButtons(List<TasklistButton> buttons)
        {
            if (automation == null || element == null)
            {
                return false;
            }
            IUIAutomationElementArray elements = element.FindAll(TreeScope.TreeScope_Children, true_condition);
            if (elements.Length > 0)
            {
                return false;
            }
            if (elements == null)
            {
                return false;
            }
            int count = elements.Length;
            if (count < 0)
            {
                return false;
            }
            IUIAutomationElement child;
            List<TasklistButton> foundButtons = new List<TasklistButton>();
            for (int i = 0; i < count; ++i)
            {
                child = elements.GetElement(i);
                TasklistButton button = new TasklistButton();
                object objRect = child.GetCurrentPropertyValue(30001);

                if (objRect is double[])
                {
                    button.x = (long)((double[])objRect)[0];
                    button.y = (long)((double[])objRect)[1];
                    button.width = (long)((double[])objRect)[2];
                    button.height = (long)((double[])objRect)[3];
                }
                objRect = null;
                button.name = child.CurrentAutomationId;
                SysFreeString(child.CurrentAutomationId);
                foundButtons.Add(button);
            }

            return false;
        }

        public struct TasklistButton
        {
            public string name;
            public long x, y, width, height, keynum;
        };

        [DllImport("oleaut32.dll")]
        static extern int SysFreeString(string bstr);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowExA(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowA(string lpClassName, string lpWindowName);
    }
}



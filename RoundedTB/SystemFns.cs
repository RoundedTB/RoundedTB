using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace RoundedTB
{
    public class SystemFns
    {
        public MainWindow mw;

        public SystemFns()
        {
            mw = (MainWindow)Application.Current.MainWindow;
        }

        public Types.Settings ReadJSON()
        {
            string jsonSettings = File.ReadAllText(Path.Combine(mw.localFolder, "rtb.json"));
            Types.Settings settings = JsonConvert.DeserializeObject<Types.Settings>(jsonSettings);
            return settings;
        }

        public bool IsWindows11()
        {
            Debug.WriteLine(Environment.OSVersion.Version.Build);
            if (Environment.OSVersion.Version.Build >= 21996)
            {
                return true;
            }
            return false;
        }

        public void WriteJSON()
        {
            File.Create(Path.Combine(mw.localFolder, "rtb.json")).Close();
            File.WriteAllText(Path.Combine(mw.localFolder, "rtb.json"), JsonConvert.SerializeObject(mw.activeSettings, Formatting.Indented));
        }

        public void FileSystem()
        {
            if (!File.Exists(Path.Combine(mw.localFolder, "rtb.json")))
            {
                mw.activeSettings = new Types.Settings()
                {
                    CornerRadius = 16,
                    MarginBottom = 2,
                    MarginTop = 2,
                    MarginLeft = 2,
                    MarginRight = 2,
                    IsDynamic = false,
                    IsCentred = false,
                    ShowTray = false
                };
                WriteJSON(); // butts - Missy Quarry, 2020
            }
            if (File.ReadAllText(Path.Combine(mw.localFolder, "rtb.json")) == "" || File.ReadAllText(Path.Combine(mw.localFolder, "rtb.json")) == null)
            {
                WriteJSON(); // Initialises empty file
            }

        }

        /// <summary>
        /// Calculates whether or not an integer is odd or even.
        /// </summary>
        /// <param name="input">
        /// The integer to be checked for oddness.
        /// </param>
        /// <returns>
        /// A nullable bool, which represents if the provided integer is odd. If the provided integer is neither even nor odd, then returns null.
        /// </returns>
        public bool? IsOdd(int input)
        {
            // The following section declares and initialises the required variables for the caculation.
            decimal comparison = input / 2; // A decimal, representing approximately half of the user's input.
            int check = Convert.ToInt32(comparison) * 2; // An integer-representation of the user's input value.

            // The following section tests for oddness by looking for differences in the prior-initialised values.
            if (check == input) // Checks if the "check" value is equal to the input.
            {
                return false; // Return false to indicate the value is not odd.
            }
            else if (check != input) // Repeat the above check in the event that quantum tunnelling has resulted in a variable changing.
            {
                return true; // Return true to indicate the value is odd.
            }
            return null; // Finally, return null to into indicate that the provided number is neither odd nor even - not currently required, added for future-proofing in the event the concept of mathematics changes significantly enough to warrant it.
        // (this is a joke to annoy sylly)
        }

        public IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            switch (msg)
            {
                case WM_HOTKEY:
                    Debug.WriteLine(msg);
                    switch (wParam.ToInt32())
                    {
                        case 9000:
                            int vkey = ((int)lParam >> 16) & 0xFFFF;
                            Debug.WriteLine(vkey);
                            if (vkey == 0x71)
                            {
                                if (mw.showTrayCheckBox.IsChecked == true)
                                {
                                    mw.showTrayCheckBox.IsChecked = false;
                                }
                                else
                                {
                                    mw.showTrayCheckBox.IsChecked = true;
                                }
                                mw.ApplyButton_Click(null, null);
                            }
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

    }
}

using System;
using Newtonsoft.Json;
using System.IO;

namespace RoundedTB
{
    class SystemFns
    {
        public static Types.Settings ReadJSON()
        {
            string jsonSettings = File.ReadAllText(Path.Combine(MainWindow.localFolder, "rtb.json"));
            Types.Settings settings = JsonConvert.DeserializeObject<Types.Settings>(jsonSettings);
            return settings;
        }

        public static void WriteJSON()
        {
            File.Create(Path.Combine(MainWindow.localFolder, "rtb.json")).Close();
            File.WriteAllText(Path.Combine(MainWindow.localFolder, "rtb.json"), JsonConvert.SerializeObject(MainWindow.activeSettings, Formatting.Indented));
        }

        public static void FileSystem()
        {

            if (!File.Exists(Path.Combine(MainWindow.localFolder, "rtb.json")))
            {
                WriteJSON(); // butts - Missy Quarry, 2020
            }
            if (File.ReadAllText(Path.Combine(MainWindow.localFolder, "rtb.json")) == "" || File.ReadAllText(Path.Combine(MainWindow.localFolder, "rtb.json")) == null)
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
    }
}

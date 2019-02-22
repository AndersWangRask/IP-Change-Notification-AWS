using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IPChange.Cmd
{
    public static class CommandLineUtilFunctions
    {
        /// <summary>
        /// Parses Command Line parameters according only to a certain specific pattern (see remarks)
        /// </summary>
        /// <param name="CommandLineArgs">The "raw" command line arguments, e.g. as passed in to Main</param>
        /// <param name="StringToObject">A function that converts the string to an object (e.g. converts string "True" to Boolean True, or whatever)</param>
        /// <param name="DefaultObjectValue">The default value where a key does not have a value. This default to Boolean True</param>
        /// <returns>
        /// A Dictionary of Key and Object Value
        /// </returns>
        /// <remarks>
        /// The supported pattern reads like this: -database:hello -silent -message:nobloodyway
        /// Only parameters starting with "-" will be parsed (because I say so :-) ).
        /// The value on the left of the ":" is the key, The value on the right is the value.
        /// Absent any value of a parameter the DefaultObjectValue is assigned. This defaults to Boolean True. (i.e. Present? True!)
        /// </remarks>
        public static Dictionary<string, object> GetCommandLineArgs(
            IEnumerable<string> CommandLineArgs,
            Func<string, string, object, IDictionary<string, object>, object> StringToObject = null,
            object DefaultObjectValue = null,
            dynamic DefaultValues = null)
        {
            //Set default values
            if (DefaultObjectValue == null)
            {
                DefaultObjectValue = true;
            }

            //new the return value
            Dictionary<string, object> commandLineArgsDictionary = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            //Get the Props of the Default Values (if any)
            Dictionary<string, object> dicDefaultValues = null;

            if (DefaultValues != null)
            {
                PropertyInfo[] pis = DefaultValues.GetType().GetProperties();

                dicDefaultValues =
                    pis
                        .ToDictionary(
                            pri => pri.Name, 
                            pri => pri.GetValue(DefaultValues), 
                            StringComparer.OrdinalIgnoreCase);
            }

            //Process and Add Command Line Args to the Return Value
            foreach (string arg in CommandLineArgs)
            {
                if ((!string.IsNullOrWhiteSpace(arg) && arg.StartsWith("-")))
                {
                    // only accept arguments that begins with "-" (because I say so)
                    // Split by ":". Key on the left side, Value (if any) on the right side
                    string[] vals = arg.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    string argKey = vals[0].TrimStart("-".ToCharArray());
                    object argVal = DefaultObjectValue;

                    if ((vals.Length > 1))
                    {
                        // If there is any value from the right side
                        if (StringToObject != null)
                        {
                            // If there is a StringToObject function that has been passed in
                            // (that function has the responsibility for returning the appropriate value)
                            argVal = StringToObject(vals[0], vals[1], DefaultObjectValue, dicDefaultValues);
                        }
                        else if (DefaultValues != null && dicDefaultValues.ContainsKey(argKey))
                        {
                            //If no StringToObject function we try to convert the string to the default value type
                            argVal = Convert.ChangeType(vals[1], dicDefaultValues[argKey].GetType());
                        }
                        else
                        {
                            //If no StringToObject function we just set the value as the string it originally was
                            argVal = vals[1];
                        }
                    }

                    // Add the Key, Value to the return value Dictionary
                    commandLineArgsDictionary.Add(argKey, argVal);
                }
            }

            //Consider Default Values
            if (DefaultValues != null)
            {
                foreach (string argName in dicDefaultValues.Keys)
                {
                    if (!commandLineArgsDictionary.ContainsKey(argName))
                    {
                        commandLineArgsDictionary.Add(argName, dicDefaultValues[argName]);
                    }
                }
            }

            // Return
            return commandLineArgsDictionary;
        }
    }
}

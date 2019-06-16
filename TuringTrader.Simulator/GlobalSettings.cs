//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        GlobalSettings
// Description: Global settings for simulator engine
// History:     2018x09, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region Libraries
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Class providing read/ write access to global settings, stored in the
    /// Windows registry.
    /// </summary>
    public static class GlobalSettings
    {
        #region static private RegistryKey OpenSubKey(string sub, bool writable = false)
        static private RegistryKey OpenSubKey(string sub, bool writable = false)
        {
            var assy = Assembly.GetEntryAssembly();
            var assyName = assy != null
                ? assy.GetName().Name
                : "TuringTrader"; // unit tests don't have an entry assembly

            string subKey = "Software"
                + "\\" + assyName
                + (sub.Length > 0 ? ("\\" + sub) : "")
                ;

            RegistryKey key = Registry.CurrentUser.OpenSubKey(subKey, writable);
            key = key ?? Registry.CurrentUser.CreateSubKey(subKey);

            return key;
        }
        #endregion
        #region static private object GetRegistryValue(string sub, string valueName)
        static private object GetRegistryValue(string sub, string valueName)
        {
            using (RegistryKey key = OpenSubKey(sub))
            {
                return key.GetValue(valueName);
            }
        }
        #endregion
        #region static private void SetRegistryValue(string sub, string valueName, object value)
        static private void SetRegistryValue(string sub, string valueName, object value)
        {
            using (RegistryKey key = OpenSubKey(sub, true))
            {
                key.SetValue(valueName, value);
            }
        }
        #endregion

        #region static public string HomePath
        /// <summary>
        /// Property to store the path to the TuringTrader's home location.
        /// </summary>
        static public string HomePath
        {
            get
            {
                object value = GetRegistryValue("SimulatorEngine", "HomePath");
                if (value == null) return "";
                return value.ToString();
            }
            set
            {
                SetRegistryValue("SimulatorEngine", "HomePath", value);
            }
        }
        #endregion
        #region static public string DataPath
        /// <summary>
        /// Property to store the path to the simulator's database.
        /// </summary>
        static public string DataPath
        {
            get
            {
                return Path.Combine(HomePath, "Data");
            }
        }
        #endregion
        #region static public string TemplatePath
        /// <summary>
        /// Property to store the path to the simulator's templates.
        /// </summary>
        static public string TemplatePath
        {
            get
            {
                return Path.Combine(HomePath, "Templates");
            }
        }
        #endregion
        #region static public string CachePath
        /// <summary>
        /// Property to store path to simulator's cache directory.
        /// </summary>
        static public string CachePath
        {
            get
            {
                return Path.Combine(HomePath, "Cache");
            }
        }
        #endregion
        #region static public string AlgorithmPath
        /// <summary>
        /// Property to store path to simulator's algorithm directory.
        /// </summary>
        static public string AlgorithmPath
        {
            get
            {
                return Path.Combine(HomePath, "Algorithms");
            }
        }
        #endregion

        #region static public string MostRecentAlgorithm
        /// <summary>
        /// Property returing the name of the most-recently run algorithm.
        /// </summary>
        static public string MostRecentAlgorithm
        {
            get
            {
                object value = GetRegistryValue("SimulatorEngine", "MostRecentAlgorithm");
                if (value == null) return null;
                return value.ToString();
            }
            set
            {
                SetRegistryValue("SimulatorEngine", "MostRecentAlgorithm", value);
            }
        }
        #endregion
        #region static public string DefaultRCore
        /// <summary>
        /// Default R-core, as found in HKLM/SOFTWARE/R-core/R
        /// </summary>
        static public string DefaultRCore
        {
            get
            {
                string subKey = "SOFTWARE\\R-core\\R";
                RegistryKey key = Registry.LocalMachine.OpenSubKey(subKey);

                if (key != null)
                {

                    // this key is used, when there is more than one R installation
                    string defaultRLocation = (string)key.GetValue("InstallPath");
                    if (defaultRLocation != null) return defaultRLocation;

                    // if there is only one, we need to look through the sub-keys
                    foreach (var rInstall in key.GetSubKeyNames())
                    {
                        RegistryKey rInstallKey = key.OpenSubKey(rInstall);
                        string rInstallLocation = (string)rInstallKey.GetValue("InstallPath");
                        if (rInstallLocation != null) return rInstallLocation;
                    }
                }

                throw new KeyNotFoundException("GlobalSettings: no R install found");
            }
        }
        #endregion
        #region static public string DefaultTemplateExtension
        /// <summary>
        /// Default file-extension for template files.
        /// </summary>
        static public string DefaultTemplateExtension
        {
            get
            {
                object value = GetRegistryValue("SimulatorEngine", "DefaultTemplateExtension");
                if (value == null) return ".cs";
                return value.ToString();
            }
            set
            {
                SetRegistryValue("SimulatorEngine", "DefaultTemplateExtension", value);
            }
        }
        #endregion
        #region static public bool AdjustForDividends
        private static bool _adjustQuotes = true;
        /// <summary>
        /// Enable/ disable quote adjustment for dividends. Default is true.
        /// Note that most data feeds (other than Norgate) cannot adjust for dividends.
        /// For these data feeds, this switch won't have any effect.
        /// </summary>
        static public bool AdjustForDividends
        {
            get
            {
                return _adjustQuotes;
            }
            set
            {
                _adjustQuotes = value;
            }
        }
        #endregion

        #region static public string DefaultDataFeed
        /// <summary>
        /// default data feed
        /// </summary>
        static public string DefaultDataFeed
        {
            get
            {
                object value = GetRegistryValue("SimulatorEngine", "DefaultDataFeed");
                if (value == null) return "Tiingo";
                return value.ToString();
            }
            set
            {
                SetRegistryValue("SimulatorEngine", "DefaultDataFeed", value);
            }
        }
        #endregion
        #region static public string TiingoApiKey
        /// <summary>
        ///  Tiingo API key
        /// </summary>
        static public string TiingoApiKey
        {
            get
            {
                object value = GetRegistryValue("SimulatorEngine", "TiingoApiKey");
                if (value == null) return "";
                return value.ToString();
            }
            set
            {
                SetRegistryValue("SimulatorEngine", "TiingoApiKey", value);
            }
        }
        #endregion
        #region static public string QuandlApiKey
        /// <summary>
        ///  Tiingo API key
        /// </summary>
        static public string QuandlApiKey
        {
            get
            {
                object value = GetRegistryValue("SimulatorEngine", "QuandlApiKey");
                if (value == null) return "";
                return value.ToString();
            }
            set
            {
                SetRegistryValue("SimulatorEngine", "QuandlApiKey", value);
            }
        }
        #endregion

        #region public static object GetRegistryValue(this SimulatorCore algo, string valueName, object defaultValue = null)
        /// <summary>
        /// Retrieve algorithm-specific registry value.
        /// </summary>
        /// <param name="algo">algorithm with which the value is associated</param>
        /// <param name="valueName">name of the value</param>
        /// <param name="defaultValue">default value, in case it has not been assigned</param>
        /// <returns></returns>
        public static object GetRegistryValue(this SimulatorCore algo, string valueName, object defaultValue = null)
        {
            object retValue = GetRegistryValue(algo.Name, valueName);

            if (retValue == null && defaultValue != null)
            {
                SetRegistryValue(algo.Name, valueName, defaultValue);
                retValue = defaultValue;
            }

            return retValue;
        }
        #endregion
        #region public static void SetRegistryValue(this SimulatorCore algo, string valueName, object value)
        /// <summary>
        /// Set algorithm-specific registry value.
        /// </summary>
        /// <param name="algo">algorithm with which the value is associated</param>
        /// <param name="valueName">name of the value</param>
        /// <param name="value">new value to be assigned</param>
        public static void SetRegistryValue(this SimulatorCore algo, string valueName, object value)
        {
            SetRegistryValue(algo.Name, valueName, value);
        }
        #endregion
    }
}

//==============================================================================
// end of file
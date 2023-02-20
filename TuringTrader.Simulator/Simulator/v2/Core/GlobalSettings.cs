//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        GlobalSettings
// Description: Global settings for simulator engine
// History:     2018x09, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
//              TuringTrader is free software: you can redistribute it and/or 
//              modify it under the terms of the GNU Affero General Public 
//              License as published by the Free Software Foundation, either 
//              version 3 of the License, or (at your option) any later version.
//              TuringTrader is distributed in the hope that it will be useful,
//              but WITHOUT ANY WARRANTY; without even the implied warranty of
//              MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//              GNU Affero General Public License for more details.
//              You should have received a copy of the GNU Affero General Public
//              License along with TuringTrader. If not, see 
//              https://www.gnu.org/licenses/agpl-3.0.
//==============================================================================

#region Libraries
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#endregion

namespace TuringTrader.SimulatorV2
{
    /// <summary>
    /// Class providing read/ write access to global settings, stored in the
    /// Windows registry.
    /// </summary>
    public static class GlobalSettings
    {
        #region internal stuff
        #region OpenSubKey
        static public RegistryKey OpenSubKey(string sub, bool writable = false)
        {
            var assy = Assembly.GetEntryAssembly();

            // make sure we have a proper assembly name.
            // testhost uses 'TuringTrader'
            var assyName = assy != null && assy.GetName().Name != "testhost"
                ? assy.GetName().Name
                : "TuringTrader";

            string subKey = "Software"
                + "\\" + assyName
                + (sub.Length > 0 ? ("\\" + sub) : "")
                ;

            RegistryKey key = Registry.CurrentUser.OpenSubKey(subKey, writable);
            key = key ?? Registry.CurrentUser.CreateSubKey(subKey);

            return key;
        }
        #endregion
        #region GetRegistryValue
        static public object GetRegistryValue(string sub, string valueName, object defaultValue = null)
        {
            using (RegistryKey key = OpenSubKey(sub))
            {
                var value = key.GetValue(valueName);

                if (value == null && defaultValue != null)
                {
                    SetRegistryValue(sub, valueName, defaultValue);
                    return defaultValue;
                }

                return value;
            }
        }
        #endregion
        #region SetRegistryValue
        static public void SetRegistryValue(string sub, string valueName, object value)
        {
            using (RegistryKey key = OpenSubKey(sub, true))
            {
                key.SetValue(valueName, value);
            }
        }
        #endregion
        #endregion

        #region Version
        /// <summary>
        /// Return version of simulation engine.
        /// </summary>
        static public string Version => GitInfo.Version
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace(" ", string.Empty);
        #endregion
        #region HomePath
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
        #region DataPath
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
        #region TemplatePath
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
        #region CachePath
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
        #region AlgorithmPath
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
        #region LoadAlgoDlls
        /// <summary>
        /// Enable/ disable loading of algorithms from external DLLs. Default is true.
        /// Note that this setting can only be made prior to accessing the
        /// AlgorithmLoader.
        /// </summary>
        static public bool LoadAlgoDlls { get; set; } = true;
        #endregion

        #region MostRecentAlgorithm
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
        #region DefaultRCore
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
        #region DefaultTemplateExtension
        /// <summary>
        /// Default file-extension for template files.
        /// </summary>
        static public string DefaultTemplateExtension
        {
            get => (string)GetRegistryValue("SimulatorEngine", "DefaultTemplateExtension", ".cs");
            set => SetRegistryValue("SimulatorEngine", "DefaultTemplateExtension", value);
        }
        #endregion
        #region ConsoleMode
        /// <summary>
        /// Display mode for console output.
        /// </summary>
        static public string ConsoleMode
        {
            get => (string)GetRegistryValue("SimulatorEngine", "DisplayMode", "errorsWarningsAndInfo");
            set => SetRegistryValue("SimulatorEngine", "DisplayMode", value);
        }
        #endregion

        #region DefaultDataFeed
        /// <summary>
        /// default data feed
        /// </summary>
        static public string DefaultDataFeed
        {
            get
            {
                object value = GetRegistryValue("SimulatorEngine", "DefaultDataFeed");
                if (value == null) return "Yahoo";
                return value.ToString();
            }
            set
            {
                SetRegistryValue("SimulatorEngine", "DefaultDataFeed", value);
            }
        }
        #endregion
        #region TiingoApiKey
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
        #region QuandlApiKey
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

        #region GetRegistryValue
        /// <summary>
        /// Retrieve algorithm-specific registry value.
        /// </summary>
        /// <param name="algo">algorithm with which the value is associated</param>
        /// <param name="valueName">name of the value</param>
        /// <param name="defaultValue">default value, in case it has not been assigned</param>
        /// <returns></returns>
        public static object GetRegistryValue(this Algorithm algo, string valueName, object defaultValue = null)
            => GetRegistryValue(algo.Name, valueName, defaultValue);
        #endregion
        #region SetRegistryValue
        /// <summary>
        /// Set algorithm-specific registry value.
        /// </summary>
        /// <param name="algo">algorithm with which the value is associated</param>
        /// <param name="valueName">name of the value</param>
        /// <param name="value">new value to be assigned</param>
        public static void SetRegistryValue(this Algorithm algo, string valueName, object value)
            => SetRegistryValue(algo.Name, valueName, value);
        #endregion
    }
}

//==============================================================================
// end of file
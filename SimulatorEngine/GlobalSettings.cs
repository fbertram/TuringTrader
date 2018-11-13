//==============================================================================
// Project:     Trading Simulator
// Name:        GlobalSettings
// Description: Global settings for simulator engine
// History:     2018x09, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region Libraries
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
            string subKey = "Software"
                + "\\" + Assembly.GetEntryAssembly().GetName().Name
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
                if (value == null) return null;
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
                return (string)key.GetValue("InstallPath");
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
                if (value == null) return ".r";
                return value.ToString();
            }
            set
            {
                SetRegistryValue("SimulatorEngine", "DefaultTemplateExtension", value);
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
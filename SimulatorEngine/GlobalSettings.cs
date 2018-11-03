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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
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

        #region static public string DataPath
        static public string DataPath
        {
            get
            {
                object value = GetRegistryValue("SimulatorEngine", "DataPath");
                if (value == null) return null;
                return value.ToString();
            }
            set
            {
                SetRegistryValue("SimulatorEngine", "DataPath", value);
            }
        }
        #endregion
        #region static public string MostRecentAlgorithm
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

        #region public static object GetRegistryValue(this SimulatorCore algo, string valueName, object defaultValue = null)
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
        public static void SetRegistryValue(this SimulatorCore algo, string valueName, object value)
        {
            SetRegistryValue(algo.Name, valueName, value);
        }
        #endregion
    }
}

//==============================================================================
// end of file
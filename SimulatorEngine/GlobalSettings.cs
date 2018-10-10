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

namespace FUB_TradingSim
{
    public static class GlobalSettings
    {
        static private RegistryKey OpenSubKey(bool writable = false)
        {
            string subKey = "Software"
                //+ "\\" + Assembly.GetEntryAssembly().GetName().Name
                + "\\" + Assembly.GetExecutingAssembly().GetName().Name;

            RegistryKey key = Registry.CurrentUser.OpenSubKey(subKey, writable);
            key = key ?? Registry.CurrentUser.CreateSubKey(subKey);

            return key;
        }

        static private object GetRegistryValue(string valueName)
        {
            using (RegistryKey key = OpenSubKey())
            {
                return key.GetValue(valueName);
            }
        }

        static private void SetRegistryValue(string valueName, object value)
        {
            using (RegistryKey key = OpenSubKey(true))
            {
                key.SetValue(valueName, value);
            }
        }

        static public string DataPath
        {
            get
            {
                object value = GetRegistryValue("DataPath");
                if (value == null) return null;
                return value.ToString();
            }
            set
            {
                SetRegistryValue("DataPath", value);
            }
        }

        static public string MostRecentAlgorithm
        {
            get
            {
                object value = GetRegistryValue("MostRecentAlgorithm");
                if (value == null) return null;
                return value.ToString();
            }
            set
            {
                SetRegistryValue("MostRecentAlgorithm", value);
            }
        }
    }
}

//==============================================================================
// end of file
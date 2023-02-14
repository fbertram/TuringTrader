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

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Class providing read/ write access to global settings, stored in the
    /// Windows registry.
    /// </summary>
    public class GlobalSettings : SimulatorV2.GlobalSettings
    {
        #region static public bool AdjustForDividends
        /// <summary>
        /// Enable/ disable quote adjustment for dividends. Default is true.
        /// Note that most data feeds (other than Norgate) cannot adjust for dividends.
        /// For these data feeds, this switch won't have any effect.
        /// </summary>
        static public bool AdjustForDividends { get; set; } = true;
        #endregion
        #region static public bool CacheDataGlobally
        /// <summary>
        /// Enable/ disable global caching of quote data. Default is true.
        /// </summary>
        static public bool CacheDataGlobally { get; set; } = true;
        #endregion
    }

    public static class GlobalSettingsExtensions
    {
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
            object retValue = GlobalSettings.GetRegistryValue(algo.Name, valueName);

            if (retValue == null && defaultValue != null)
            {
                GlobalSettings.SetRegistryValue(algo.Name, valueName, defaultValue);
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
            GlobalSettings.SetRegistryValue(algo.Name, valueName, value);
        }
        #endregion
    }
}

//==============================================================================
// end of file
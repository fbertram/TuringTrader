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
using System;
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
        /// <summary>
        /// Property to store the path to the TuringTrader's home location.
        /// </summary>
        static public string HomePath
        {
            get => SimulatorV2.GlobalSettings.HomePath;
            set => SimulatorV2.GlobalSettings.HomePath = value;
        }

        /// <summary>
        /// Property to store the path to the simulator's database.
        /// </summary>
        static public string DataPath
            => SimulatorV2.GlobalSettings.DataPath;

        /// <summary>
        /// Property to store the path to the simulator's templates.
        /// </summary>
        static public string TemplatePath
            => SimulatorV2.GlobalSettings.TemplatePath;


        /// <summary>
        /// Property to store path to simulator's cache directory.
        /// </summary>
        static public string CachePath
            => SimulatorV2.GlobalSettings.CachePath;

        /// <summary>
        /// Property to store path to simulator's algorithm directory.
        /// </summary>
        static public string AlgorithmPath
            => SimulatorV2.GlobalSettings.AlgorithmPath;

        /// <summary>
        /// Enable/ disable loading of algorithms from external DLLs. Default is true.
        /// Note that this setting can only be made prior to accessing the
        /// AlgorithmLoader.
        /// </summary>
        static public bool LoadAlgoDlls 
        {
            get => SimulatorV2.GlobalSettings.LoadAlgoDlls; 
            set => SimulatorV2.GlobalSettings.LoadAlgoDlls = value; 
        }
        
        /// <summary>
        /// Property returing the name of the most-recently run algorithm.
        /// </summary>
        static public string MostRecentAlgorithm
        {
            get => SimulatorV2.GlobalSettings.MostRecentAlgorithm;
            set => SimulatorV2.GlobalSettings.MostRecentAlgorithm = value;
        }

        /// <summary>
        /// Default R-core, as found in HKLM/SOFTWARE/R-core/R
        /// </summary>
        static public string DefaultRCore
            => SimulatorV2.GlobalSettings.DefaultRCore;

        /// <summary>
        /// Default file-extension for template files.
        /// </summary>
        static public string DefaultTemplateExtension
        {
            get => SimulatorV2.GlobalSettings.DefaultTemplateExtension;
            set => SimulatorV2.GlobalSettings.DefaultTemplateExtension = value;
        }

        /// <summary>
        /// specify display mode
        /// </summary>
        static public Output.DisplayModeType DisplayMode
        {
            get => (Output.DisplayModeType)Enum.Parse(typeof(Output.DisplayModeType), SimulatorV2.GlobalSettings.DisplayMode.ToString());
            set => SimulatorV2.GlobalSettings.DisplayMode = (SimulatorV2.Output.DisplayModeType)Enum.Parse(typeof(SimulatorV2.Output.DisplayModeType), value.ToString());
        }

        /// <summary>
        /// default data feed
        /// </summary>
        static public string DefaultDataFeed
        {
            get => SimulatorV2.GlobalSettings.DefaultDataFeed;
            set => SimulatorV2.GlobalSettings.DefaultDataFeed = value;
        }

        /// <summary>
        ///  Tiingo API key
        /// </summary>
        static public string TiingoApiKey
        {
            get => SimulatorV2.GlobalSettings.TiingoApiKey;
            set => SimulatorV2.GlobalSettings.TiingoApiKey = value;
        }

        /// <summary>
        ///  Quandl API key
        /// </summary>
        static public string QuandlApiKey
        {
            get => SimulatorV2.GlobalSettings.QuandlApiKey;
            set => SimulatorV2.GlobalSettings.QuandlApiKey = value;
        }

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
            object retValue = SimulatorV2.GlobalSettings.GetRegistryValue(algo.Name, valueName);

            if (retValue == null && defaultValue != null)
            {
                SimulatorV2.GlobalSettings.SetRegistryValue(algo.Name, valueName, defaultValue);
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
            SimulatorV2.GlobalSettings.SetRegistryValue(algo.Name, valueName, value);
        }
        #endregion
    }
}

//==============================================================================
// end of file
//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceNorgate
// Description: Data source for Norgate Data.
// History:     2022xi25, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
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

#define NO_REENTRY

#region libraries
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NDU = NorgateData.DataAccess;
#endregion

namespace TuringTrader.SimulatorV2
{
    public static partial class DataSource
    {
        #region Norgate DLL loading helpers
        private static class NorgateHelpers
        {
            private static bool _handleUnresolvedAssemblies = true;
            private static DateTime _lastNDURun = default(DateTime);
            private static object _lockUnresolved = new object();
            private static object _lockNDU = new object();

            public static void RunNDU(bool runAlways = false)
            {
                lock (_lockNDU)
                {
                    if (runAlways || DateTime.Now - _lastNDURun > TimeSpan.FromMinutes(5))
                    {
                        _lastNDURun = DateTime.Now;

                        string nduBinPath;
                        getNDUBinPath(out nduBinPath);
                        string nduTrigger = Path.Combine(nduBinPath, "NDU.Trigger.exe");

                        Process ndu = Process.Start(nduTrigger, "UPDATE CLOSE WAIT");
                        ndu.WaitForExit();
                    }
                }
            }
            public static void HandleUnresovledAssemblies()
            {
                lock (_lockUnresolved)
                {
                    if (_handleUnresolvedAssemblies)
                    {
                        _handleUnresolvedAssemblies = false;

                        AppDomain currentDomain = AppDomain.CurrentDomain;
                        currentDomain.AssemblyResolve += currentDomain_AssemblyResolve;
                    }
                }
            }
            public static Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
            {
                string actualassemblyname = new AssemblyName(args.Name).Name;

                if (actualassemblyname == "norgate.data.dotnet" && isAPIAvaliable)
                {
                    Assembly assembly = Assembly.LoadFrom(actualAPILocation);

                    return assembly;
                }

                return null;  // this is bad, means the API could not be loaded and will probably throw a program wide exception 
            }

            public static bool isNDUInstalled
            {
                get { return checkForNDUInstallKey(); }
            }
            public static string actualAPILocation
            {
                get
                {
                    string result = "";
                    if (isNDUInstalled)
                    {
                        // get the registry information for NDU BIN path
                        string nduBinPath = "";
                        if (getNDUBinPath(out nduBinPath))
                        {
                            if (File.Exists(nduBinPath + "norgate.data.dotnet.dll"))
                            {
                                result = nduBinPath + "norgate.data.dotnet.dll";
                            }
                        }
                    }
                    return result;
                }
            }
            public static bool isAPIAvaliable
            {
                get
                {
                    if (File.Exists(actualAPILocation))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            public static bool getNDUBinPath(out string binPath)
            {
                bool result = false;
                binPath = "";
                try
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\NDU\\norgate");
                    if (key != null)
                    {
                        Object o = key.GetValue("basePath");
                        if (o != null)
                        {
                            string work = (string)o;
                            work += "bin\\";
                            if (Directory.Exists(work))
                            {
                                binPath = work;
                                result = true;
                            }
                        }
                        else
                        {
                            result = false;
                        }
                    }
                }
                catch
                {
                    result = false;
                }
                return result;
            }
            public static bool checkForNDUInstallKey()
            {
                bool result = false;
                try
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\NDU\\norgate");
                    if (key != null)
                    {
                        Object o = key.GetValue("nduInstalled");
                        if (o != null)
                        {
                            int isInstalled = (int)o;
                            if (isInstalled != 1)
                            {
                                // not installed
                                result = false;
                            }
                            else
                            {
                                // is installed (at least registry believes so)
                                result = true;
                            }
                        }
                    }
                }
                catch
                {
                    result = false;
                }
                return result;
            }
        }
        #endregion
        #region internal data & helpers
        private static object _lockReentrance = new object();
        #endregion

        private static void InitNorgateFeed()
        {
            NorgateHelpers.HandleUnresovledAssemblies();
        }

        private static List<BarType<OHLCV>> LoadNorgateData(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            var tradingDays = algo.TradingCalendar.TradingDays;
            var startDate = tradingDays.First();
            var endDate = tradingDays.Last();

#if NO_REENTRY
            lock (_lockReentrance)
#endif
            {
                if (!NorgateHelpers.isAPIAvaliable)
                    throw new Exception("Norgate Data Updater not installed");

                //--- Norgate setup
                NDU.Api.SetAdjustmentType = NDU.AdjustmentType.TotalReturn;
                NDU.Api.SetPaddingType = NDU.PaddingType.AllMarketDays;

                //--- run NDU as required
#if false
                // this should work, but seems broken as of 01/09/2019
                // confirmed broken 12/25/2022
                DateTime dbTimeStamp = NDU.Api.LastDatabaseUpdateTime;
#else
                var exchangeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(info[DataSourceParam.timezone]);
                var timeOfDay = DateTime.Parse(info[DataSourceParam.time]).TimeOfDay;

                List<NDU.RecOHLC> q = new List<NDU.RecOHLC>();
                NDU.Api.GetData("$SPX", out q, DateTime.Now - TimeSpan.FromDays(5), DateTime.Now + TimeSpan.FromDays(5));
                DateTime dbLastQuote = q
                    .Select(ohlc => ohlc.Date)
                    .OrderByDescending(d => d)
                    .First()
                    .Date + timeOfDay;

                var dbTimeStamp = TimeZoneInfo.ConvertTimeToUtc(dbLastQuote, exchangeTimeZone).ToLocalTime();
#endif

                if (endDate > dbTimeStamp)
                    NorgateHelpers.RunNDU();

                //--- retrieve data from Norgate
                List<NDU.RecOHLC> norgateData = new List<NDU.RecOHLC>();
                NDU.OperationResult result = NDU.Api.GetData(info[DataSourceParam.symbolNorgate], out norgateData, startDate, endDate);

                //--- copy to TuringTrader bars
                var bars = new List<BarType<OHLCV>>();
                //var exchangeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(info[DataSourceParam.timezone]);
                //var timeOfDay = DateTime.Parse(info[DataSourceParam.time]).TimeOfDay;

                foreach (var ohlcv in norgateData)
                {
                    // Norgate bars only have dates, no time.
                    // We add the time from the data source descriptor,
                    // and convert it to the local timezone.
                    var dateTimeAtExchange = ohlcv.Date.Date + timeOfDay;
                    var dateTimeLocal = TimeZoneInfo.ConvertTimeToUtc(dateTimeAtExchange, exchangeTimeZone).ToLocalTime();

                    bars.Add(new BarType<OHLCV>(
                        dateTimeLocal,
                        new OHLCV(
                            (double)ohlcv.Open,
                            (double)ohlcv.High,
                            (double)ohlcv.Low,
                            (double)ohlcv.Close,
                            (double)ohlcv.Volume)));
                }

                return bars;
            }
        }

        private static TimeSeriesAsset.MetaType LoadNorgateMeta(Algorithm algo, Dictionary<DataSourceParam, string> info)
        {
            //var makeSureWeLoadNorgateDll = new NorgateLoaderObject();

#if NO_REENTRY
            lock (_lockReentrance)
#endif
            {

                var ticker = info[DataSourceParam.symbolNorgate];
                var meta = new TimeSeriesAsset.MetaType
                {
                    Ticker = ticker,
                    Description = NDU.Api.GetSecurityName(ticker),
                };

                return meta;
            }
        }
    }
}

//==============================================================================
// end of file

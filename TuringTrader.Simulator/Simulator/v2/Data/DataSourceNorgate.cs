//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceNorgate
// Description: Data source for Norgate Data.
// History:     2022xi25, FUB, created
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
using NDW = NorgateData.WatchListLibrary;
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

        private class _norgateUniverse
        {
            private Algorithm _algorithm;
            private string _universe;

            private static Dictionary<string, string> _watchlistNames = new Dictionary<string, string>()
            {
                { "$OEX", "S&P 100 Current & Past" },
                { "$SPX", "S&P 500 Current & Past"},
                { "$MID", "S&P MidCap 400 Current & Past" },
                { "$SML", "S&P SmallCap 600 Current & Past" },
                { "$SP1500", "S&P Composite 1500 Current & Past" },
                { "$SPDAUDP", "S&P 500 Dividend Aristocrats Current & Past" },

                { "$RUI", "Russell 1000 Current & Past" },
                { "$RUT", "Russell 2000 Current & Past" },
                { "$RUA", "Russell 3000 Current & Past" },
                { "$RMC", "Russell Mid Cap Current & Past" },
                { "$RUMIC", "Russell Micro Cap Current & Past" },

                { "$NDX", "NASDAQ 100 Current & Past" },
                { "$NGX", "Nasdaq Next Generation 100 Current & Past" },
                { "$NXTQ", "Nasdaq Q-50 Current & Past" },
                { "$DJI", "Dow Jones Industrial Average Current & Past" },
            };
            private NDW.Watchlist _watchlist;
            private NDW.SecurityList _securityList;
            private Dictionary<int, List<NDU.RecIndicator>> _constituency = new Dictionary<int, List<NDU.RecIndicator>>();
            private Dictionary<int, int> _constituencyIndex = new Dictionary<int, int>();
            private DateTime _previousLocalClose = default;

            public _norgateUniverse(Algorithm algo, string universe)
            {
                _algorithm = algo;
                _universe = universe;

                if (!_watchlistNames.ContainsKey(universe))
                    throw new Exception(String.Format("Unknown universe {0}:{1}", "norgate", universe));

#if NO_REENTRY
                lock (_lockReentrance)
#endif
                {

                    NDU.OperationResult success;
                    // get watchlist object
                    success = NDU.Api.GetWatchlist(_watchlistNames[universe], out _watchlist);

                    // get all securities on that watchlist
                    var allSecurities = (NDW.SecurityList)null;
                    success = _watchlist.GetSecurityList(out allSecurities);

                    // get constituency time series
                    _securityList = new NDW.SecurityList();
                    foreach (var security in allSecurities)
                    {
                        var rawTimeSeries = (List<NDU.RecIndicator>)null;
                        success = NDU.Api.GetIndexConstituentTimeSeries(
                            security.AssetID, out rawTimeSeries, _universe,
                            TimeZoneInfo.ConvertTime((DateTime)_algorithm.StartDate - TimeSpan.FromDays(5), _algorithm.TradingCalendar.ExchangeTimeZone).Date,
                            TimeZoneInfo.ConvertTime((DateTime)_algorithm.EndDate, _algorithm.TradingCalendar.ExchangeTimeZone).Date,
                            NDU.PaddingType.AllCalendarDays);

                        // NOTE: the constituency time series occupy a lot of memory.
                        //       we compress the data here by removing unnecessary
                        //       series and time stamps.
                        if (rawTimeSeries.Count > 0)
                        {
                            // NOTE: constituency time series may apruptly end with a '1'.
                            //       When we evaluate this series later, this leads to the
                            //       asset being stuck. To prevent this, we add a '0' at
                            //       the end of the series.
                            rawTimeSeries.Add(new NDU.RecIndicator
                            {
                                Date = rawTimeSeries.Last().Date + TimeSpan.FromDays(1),
                                value = 0,
                            });

                            var prevValue = (double?)47.11;
                            var timeSeries = new List<NDU.RecIndicator>();
                            foreach (var t in rawTimeSeries)
                            {
                                if (t.value != prevValue)
                                {
                                    prevValue = t.value;
                                    timeSeries.Add(t);
                                }
                            }

                            _constituency[security.AssetID] = timeSeries;
                            _securityList.Add(security);

                        }
                    }
                }
            }

            public HashSet<string> Constituents()
            {
                var localClose = _algorithm.SimDate;
                var exchangeTime = TimeZoneInfo.ConvertTime(localClose, _algorithm.TradingCalendar.ExchangeTimeZone);

                // reset time series
                // NOTE: this should only happen when the simloop is reentered,
                //       most likely through the use of a lambda-function indicator.
                if (_previousLocalClose == default || localClose < _previousLocalClose)
                {
                    foreach (var security in _securityList)
                        _constituencyIndex[security.AssetID] = 0;
                }
                _previousLocalClose = localClose;

                // advance time series
                foreach (var id in _securityList.Select(s => s.AssetID))
                {
                    while (_constituencyIndex[id] < _constituency[id].Count - 1
                    && _constituency[id][_constituencyIndex[id] + 1].Date < exchangeTime.Date)
                    {
                        _constituencyIndex[id]++;
                    }
                }

                // collect constituents
                var constituents = new HashSet<string>();
                foreach (var security in _securityList)
                {
                    var id = security.AssetID;
                    if (_constituency[id][_constituencyIndex[id]].Date <= exchangeTime
                    && _constituency[id][_constituencyIndex[id]].value != 0)
                    {
                        constituents.Add("norgate:" + security.Symbol);
                    }
                }

                return constituents;
            }
        }
        #endregion

        private static void NorgateInit()
        {
            NorgateHelpers.HandleUnresovledAssemblies();
        }

        private static List<BarType<OHLCV>> NorgateLoadData(Algorithm owner, Dictionary<DataSourceParam, string> info)
        {
            var tradingDays = owner.TradingCalendar.TradingDays;
            var startDate = tradingDays.First();
            var endDate = tradingDays
                .Where(t => t <= DateTime.Now)
                .Last();

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

                if (!result.IsSuccess())
                    throw new Exception(string.Format("Failed to load data for {0} from Norgate: {1}", info[DataSourceParam.symbolNorgate], result.ErrorMessage));

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

        private static TimeSeriesAsset.MetaType NorgateLoadMeta(Algorithm owner, Dictionary<DataSourceParam, string> info)
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

        private static Tuple<List<BarType<OHLCV>>, TimeSeriesAsset.MetaType> NorgateGetAsset(Algorithm owner, Dictionary<DataSourceParam, string> info)
        {
            return Tuple.Create(
                NorgateLoadData(owner, info),
                NorgateLoadMeta(owner, info));
        }

        private static HashSet<string> NorgateGetUniverse(Algorithm owner, string universe)
        {
            NorgateInit();

            var theUniverse = owner.ObjectCache.Fetch(
                string.Format("Universe({0})", universe),
                () => new _norgateUniverse(owner, universe));

            return theUniverse.Constituents();
        }
    }
}

//==============================================================================
// end of file

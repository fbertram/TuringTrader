﻿//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceNorgate
// Description: Data source for Norgate Data.
//              Tested w/ Norgate Data API 4.1.5.27.
// History:     2019i06, FUB, created.
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

#region libraries
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NDU = NorgateData.DataAccess;
using NDW = NorgateData.WatchListLibrary;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataSourceCollection
    {
        #region Norgate DLL loading helpers
        private static class NorgateHelpers
        {
            private static bool _handleUnresolvedAssemblies = true;
            private static DateTime _lastNDURun = default(DateTime);
            private static object _lockNorgate = SimulatorV2.DataSource._lockNorgate;

            public static void RunNDU(bool runAlways = false)
            {
                lock (_lockNorgate)
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
                lock (_lockNorgate)
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
        #region public static void RunNDU()
        /// <summary>
        /// Run Norgate Data Updater.
        /// </summary>
        public static void RunNDU()
        {
            NorgateHelpers.RunNDU(true);
        }
        #endregion

        private class DataSourceNorgate : DataSource
        {
            #region internal data
            private static object _lockNorgate = SimulatorV2.DataSource._lockNorgate; // new object();
            #endregion
            #region internal helpers
            private void SetName()
            {
                lock (_lockNorgate)
                {
                    // no proper name given, try to retrieve from Norgate
                    string ticker = Info[DataSourceParam.symbolNorgate];
                    Info[DataSourceParam.name] = NDU.Api.GetSecurityName(ticker);
                }
            }
            private Bar CreateBar(NDU.RecOHLC norgate, double priceMultiplier)
            {
                DateTime barTime = norgate.Date.Date
                    + DateTime.Parse(Info[DataSourceParam.time]).TimeOfDay;

                return new Bar(
                                Info[DataSourceParam.ticker], barTime,
                                (double)norgate.Open * priceMultiplier,
                                (double)norgate.High * priceMultiplier,
                                (double)norgate.Low * priceMultiplier,
                                (double)norgate.Close * priceMultiplier,
                                (long)norgate.Volume, true,
                                0.0, 0.0, 0, 0, false,
                                default(DateTime), 0.0, false);
            }
            private void LoadData(List<Bar> data, DateTime startTime, DateTime endTime)
            {
                lock (_lockNorgate)
                {
                    if (!NorgateHelpers.isAPIAvaliable)
                        throw new Exception(string.Format("{0}: Norgate Data Updater not installed", GetType().Name));

                    //--- Norgate setup
                    NDU.Api.SetAdjustmentType = GlobalSettings.AdjustForDividends
                        ? NDU.AdjustmentType.TotalReturn
                        : NDU.AdjustmentType.CapitalSpecial;
                    NDU.Api.SetPaddingType = NDU.PaddingType.AllMarketDays;

                    //--- run NDU as required
                    NDU.OperationResult result;
#if false
                    // this should work, but seems broken as of 01/09/2019
                    // confirmed broken as of 02/10/2023
                    DateTime dbTimeStamp = NDU.Api.LastDatabaseUpdateTime;
#else
                    List<NDU.RecOHLC> q = new List<NDU.RecOHLC>();
                    result = NDU.Api.GetData("$SPX", out q, DateTime.Now - TimeSpan.FromDays(5), DateTime.Now + TimeSpan.FromDays(5));

                    //if (!result.IsSuccess())
                    //    Output.ThrowError("failed to retrieve data for {0}: {1}", "$SPX", result.ErrorMessage);

                    DateTime dbTimeStamp = q.Count > 0
                        ? q
                            .Select(ohlc => ohlc.Date)
                            .OrderByDescending(d => d)
                            .First()
                        : default(DateTime);
#endif

                    if (endTime > dbTimeStamp)
                        NorgateHelpers.RunNDU();

                    //--- get data from Norgate
                    List<NDU.RecOHLC> norgateData = new List<NDU.RecOHLC>();
                    result = NDU.Api.GetData(Info[DataSourceParam.symbolNorgate], out norgateData, startTime, endTime);

                    if (!result.IsSuccess())
                        Output.ThrowError("failed to retrieve data for {0}: {1}", Info[DataSourceParam.symbolNorgate], result.ErrorMessage);

                    //--- copy to TuringTrader bars
                    double priceMultiplier = Info.ContainsKey(DataSourceParam.dataUpdaterPriceMultiplier)
                        ? Convert.ToDouble(Info[DataSourceParam.dataUpdaterPriceMultiplier])
                        : 1.0;

                    foreach (var ohlc in norgateData)
                    {
                        // Norgate bars only have dates, no time.
                        // we need to make sure that we won't return bars
                        // outside of the requested range, as otherwise
                        // the simulator's IsLastBar will be incorrect
                        Bar bar = CreateBar(ohlc, priceMultiplier);

                        if (bar.Time >= startTime && bar.Time <= endTime)
                            data.Add(bar);
                    }
                }
            }
            #endregion

            //---------- API
            #region public DataSourceNorgate(Dictionary<DataSourceValue, string> info)
            /// <summary>
            /// Create and initialize new data source for Norgate Data.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceNorgate(Dictionary<DataSourceParam, string> info) : base(info)
            {
                // make sure Norgate api is properly loaded
                NorgateHelpers.HandleUnresovledAssemblies();

                if (info[DataSourceParam.name] == info[DataSourceParam.nickName])
                {
                    // no proper name given, try to retrieve from Norgate
                    SetName();
                }
            }
            #endregion
            #region public override IEnumerable<Bar> LoadData(DateTime startTime, DateTime endTime)
            /// <summary>
            /// Load data into memory.
            /// </summary>
            /// <param name="startTime">start of load range</param>
            /// <param name="endTime">end of load range</param>
            public override IEnumerable<Bar> LoadData(DateTime startTime, DateTime endTime)
            {
                var cacheKey = new CacheId().AddParameters(
                    Info[DataSourceParam.nickName].GetHashCode(),
                    startTime.GetHashCode(),
                    endTime.GetHashCode());

                List<Bar> retrievalFunction()
                {
                    //DateTime t1 = DateTime.Now;
                    Output.WriteInfo(string.Format("{0}: loading data for {1}...", GetType().Name, Info[DataSourceParam.nickName2]));

                    List<Bar> bars = new List<Bar>();

                    LoadData(bars, startTime, endTime);

                    //DateTime t2 = DateTime.Now;
                    //Output.ShowInfo(string.Format(" finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                    return bars;
                };

                List<Bar> data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction, true);

                if (data.Count == 0 && !Info[DataSourceParam.dataFeed].ToLower().Contains("accept_no_data"))
                    throw new Exception(string.Format("{0}: no data for {1}", GetType().Name, Info[DataSourceParam.nickName2]));

                CachedData = data;
                return data;
            }
            #endregion
        }

        private class UniverseNorgate : Universe
        {
            #region internal data
            private static object _lockNorgate = SimulatorV2.DataSource._lockNorgate;
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
            private string _nickname;
            // Norgate dll is loaded *while* first instance is created
            private object /*NDW.Watchlist*/ _watchlist;
            private object /*NDW.SecurityList*/ _constituents;
            private Dictionary<int, object/*List<NDU.RecIndicator>*/> _constituentsTimeSeries = new Dictionary<int, object>();
            private Dictionary<int, int> _consituentsTimeSeriesIndex = new Dictionary<int, int>();
            #endregion
            #region internal helpers
            private void getWatchlist()
            {
                // TODO: it seems that Norgate's code might not be multi-threaded
                //       this mutex here avoids issues with GetWatchlist returning
                //       a null-pointer for the out-parameter watchlist,
                //       when run in the optimizer.
                lock (_lockNorgate)
                {
                    // this code cannot be in the same method
                    // that calls HandleUnresolvedAssemblies
                    NDW.Watchlist watchlist;
                    var success = NDU.Api.GetWatchlist(_watchlistNames[_nickname], out watchlist);
                    _watchlist = watchlist;
                }
            }
            #endregion

            #region public UniverseNorgate(string nickname)
            public UniverseNorgate(string nickname)
            {
                // make sure Norgate api is properly loaded
                NorgateHelpers.HandleUnresovledAssemblies();

                if (!_watchlistNames.ContainsKey(nickname))
                    throw new Exception(string.Format("{0}: no watchlist found for '{1}'", GetType().Name, nickname));

                _nickname = nickname;
                getWatchlist();
            }
            #endregion

            //---------- API
            #region abstract public IEnumerable<string> Constituents()
            /// <summary>
            /// Return universe constituents.
            /// </summary>
            /// <returns>enumerable with nicknames</returns>
            override public IEnumerable<string> Constituents
            {
                get
                {
                    NDW.Watchlist watchlist = (NDW.Watchlist)_watchlist;

                    if (_constituents == null)
                    {
                        lock (_lockNorgate)
                        {
                            NDW.SecurityList constituents1;
                            var result = watchlist.GetSecurityList(out constituents1);

                            if (!result.IsSuccess())
                                Output.ThrowError("failed to retrieve securities list for {0}: {1}", _nickname, result.ErrorMessage);

                            _constituents = constituents1;
                        }
                    }

                    NDW.SecurityList constituents = (NDW.SecurityList)_constituents;
                    foreach (var security in constituents)
                    {
                        yield return "norgate#accept_no_data:" + security.Symbol;
                    }

                    yield break;
                }
            }
            #endregion
            #region abstract public bool IsConstituent(string nickname, DateTime timestamp);
            /// <summary>
            /// Determine if instrument is constituent of universe.
            /// </summary>
            /// <param name="nickname">nickname of instrument to look for</param>
            /// <param name="timestamp">timestamp to check</param>
            /// <returns>true, if constituent of universe</returns>
            override public bool IsConstituent(string nickname, DateTime timestamp)
            {
                int idx = nickname.Contains(":") ? nickname.IndexOf(":") : -1;
                string datasource = idx > 0 ? nickname.Substring(0, idx - 1).ToLower() : "";
                string symbol = nickname.Substring(idx + 1);

                if (datasource.Length > 0 && !datasource.Contains("norgate"))
                    return false;

                if (_constituents == null)
                {
                    var dummy = Constituents.First();
                }
                NDW.SecurityList constituents = (NDW.SecurityList)_constituents;
                NDW.Security security = constituents
                    .Where(c => c.Symbol == symbol)
                    .FirstOrDefault();

                if (security == null)
                    return false;

                if (!_constituentsTimeSeries.ContainsKey(security.AssetID))
                {
                    lock (_lockNorgate)
                    {
                        List<NDU.RecIndicator> timeSeries1;
                        var result = NDU.Api.GetIndexConstituentTimeSeries(
                            security.AssetID, out timeSeries1, _nickname,
                            DateTime.Parse("01/01/1990", CultureInfo.InvariantCulture),
                            DateTime.Now.Date,
                            NDU.PaddingType.None);

                        if (!result.IsSuccess())
                            Output.ThrowError("failed to retrieve constituent time series for {0}: {1}", security.Symbol, result.ErrorMessage);

                        _constituentsTimeSeries[security.AssetID] = timeSeries1;
                    }
                }
                List<NDU.RecIndicator> timeSeries = (List<NDU.RecIndicator>)_constituentsTimeSeries[security.AssetID];

                // no index? set to zero
                int index = _consituentsTimeSeriesIndex.ContainsKey(security.AssetID)
                    ? _consituentsTimeSeriesIndex[security.AssetID]
                    : 0;

                // index too far? reset to zero
                if (timeSeries[index].Date.Date > timestamp.Date)
                    index = 0;

                // advance index as required
                while (index < timeSeries.Count - 1
                && timeSeries[Math.Min(index + 1, timeSeries.Count - 1)].Date.Date < timestamp.Date)
                    index++;

                // cache this index to speed up next iteration
                _consituentsTimeSeriesIndex[security.AssetID] = index;

                // reached end of indices
                // this shouldn't matter, as the simulator core removes stale instruments
                if (timeSeries[index].Date.Date < timestamp.Date && index == timeSeries.Count - 1)
                    return false;

                return timeSeries[index].value != 0.0 ? true : false;
            }
            #endregion
        }
    }

}

//==============================================================================
// end of file
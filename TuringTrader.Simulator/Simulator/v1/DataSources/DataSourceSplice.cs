//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceSplice
// Description: Data source to splice multiple data sources.
// History:     2019ix26, FUB, created
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
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataSourceCollection
    {
        class DataSourceSplice : DataSource
        {
            #region internal data
            private List<string> _symbols = null;
            #endregion

            #region public DataSourceSplice(Dictionary<DataSourceValue, string> info)
            /// <summary>
            /// Create and initialize new splicing data source.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceSplice(Dictionary<DataSourceParam, string> info) : base(info)
            {
                if (!Info.ContainsKey(DataSourceParam.symbolSplice))
                    throw new Exception(string.Format("{0}: {1} missing mandatory symbolSplice key", GetType().Name, info[DataSourceParam.nickName]));

                _symbols = info[DataSourceParam.symbolSplice].Split(",").ToList();

#if true
                string name = null;
                string ticker = null;
                foreach (var nick in _symbols)
                {
                    var d = DataSource.New(nick);

                    name = name ?? d.Info[DataSourceParam.name];
                    ticker = ticker ?? d.Info[DataSourceParam.ticker] + ".";
                }

                Info[DataSourceParam.name] = name;
                Info[DataSourceParam.ticker] = ticker;
#endif
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
                    DateTime t1 = DateTime.Now;
                    //Output.Write(string.Format("{0}: loading data for {1}...", GetType().Name, Info[DataSourceParam.nickName]));

                    // load data from specified data sources
                    // and save as list of bars, in reverse order
                    Dictionary<string, List<Bar>> dsBars = new Dictionary<string, List<Bar>>();
                    foreach (var nick in _symbols)
                    {
                        var d = DataSource.New(nick);

                        try
                        {
                            var data = d.LoadData(startTime, endTime);
                            dsBars[nick] = data.Reverse().ToList();
                        }
                        catch (Exception /*e*/)
                        {
                            Output.WriteLine("{0}: {1} failed to load {2}", this.GetType().Name, Info[DataSourceParam.nickName], nick);

                            // add an empty list, if need be
                            // this will be ignored further down during splicing
                            if (!dsBars.ContainsKey(nick))
                                dsBars[nick] = new List<Bar>();
                        }

                        //Output.WriteLine("{0}: {1} data range {2:MM/dd/yyyy}, {3:MM/dd/yyyy}", GetType().Name, nick, d.FirstTime, d.LastTime);
                    }

                    // create enumerators for all data sources
                    Dictionary<string, IEnumerator<Bar>> dsEnums = new Dictionary<string, IEnumerator<Bar>>();
                    Dictionary<string, bool> dsHasData = new Dictionary<string, bool>();
                    foreach (var nick in _symbols)
                    {
                        dsEnums[nick] = dsBars[nick].GetEnumerator();
                        dsHasData[nick] = dsEnums[nick].MoveNext();
                    }

                    // skip bars from all proxy datasources, so that no proxy
                    // has bars after the primary datasource
                    // example: extending GLD w/ XAUUSD. because XAUUSD has
                    // a different trading calendar, XAUUSD might have bars
                    // after GLD, leading to faulty results
                    var lastPrimary = dsEnums[_symbols.First()].Current.Time;

                    foreach (var nick in _symbols)
                    {
                        while (dsHasData[nick] && dsEnums[nick].Current.Time > lastPrimary)
                            dsHasData[nick] = dsEnums[nick].MoveNext();
                    }

                    // collect bars
                    List<Bar> bars = new List<Bar>();

                    Dictionary<string, double?> dsScale = new Dictionary<string, double?>();
                    _symbols.ForEach(n => dsScale[n] = null);
                    dsScale[_symbols.First()] = 1.0;

                    while (dsHasData.Values.Aggregate((a, b) => a || b))
                    {
                        // find most-recent timestamp
                        DateTime ts = _symbols
                            .Where(n => dsHasData[n])
                            .Select(n => dsEnums[n].Current.Time)
                            .Max(t => t);

                        Bar bar = null;

                        foreach (var nick in _symbols)
                        {
                            // no data: continue
                            if (!dsHasData[nick])
                                continue;

                            // older bar: continue
                            if (dsEnums[nick].Current.Time < ts)
                                continue;

                            if (bar == null)
                            {
                                // highest priority bar
                                Bar rawBar = dsEnums[nick].Current;

                                // we might get here, with dsScale not set yet.
                                // this is the best we can do to fix things
                                if (dsScale[nick] == null)
                                    dsScale[nick] = bars.Last().Open / rawBar.Close;

                                double open = rawBar.Open * (double)dsScale[nick];
                                double high = rawBar.High * (double)dsScale[nick];
                                double low = rawBar.Low * (double)dsScale[nick];
                                double close = rawBar.Close * (double)dsScale[nick];
                                long volume = 0;

                                bar = Bar.NewOHLC(Info[DataSourceParam.ticker], ts, open, high, low, close, volume);

                                bars.Add(bar);
                            }
                            else
                            {
                                // lower priority bars
                                Bar rawBar = dsEnums[nick].Current;

                                List<double> scales = new List<double>
                                {
                                    bar.Open / rawBar.Open,
                                    bar.High / rawBar.High,
                                    bar.Low / rawBar.Low,
                                    bar.Close / rawBar.Close,
                                };

                                dsScale[nick] = scales.Average();
                            }

                            dsHasData[nick] = dsEnums[nick].MoveNext();
                        }
                    }

                    // reverse order of bars
                    bars.Reverse();

                    DateTime t2 = DateTime.Now;
                    //Output.WriteLine(string.Format(" finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                    return bars;
                };

                List<Bar> data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction, true);

                if (data.Count == 0)
                    throw new Exception(string.Format("{0}: no data for {1}", GetType().Name, Info[DataSourceParam.nickName]));

                CachedData = data;
                return data;
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file
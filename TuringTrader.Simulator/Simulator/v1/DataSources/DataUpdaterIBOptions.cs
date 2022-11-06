//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataUpdaterIBOptions
// Description: Option data updater, Interactive Brokers
// History:     2018x19, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
//              https://www.bertram.solutions
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

#region libraries
using System;
using System.Collections.Generic;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataUpdaterCollection
    {
#if true
        private class DataUpdaterIBOptions : DataUpdater
        {
            public override string Name => "InteractiveBrokers";

            public DataUpdaterIBOptions(SimulatorCore simulator, Dictionary<DataSourceParam, string> info) : base(simulator, info)
            {

            }
            override public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime)
            {
                throw new Exception("Interactive Brokers download currently broken, we're working on it. Use Tiingo instead.");
            }
        }
#else
        /// <summary>
        /// Data updater for Interactive Brokers option prices
        /// </summary>
        private class DataUpdaterIBOptions : DataUpdater
        {
        #region internal data
        #endregion
        #region internal helpers
        #endregion

        #region public DataUpdaterIBOptions(SimulatorCore simulator, Dictionary<DataSourceValue, string> info) : base(simulator, info)
            /// <summary>
            /// Create and initialize data updater.
            /// </summary>
            /// <param name="simulator">parent simulator</param>
            /// <param name="info">info dictionary</param>
            public DataUpdaterIBOptions(SimulatorCore simulator, Dictionary<DataSourceValue, string> info) : base(simulator, info)
            {
            }
        #endregion

        #region override IEnumerable<Bar> void UpdateData(DateTime startTime, DateTime endTime)
            /// <summary>
            /// Run data update.
            /// </summary>
            /// <param name="startTime">start of update range</param>
            /// <param name="endTime">end of update range</param>
            /// <returns>enumerable with updated bars</returns>
            override public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime)
            {
                string twsUser = (string)Simulator.GetRegistryValue("TwsUser", "");
                string twsPass = (string)Simulator.GetRegistryValue("TwsPass", "");
                int twsPort = (int)Simulator.GetRegistryValue("TwsPort", 7497);

                BrokerClientIB ibClient = new BrokerClientIB();
                DateTime timestamp1 = DateTime.Now;

                try
                {
                    ibClient.Connect(twsUser, twsPass, twsPort);

                    string[] mapping = Info[DataSourceValue.symbolInteractiveBrokers].Split(',');
                    string symbol = mapping[0];
                    string type = mapping.Count() >= 2 ? mapping[1] : "STK";
                    string exchange = mapping.Count() >= 3 ? mapping[2] : "SMART";
                    string currency = mapping.Count() >= 4 ? mapping[3] : "USD";

                    //----- get option chain
                    var optionChain = ibClient.ContractDetails(symbol, type, exchange, currency);
                    Output.Write("found {0} contracts...", optionChain.Count());

                    //----- multi-threaded bid/ask price query
                    MTJobQueue mtJobQueue = new MTJobQueue(10);
                    int num = 0;
                    foreach (var option in optionChain)
                    {
                        mtJobQueue.QueueJob(() =>
                        {
                            try
                            {
                                double bid = option.Bid;
                                double ask = option.Ask;
                                lock ((object)num)
                                {
                                    if (++num % 1000 == 0) Output.Write("|");
                                    else if (num % 100 == 0) Output.Write(".");
                                }
                            }
                            catch
                            {
                                option.Bid = 0.0;
                                option.Ask = 0.0;
                            }
                        });
                    }
                    mtJobQueue.WaitForCompletion();

                    //----- return bars
                    foreach (var option in optionChain)
                    {
                        if (option.Bid == 0.0 || option.Ask == 0.0)
                            continue;

                        // FIXME: this is far from pretty: need better timezone conversion
                        DateTime time = DateTime.Now + TimeSpan.FromHours(3);
                        DateTime expiration = new DateTime(
                            Convert.ToInt32(option.Details.Summary.LastTradeDateOrContractMonth.Substring(0, 4)),
                            Convert.ToInt32(option.Details.Summary.LastTradeDateOrContractMonth.Substring(4, 2)),
                            Convert.ToInt32(option.Details.Summary.LastTradeDateOrContractMonth.Substring(6, 2)))
                            + TimeSpan.FromHours(16);
                        if (expiration - time > TimeSpan.FromDays(180))
                            continue;

                        string right = option.Details.Summary.Right;
                        double strike = option.Details.Summary.Strike;
                        double bid = option.Bid;
                        double ask = option.Ask;

                        if (bid > 0 && ask > 0)
                        {
                            Bar newBar = new Bar(
                                symbol, time,
                                default(double), default(double), default(double), default(double), default(long), false,
                                bid, ask, default(long), default(long), true,
                                expiration, strike, right == "P");

                            yield return newBar;
                        }
                    }

                    yield break;
                }
                finally
                {
                    ibClient.Disconnect();
                    DateTime timestamp2 = DateTime.Now;
                    Output.WriteLine("finished option loading after {0:F1} seconds", (timestamp2 - timestamp1).TotalSeconds);
                }
            }
        #endregion

        #region public override string Name
            /// <summary>
            /// Name of updater.
            /// </summary>
            public override string Name
            {
                get
                {
                    return "InteractiveBrokers";
                }
            }
        #endregion
        }
#endif
    }
}

//==============================================================================
// end of file
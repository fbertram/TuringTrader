//==============================================================================
// Project:     Trading Simulator
// Name:        DataUpdaterIBOptions
// Description: Option data updater, Interactive Brokers
// History:     2018x19, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
#endregion

namespace FUB_TradingSim
{
    public class DataUpdaterIBOptions : DataUpdater
    {
        #region internal data
        #endregion
        #region internal helpers
        #endregion

        #region public DataUpdaterIBOptions(Algorithm algorithm, Dictionary<DataSourceValue, string> info) : base(info)
        public DataUpdaterIBOptions(Algorithm algorithm, Dictionary<DataSourceValue, string> info) : base(algorithm, info)
        {
        }
        #endregion

        #region override IEnumerable<Bar> void UpdateData(DateTime startTime, DateTime endTime)
        override public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime)
        {
            string twsUser = (string)Algorithm.GetRegistryValue("TwsUser", "");
            string twsPass = (string)Algorithm.GetRegistryValue("TwsPass", "");
            int twsPort = (int)Algorithm.GetRegistryValue("TwsPort", 7497);

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

                var optionChain = ibClient.ContractDetails(symbol, type, exchange, currency);

                foreach (var option in optionChain)
                {
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

                    // FIXME: this is really slow. accessing option.Bid and option.Ask will
                    //        trigger a request for market data, which takes about 1 second
                    //        per option to complete.
                    Bar newBar = new Bar(
                        symbol, time,
                        default(double), default(double), default(double), default(double), default(long), false,
                        option.Bid, option.Ask, default(long), default(long), true,
                        expiration, strike, right == "P");

                    yield return newBar;
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
        public override string Name
        {
            get
            {
                return "InteractiveBrokers";
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file
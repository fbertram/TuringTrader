//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo04_Options
// Description: demonstrate option trading
// History:     2018ix11, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#define USE_FAKE_QUOTES
// if this switch is defined, we simulate using fake quotes,
// created from SPX and VIX using the Black-Scholes model.

#region libraries
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.Demos
{
    public class Demo04_Options : Algorithm
    {
        #region internal data
        private Plotter _plotter = new Plotter();
        private readonly string _template = "SimpleChart";
        private readonly string _underlyingNickname = "$SPX.index";
#if USE_FAKE_QUOTES
        private readonly string _optionsNickname = "$SPX.fake.options";
#else
        //private readonly string _optionsNickname = "$SPX.options";
        private readonly string _optionsNickname = "$SPX.weekly.options";
#endif
        private readonly double _regTMarginToUse = 0.8;
        private readonly double _initialCash = 1e6;
        private double? _initialUnderlyingPrice = null;
        private Instrument _underlyingInstrument;
        #endregion

        #region override public void Run()
        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            WarmupStartTime = DateTime.Parse("01/01/2017");
            StartTime = DateTime.Parse("01/01/2017");
            EndTime = DateTime.Parse("08/01/2018");

            // set account value
            Deposit(_initialCash);
            CommissionPerShare = 0.01;

            // add instruments
            // the underlying must be added explicitly,
            // as the simulation engine requires it
            AddDataSource(_underlyingNickname);
            AddDataSource(_optionsNickname);

            //---------- simulation

            // loop through all bars
            foreach (DateTime simTime in SimTimes)
            {
                // find the underlying instrument
                // we could also find the underlying from the option chain
                if (_underlyingInstrument == null)
                    _underlyingInstrument = FindInstrument(_underlyingNickname);

                // retrieve the underlying spot price
                double underlyingPrice = _underlyingInstrument.Close[0];
                if (_initialUnderlyingPrice == null)
                    _initialUnderlyingPrice = underlyingPrice;

                // calculate volatility
                ITimeSeries<double> volatilitySeries = _underlyingInstrument.Close
                    .Volatility(10)
                    .Multiply(Math.Sqrt(252.0));
                double volatility = Math.Max(
                    volatilitySeries.EMA(21)[0], 
                    volatilitySeries.Highest(5)[0]);

                // find all expiry dates on the 3rd Friday of the month
                List<DateTime> expiryDates = OptionChain(_optionsNickname)
                    .Where(o => o.OptionExpiry.DayOfWeek == DayOfWeek.Friday && o.OptionExpiry.Day >= 15 && o.OptionExpiry.Day <= 21
                            || o.OptionExpiry.DayOfWeek == DayOfWeek.Saturday && o.OptionExpiry.Day >= 16 && o.OptionExpiry.Day <= 22)
                    .Select(o => o.OptionExpiry)
                    .Distinct()
                    .ToList();

                // select expiry 3 to 4 weeks out
                DateTime expiryDate = expiryDates
                        .Where(d => (d - simTime).TotalDays >= 21
                            && (d - simTime).TotalDays <= 28)
                        .FirstOrDefault();

                // retrieve option chain for this expiry
                List<Instrument> optionChain = OptionChain(_optionsNickname)
                        .Where(o => o.OptionIsPut
                            && o.OptionExpiry == expiryDate)
                        .ToList();

                // if we are currently flat, attempt to open a position
                if (Positions.Count == 0)
                {
                    // determine strike price: far away from spot price
                    double strikePrice = _underlyingInstrument.Close[0]
                        / Math.Exp(1.75 * Math.Sqrt((expiryDate - simTime).TotalDays / 365.25) * volatility);

                    // find contract closest to our desired strike
                    Instrument shortPut = optionChain
                        .OrderBy(o => Math.Abs(o.OptionStrike - strikePrice))
                        .FirstOrDefault();

                    // enter short put position
                    if (shortPut != null)
                    {
                        // Interactive Brokers margin requirements for short naked puts:
                        // Put Price + Maximum((15 % * Underlying Price - Out of the Money Amount), 
                        //                     (10 % * Strike Price)) 
                        double margin = Math.Max(0.15 * underlyingPrice - Math.Max(0.0, underlyingPrice - shortPut.OptionStrike),
                                            0.10 * underlyingPrice);
                        int contracts = (int)Math.Floor(Math.Max(0.0, _regTMarginToUse * Cash / (100.0 * margin)));

                        shortPut.Trade(-contracts, OrderType.closeThisBar)
                            .Comment = "open";
                    }
                }

                // monitor and maintain existing positions
                else // if (Postions.Count != 0)
                {
                    // find our currently open position
                    // we might need fancier code, in case we have more than
                    // one position open
                    Instrument shortPut = Positions.Keys.First();

                    // re-evaluate the likely trading range
                    double expectedLowestPrice = _underlyingInstrument.Close[0]
                        / Math.Exp(0.60 * Math.Sqrt((shortPut.OptionExpiry - simTime).Days / 365.25) * volatility);

                    // exit, when the risk of ending in the money is too high
                    // and, the contract is actively traded
                    if (expectedLowestPrice < shortPut.OptionStrike
                    &&  shortPut.BidVolume[0] > 0
                    &&  shortPut.Ask[0] < 2 * shortPut.Bid[0])
                    {
                        shortPut.Trade(-Positions[shortPut], OrderType.closeThisBar)
                            .Comment = "exit early";
                    }
                }

                // plot the underlying against our strategy results, plus volatility
                _plotter.SelectChart("nav vs time", "time"); // this will go to Sheet1
                _plotter.SetX(simTime);
                _plotter.Plot(_underlyingInstrument.Symbol, underlyingPrice / (double)_initialUnderlyingPrice);
                _plotter.Plot("volatility", volatilitySeries[0]);
                _plotter.Plot("net asset value", NetAssetValue[0] / _initialCash);
            }

            //---------- post-processing

            // create a list of trades on Sheet2
            _plotter.SelectChart("trades", "time");
            foreach (LogEntry entry in Log)
            {
                _plotter.SetX(entry.BarOfExecution.Time);
                _plotter.Plot("action", entry.Action);
                _plotter.Plot("instr", entry.Symbol);
                _plotter.Plot("qty", entry.OrderTicket.Quantity);
                _plotter.Plot("fill", entry.FillPrice);
                _plotter.Plot("comment", entry.OrderTicket.Comment ?? "");
            }

            FitnessValue = NetAssetValue[0];
        }
        #endregion
        #region override public void Report()
        public override void Report()
        {
            _plotter.OpenWith(_template);
        }
        #endregion
    }
}

//==============================================================================
// end of file
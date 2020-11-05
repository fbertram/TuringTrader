//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Aeromir_ParkingTrade
// Description: Aeromir.com's Parking Trade
// History:     2019i18, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2020, Bertram Solutions LLC
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
//------------------------------------------------------------------------------
//    Developed by Tim Pierson and Dave Thomas,
//    published and discussed on https://aeromir.com/
//
//    SPX Parking Trade
//    Entry
//        * Open a new trade once a week, preferably after a down move. If by
//          Wednesday you are not in, probably should enter it regardless of a
//          down day
//        * Sell the weekly that is approximately 30 days to expiration
//        * 20 or 25 point wide put credit spread
//        * Choose strike to get $1.00 premium, +/- 20 cents
//        * Will normally be about 100 points OTM
//    Exit
//        * Immediately after opening, place a GTC order to close for $0.20
//        * Set an alert on your trigger for recovery
//        * Expectation is 95% or more of trades are no-touch
//        * If you go into expiration week (can happen if it never exceeds $3.00)
//            - Close or roll no matter what on last trading day
//            - Roll when price is within one-day SD of the shorts
//==============================================================================

#define FAKE_DATA
// run on fake data, instead of actual quotes. comment out to use actual quotes.

//#define FUB_IMPROVEMENTS
// enable improvements made by FUB,
// some slight modifications/ optimizations to the original rules and settings

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public class Aeromir_ParkingTrade : Algorithm
    {
        public override string Name => "Parking Trade";

        #region inputs
        private readonly double INITIAL_CASH = 1e6;
        private readonly double COMMISSION = 0.01;
#if FAKE_DATA
        private readonly string UNDERLYING_NICK = "$SPX";
        public string OPTION_NICK = "$SPX.fake.options";
#else
        private string UNDERLYING_NICK = "$SPX";
        private string OPTION_NICK = "$SPX.weekly.options";
#endif
        private readonly string DEBUG_REPORT = "SimpleReport";

        private readonly int RISK_PER_TRADE = 7;

#if FUB_IMPROVEMENTS
        public int DOLLAR_REF = 1300; // as of mid 2012

        [OptimizerParam(50, 200, 10)]
        public int OPEN_PREMIUM_TARGET = 120;

        [OptimizerParam(20, 50, 5)]
        public int OPEN_PREMIUM_DIFF = 50;

        [OptimizerParam(10, 60, 5)]
        public int CLOSE_PROFIT_TARGET = 35;

        [OptimizerParam(200, 600, 25)]
        public int CLOSE_STOP_LOSS = 475;
#endif
        #endregion
        #region internal data
        private Plotter _plotter = new Plotter();
        private Instrument _underlyingInstrument = null;
#if FUB_IMPROVEMENTS
        private double SPX_SCALE
        {
            get
            {
                return _underlyingInstrument.Close[0] / DOLLAR_REF;
            }
        }
#endif
        #endregion
        #region private DebugPlot()
        private void DebugPlot()
        {
            if (TradingDays <= 0 || IsOptimizing)
                return;

            // plots on Sheet 1
            _plotter.SelectChart(Name, "Date");
            _plotter.SetX(SimTime[0]);
            _plotter.Plot(Name, NetAssetValue[0]);
            _plotter.Plot(_underlyingInstrument.Name, _underlyingInstrument.Close[0]);

            // trade log on Sheet 2
            if (IsLastBar)
            {
                _plotter.SelectChart("Strategy Trades", "time");
                foreach (LogEntry entry in Log)
                {
                    _plotter.SetX(entry.BarOfExecution.Time);
                    _plotter.Plot("action", entry.Action);
                    _plotter.Plot("type", entry.InstrumentType);
                    _plotter.Plot("instr", entry.Symbol);
                    _plotter.Plot("qty", entry.OrderTicket.Quantity);
                    _plotter.Plot("fill", entry.FillPrice);
                    _plotter.Plot("gross", -100.0 * entry.OrderTicket.Quantity * entry.FillPrice);
                    _plotter.Plot("commission", -entry.Commission);
                    _plotter.Plot("net", -100.0 * entry.OrderTicket.Quantity * entry.FillPrice
                                        - entry.Commission);
                    _plotter.Plot("comment", entry.OrderTicket.Comment ?? "");
                }
            }
        }
        #endregion
        #region private void OpenParkingTrade(DateTime expiry)
        private void OpenParkingTrade(DateTime expiry)
        {
#if FUB_IMPROVEMENTS || true
            bool downDay = _underlyingInstrument.Close[0]
                < _underlyingInstrument.Close.SMA(3)[0];
#else
            bool downDay = _underlyingInstrument.Close[0] 
                < _underlyingInstrument.Close[1];
#endif

            if (downDay || SimTime[0].DayOfWeek == DayOfWeek.Wednesday)
            {
                // filter option chain for viable candidates
                List<Instrument> putCandidates = OptionChain(OPTION_NICK)
                    .Where(o => o.OptionIsPut
                        && o.OptionExpiry == expiry
                        && o.OptionStrike < _underlyingInstrument.Close[0])
                    .ToList();

#if FUB_IMPROVEMENTS
                double premiumTarget = OPEN_PREMIUM_TARGET / 100.0 * SPX_SCALE;
                double premiumDiff = OPEN_PREMIUM_DIFF / 100.0 * SPX_SCALE;
                double premiumMin = premiumTarget - premiumDiff;
                double premiumMax = premiumTarget + premiumDiff;
#else
                double premiumTarget = 1.00;
                double premiumMin = premiumTarget - 0.20;
                double premiumMax = premiumTarget + 0.20;
#endif

                // find put spread best matching our criteria
                var putSpread = putCandidates
                    .SelectMany(
                        s => putCandidates
                            .Where(l => l.OptionStrike >= s.OptionStrike - 25
                                && l.OptionStrike <= s.OptionStrike - 20),
                        (s, l) => new
                        {
                            shortLeg = s,
                            longLeg = l,
                            credit = s.Bid[0] - l.Ask[0]
                        })
                    .Where(s => s.credit >= premiumMin && s.credit <= premiumMax)
                    .OrderBy(s => Math.Abs(s.credit - premiumTarget))
                    .FirstOrDefault();

                if (putSpread == null)
                {
                    if (!IsOptimizing)
                        Output.WriteLine("{0:MM/dd/yyyy} no suitable contracts found", SimTime[0]);

                    return;
                }

                // determine risk
                double risk = 100.0 * (
                    putSpread.shortLeg.OptionStrike
                    - putSpread.longLeg.OptionStrike
                    - putSpread.credit);
                int numContracts = (int)Math.Round(NetAssetValue[0] * RISK_PER_TRADE / 100.0 / risk);

                // trade the spread
                putSpread.shortLeg.Trade(-numContracts, OrderType.closeThisBar)
                    .Comment = "open";
                putSpread.longLeg.Trade(+numContracts, OrderType.closeThisBar)
                    .Comment = "open";
            }
        }
        #endregion
        #region private void MaintainParkingTrade(DateTime expiry)
        private void MaintainParkingTrade(DateTime expiry)
        {
            // find long and short legs
            Instrument shortLeg = Positions.Keys
                .Where(o => o.IsOption
                    && o.OptionIsPut
                    && o.OptionExpiry == expiry
                    && o.Position < 0)
                .FirstOrDefault();
            Instrument longLeg = Positions.Keys
                .Where(o => o.IsOption
                    && o.OptionIsPut
                    && o.OptionExpiry == expiry
                    && o.Position > 0)
                .FirstOrDefault();

            if (shortLeg == null || longLeg == null)
                return;

            // determine price to close, and DTE
            double priceToClose = shortLeg.Ask[0] - longLeg.Bid[0];
            double dte = (expiry - SimTime[0]).TotalDays;

            // close the position, as required
#if FUB_IMPROVEMENTS
            double profitTarget = CLOSE_PROFIT_TARGET / 100.0 * SPX_SCALE;
            double stopLoss = CLOSE_STOP_LOSS / 100.0 * SPX_SCALE;
#else
            double profitTarget = 0.20;
            double stopLoss = 3.00;
#endif

            string closingMessage = null;
            if (priceToClose <= profitTarget)
            {
                closingMessage = "profit target";
            }
            else if (priceToClose >= stopLoss)
            {
                closingMessage = "stop loss";
            }
            else if (dte < 2)
            {
                closingMessage = "DTE < 2";
            }

            if (closingMessage != null)
            {
                shortLeg.Trade(-shortLeg.Position, OrderType.closeThisBar)
                    .Comment = closingMessage;
                longLeg.Trade(-longLeg.Position, OrderType.closeThisBar)
                    .Comment = closingMessage;
            }

            // TODO: roll when price is within one-day SD of the shorts
        }
        #endregion

        #region override public void Run()
        override public void Run()
        {
            //---------- initialization

            // set simulation time frame
            WarmupStartTime = DateTime.Parse("06/01/2011", CultureInfo.InvariantCulture);
            StartTime = DateTime.Parse("01/01/2012", CultureInfo.InvariantCulture);
            EndTime = DateTime.Parse("12/31/2018", CultureInfo.InvariantCulture);

            // set commission
            // Interactive Brokers: $0.70 per contract (premium >= $0.10, volume <= 10,000)
            CommissionPerShare = COMMISSION;

            // fund account
            Deposit(INITIAL_CASH);

            // add instruments
            // the underlying must be added explicitly, for the simulator to work
            AddDataSource(UNDERLYING_NICK);
            AddDataSource(OPTION_NICK);

            //---------- simulation

            // loop through all bars
            foreach (DateTime simTime in SimTimes)
            {
                // find the underlying instrument
                if (_underlyingInstrument == null)
                    _underlyingInstrument = FindInstrument(UNDERLYING_NICK);

                // find all weekly expiry dates
                List<DateTime> expiryDates = OptionChain(OPTION_NICK)
                    .Where(o => o.OptionExpiry.DayOfWeek == DayOfWeek.Friday
                        || o.OptionExpiry.DayOfWeek == DayOfWeek.Saturday)
                    .Select(o => o.OptionExpiry.Date)
                    .Distinct()
                    .ToList();

                // finding our expiry date for next open
                DateTime openExpiry = expiryDates
                    .Where(d => (d - SimTime[0]).TotalDays <= 32
                        && (d - SimTime[0]).TotalDays >= 30)
                    .FirstOrDefault();

                // roll positions on the key dates
                if (openExpiry != default(DateTime)
                && Positions.Keys.Where(p => p.OptionExpiry == openExpiry).Count() == 0)
                {
                    OpenParkingTrade(openExpiry);
                }

                // maintain positions daily
                foreach (DateTime expiry in Positions.Keys.Select(i => i.OptionExpiry).Distinct())
                {
                    MaintainParkingTrade(expiry);
                }

                DebugPlot();
            }

            //---------- post-processing

            // poor man's sharpe ratio
            FitnessValue = (NetAssetValue[0] / INITIAL_CASH - 1.0) / NetAssetValueMaxDrawdown;
        }
        #endregion
        #region override public void Report()
        override public void Report()
        {
            _plotter.OpenWith(DEBUG_REPORT);
        }
        #endregion
    }
}

//==============================================================================
// end of file

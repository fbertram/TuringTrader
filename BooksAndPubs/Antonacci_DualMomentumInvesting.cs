//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Antonacci_DualMomentumInvesting
// Description: Strategy, as published in Gary Antonacci's book
//              'Dual Momentum Investing'.
//              http://www.optimalmomentum.com/
// History:     2018xi22, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public class Antonacci_DualMomentumInvesting : Algorithm
    {
        public override string Name => "Dual Momentum"; 

        #region internal data
        private readonly double INITIAL_FUNDS = 100000;
        private readonly string BENCHMARK = "@60_40";
        private Instrument _benchmark = null;
        private Plotter _plotter;
        private AllocationTracker _alloc = new AllocationTracker();
        #endregion
        #region instruments
        // this is the benchmark to measure absolute momentum:
        private static string ABS_BENCHM = "BIL"; // SPDR Bloomberg Barclays 1-3 Month T-Bill ETF

        // the safe instrument, in case we fall below the absolute benchmark:
        private static string SAFE_INSTR = "AGG"; // iShares Core U.S. Aggregate Bond ETF

        private static readonly HashSet<HashSet<string>> _assetClasses = new HashSet<HashSet<string>>
        {
            //--- equity
            new HashSet<string> {
                "VTI",   // Vanguard Total Stock Market Index ETF
                "VEU",   // Vanguard FTSE All World ex US ETF
                // could use SPY/ EFA here
                ABS_BENCHM,
            },
            //--- credit
            new HashSet<string> {
                "HYG",   // iShares iBoxx High Yield Corporate Bond ETF
                //"CIU" => changed to IGIB in 06/2018
                "IGIB",  // iShares Intermediate-Term Corporate Bond ETF
                ABS_BENCHM,
            },
            //--- real estate
            new HashSet<string> {
                "VNQ",   // Vanguard Real Estate Index ETF
                "REM",   // iShares Mortgage Real Estate ETF
                ABS_BENCHM,
            },
            //--- economic stress
            new HashSet<string> {
                "GLD",   // SPDR Gold Shares ETF
                "TLT",   // iShares 20+ Year Treasury Bond ETF
                ABS_BENCHM,
            },
        };
        #endregion
        #region ctor
        public Antonacci_DualMomentumInvesting()
        {
            _plotter = new Plotter(this);
        }
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //----- initialization

            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;

            // list of assets we can trade
            List<string> assets = _assetClasses
                .SelectMany(c => c)
                .Distinct()
                .Where(nick => nick != ABS_BENCHM)
                .ToList();
            assets.Add(SAFE_INSTR);

            AddDataSources(assets);
            AddDataSource(ABS_BENCHM);
            AddDataSource(BENCHMARK);

            Deposit(INITIAL_FUNDS);
            CommissionPerShare = Globals.COMMISSION; // it is unclear, if the book considers commissions

            //----- simulation loop

            foreach (DateTime simTime in SimTimes)
            {
                // evaluate momentum for all known instruments
                Dictionary<Instrument, double> instrumentMomentum = Instruments
                    .ToDictionary(i => i,
                        i => i.Close[0] / i.Close[252] - 1.0);

                // skip if there are any missing instruments
                if (!HasInstruments(assets) || !HasInstrument(BENCHMARK) || !HasInstrument(ABS_BENCHM))
                    continue;

                // execute trades once per month
                // CAUTION: do not calculate indicators within this block
                if (SimTime[0].Month != SimTime[1].Month)
                {
                    // create empty structure for instrument weights
                    Dictionary<Instrument, double> instrumentWeights = Instruments
                    .ToDictionary(i => i, i => 0.0);

                    // loop through all asset classes, and find the top-ranked one
                    foreach (HashSet<string> assetClass in _assetClasses)
                    {
                        List<Instrument> assetClassInstruments = assetClass
                            .Select(n => FindInstrument(n))
                            .ToList();

                        // find the instrument with the highest momentum
                        // in each asset class
                        var bestInstrument = assetClassInstruments
                            .OrderByDescending(i => instrumentMomentum[i])
                            .Take(1)
                            .First();

                        // sum up the weights (because the safe instrument is duplicated)
                        instrumentWeights[bestInstrument] += 1.0 / _assetClasses.Count;
                    }

                    // if momentum of any instrument drops below that of a T-Bill,
                    // we use the safe instrument
                    // these 2 lines swap T-Bills for the safe instrument:
                    if (SAFE_INSTR != ABS_BENCHM)
                    {
                        instrumentWeights[FindInstrument(SAFE_INSTR)] = instrumentWeights[FindInstrument(ABS_BENCHM)];
                        instrumentWeights[FindInstrument(ABS_BENCHM)] = 0.0;
                    }

                    _alloc.LastUpdate = SimTime[0];

                    foreach (var instrumentWeight in instrumentWeights)
                    {
                        if (assets.Contains(instrumentWeight.Key.Nickname))
                            _alloc.Allocation[instrumentWeight.Key] = instrumentWeight.Value;

                        int targetShares = (int)Math.Floor(instrumentWeight.Value * NetAssetValue[0] / instrumentWeight.Key.Close[0]);
                        int currentShares = instrumentWeight.Key.Position;

                        Order newOrder = instrumentWeight.Key.Trade(targetShares - currentShares);

                        if (newOrder != null)
                        {
                            if (currentShares == 0) newOrder.Comment = "open";
                            else if (targetShares == 0) newOrder.Comment = "close";
                            else newOrder.Comment = "rebalance";
                        }
                    }
                }

                // plot nav and benchmark
                _benchmark = _benchmark ?? FindInstrument(BENCHMARK);

                if (TradingDays > 0)
                {
                    _plotter.SelectChart(Name, "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot(Name, NetAssetValue[0]);
                    _plotter.Plot(_benchmark.Symbol, _benchmark.Close[0]);
                }
            }

            //----- post processing

            // create sheet w/ trading log
            _plotter.SelectChart("Strategy Trades", "date");
            foreach (LogEntry entry in Log)
            {
                _plotter.SetX(entry.BarOfExecution.Time);
                _plotter.Plot("action", entry.Action);
                _plotter.Plot("type", entry.InstrumentType);
                _plotter.Plot("instr", entry.Symbol);
                _plotter.Plot("qty", entry.OrderTicket.Quantity);
                _plotter.Plot("fill", entry.FillPrice);
                _plotter.Plot("gross", -entry.OrderTicket.Quantity * entry.FillPrice);
                _plotter.Plot("commission", -entry.Commission);
                _plotter.Plot("net", -entry.OrderTicket.Quantity * entry.FillPrice - entry.Commission);
                _plotter.Plot("comment", entry.OrderTicket.Comment ?? "");
            }

            // create sheet w/ asset allocation
            _alloc.ToPlotter(_plotter);
        }
        #endregion
        #region public override void Report()
        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
        #endregion
    }
}

//==============================================================================
// end of file
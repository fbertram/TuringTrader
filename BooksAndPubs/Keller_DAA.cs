//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Keller_DAA
// Description: Strategy, as published in Wouter J. Keller and Jan Willem Keuning's
//              paper 'Breadth Momentum and the Canary Universe:
//              Defensive Asset Allocation (DAA)'
//              https://papers.ssrn.com/sol3/papers.cfm?abstract_id=3212862
// History:     2019ii18, FUB, created
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
using TuringTrader.BooksAndPubs;
using TuringTrader.Simulator;
#endregion

namespace BooksAndPubs
{
    public abstract class Keller_DAA_Core : Algorithm
    {
        public override string Name => "Keller's DAA";

        #region inputs
        protected abstract List<string> riskyUniverse { get; }
        protected abstract List<string> cashUniverse { get; }
        protected abstract List<string> protectiveUniverse { get; }
        protected abstract int T { get; } // (risky) top parameter
        protected abstract int B { get; } // breadth parameter
        #endregion
        #region internal data
        private readonly string BENCHMARK = "@60_40";
        private Plotter _plotter;
        private AllocationTracker _alloc = new AllocationTracker();
        #endregion
        #region ctor
        public Keller_DAA_Core()
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

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION; // paper does not consider trade commissions

            AddDataSources(riskyUniverse);
            AddDataSources(cashUniverse);
            AddDataSources(protectiveUniverse);
            AddDataSource(BENCHMARK);

            //----- simulation loop

            foreach (DateTime simTime in SimTimes)
            {
                // calculate 13612W momentum for all instruments
                Dictionary<Instrument, double> momentum13612W = Instruments
                    .ToDictionary(
                        i => i,
                        i => 0.25 *
                            (12.0 * (i.Close[0] / i.Close[21] - 1.0)
                            + 4.0 * (i.Close[0] / i.Close[63] - 1.0)
                            + 2.0 * (i.Close[0] / i.Close[126] - 1.0)
                            + 1.0 * (i.Close[0] / i.Close[252] - 1.0)));

                // skip if there are any missing instruments
                // we want to make sure our strategy has all instruments available
                if (!HasInstruments(riskyUniverse)
                || !HasInstruments(cashUniverse)
                || !HasInstruments(protectiveUniverse))
                    continue;

                // rebalance once per month
                // CAUTION: no indicator calculations within this block!
                if (SimTime[0].Month != SimTime[1].Month)
                {
                    // find T top risky assets
                    IEnumerable<Instrument> topInstruments = Instruments
                    .Where(i => riskyUniverse.Contains(i.Nickname))
                    .OrderByDescending(i => momentum13612W[i])
                    .Take(T);

                    // find single cash/ bond asset
                    Instrument cashInstrument = Instruments
                        .Where(i => cashUniverse.Contains(i.Nickname))
                        .OrderByDescending(i => momentum13612W[i])
                        .First();

                    // determine number of bad assets in canary universe
                    double b = Instruments
                        .Where(i => protectiveUniverse.Contains(i.Nickname))
                        .Sum(i => momentum13612W[i] < 0.0 ? 1.0 : 0.0);

                    // calculate cash fraction
                    //double CF = Math.Min(1.0, b / B) // standard calculation
                    double CF = Math.Min(1.0, 1.0 / T * Math.Floor(b * T / B)); // Easy Trading

                    // set instrument weights
                    Dictionary<Instrument, double> weights = Instruments
                        .ToDictionary(i => i, i => 0.0);

                    weights[cashInstrument] = CF;

                    foreach (Instrument i in topInstruments)
                        weights[i] += (1.0 - CF) / T;

                    _alloc.LastUpdate = SimTime[0];

                    foreach (Instrument i in Instruments)
                    {
                        if (riskyUniverse.Contains(i.Nickname) || cashUniverse.Contains(i.Nickname))
                            _alloc.Allocation[i] = weights[i];

                        int targetShares = (int)Math.Floor(weights[i] * NetAssetValue[0] / i.Close[0]);

                        Order newOrder = i.Trade(targetShares - i.Position);

                        if (newOrder != null)
                        {
                            if (i.Position == 0) newOrder.Comment = "open";
                            else if (targetShares == 0) newOrder.Comment = "close";
                            else newOrder.Comment = "rebalance";
                        }
                    }
                }

                // plotter output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, FindInstrument(BENCHMARK));
                    _plotter.AddStrategyHoldings(this, Instruments
                        .Where(i => riskyUniverse.Contains(i.Nickname) || cashUniverse.Contains(i.Nickname)));
                }
            }

            //----- post processing

            if (!IsOptimizing)
            {
                _plotter.AddTargetAllocation(_alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
            }

            FitnessValue = this.CalcFitness();
        }
        #endregion
        #region public override void Report()
        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
        #endregion
    }

    #region DAA-G4 - has subpar risk/ return
    public class Keller_DAA_G4 : Keller_DAA_Core
    {
        public override string Name => "Keller's DAA-G4";
        protected override List<string> riskyUniverse => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };

        protected override List<string> cashUniverse => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
            "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };

        protected override List<string> protectiveUniverse => new List<string>
        {
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND"  // Vanguard Total Bond Market ETF
        };

        protected override int T => 2; // (risky) top parameter
        protected override int B => 1; // breadth parameter
    }
    #endregion
    #region DAA-G6 - instead of DAA-G4, we use DAA-G6
    public class Keller_DAA_G6 : Keller_DAA_Core
    {
        public override string Name => "Keller's DAA-G6";
        protected override List<string> riskyUniverse => new List<string>
            {
                    "SPY", // SPDR S&P 500 ETF
                    "VEA", // Vanguard FTSE Developed Markets ETF
                    "VWO", // Vanguard FTSE Emerging Markets ETF
                    "LQD", // iShares iBoxx Investment Grade Corporate Bond ETF
                    "TLT", // iShares 20+ Year Treasury Bond ETF
                    "HYG"  // iShares iBoxx High Yield Corporate Bond ETF
            };

        protected override List<string> cashUniverse => new List<string>
            {
                    "SHY", // iShares 1-3 Year Treasury Bond ETF
                    "IEF", // iShares 7-10 Year Treasury Bond ETF
                    "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
            };

        protected override List<string> protectiveUniverse => new List<string>
            {
                    "VWO", // Vanguard FTSE Emerging Markets ETF
                    "BND"  // Vanguard Total Bond Market ETF
            };

        protected override int T => 6; // (risky) top parameter
        protected override int B => 2; // breadth parameter
    }
    #endregion
    #region DAA-G12 - this is the 'standard' DAA
    public class Keller_DAA_G12 : Keller_DAA_Core
    {
        public override string Name => "Keller's DAA-G12";
        protected override List<string> riskyUniverse => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "IWM", // iShares Russell 2000 ETF
            "QQQ", // Invesco Nasdaq-100 ETF
            "VGK", // Vanguard FTSE Europe ETF
            "EWJ", // iShares MSCI Japan ETF
            "VWO", // Vanguard MSCI Emerging Markets ETF
            "VNQ", // Vanguard Real Estate ETF
            "GSG", // iShares S&P GSCI Commodity-Indexed Trust
            "GLD", // SPDR Gold Trust ETF
            "TLT", // iShares 20+ Year Treasury Bond ETF
            "HYG", // iShares iBoxx High Yield Corporate Bond ETF
            "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };

        protected override List<string> cashUniverse => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
            "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };

        protected override List<string> protectiveUniverse => new List<string>
        {
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND"  // Vanguard Total Bond Market ETF
        };

        protected override int T => 6; // (risky) top parameter
        protected override int B => 2; // breadth parameter
    }
    #endregion

    #region DAA1-G4 - aggressive G4
    public class Keller_DAA1_G4 : Keller_DAA_Core
    {
        public override string Name => "Keller's DAA1-G4";
        protected override List<string> riskyUniverse => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };

        protected override List<string> cashUniverse => new List<string>
        {
            "SHV", // iShares Short Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
            "UST"  // ProShares Ultra 7-10 Year Treasury ETF
        };

        protected override List<string> protectiveUniverse => new List<string>
        {
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND"  // Vanguard Total Bond Market ETF
        };

        protected override int T => 4; // (risky) top parameter
        protected override int B => 1; // breadth parameter
    }
    #endregion
    #region DAA1-G12 - aggressive G12
    public class Keller_DAA1_G12 : Keller_DAA_Core
    {
        public override string Name => "Keller's DAA1-G12";
        protected override List<string> riskyUniverse => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "IWM", // iShares Russell 2000 ETF
            "QQQ", // Invesco Nasdaq-100 ETF
            "VGK", // Vanguard FTSE Europe ETF
            "EWJ", // iShares MSCI Japan ETF
            "VWO", // Vanguard MSCI Emerging Markets ETF
            "VNQ", // Vanguard Real Estate ETF
            "GSG", // iShares S&P GSCI Commodity-Indexed Trust
            "GLD", // SPDR Gold Trust ETF
            "TLT", // iShares 20+ Year Treasury Bond ETF
            "HYG", // iShares iBoxx High Yield Corporate Bond ETF
            "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };

        protected override List<string> cashUniverse => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
            "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };

        protected override List<string> protectiveUniverse => new List<string>
        {
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND"  // Vanguard Total Bond Market ETF
        };

        protected override int T => 2; // (risky) top parameter
        protected override int B => 1; // breadth parameter
    }
    #endregion
    #region DAA1-U1 - minimalistic version
    public class Keller_DAA1_U1 : Keller_DAA_Core
    {
        public override string Name => "Keller's DAA1-U1";
        protected override List<string> riskyUniverse => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
        };

        protected override List<string> cashUniverse => new List<string>
        {
            "SHV", // iShares Short Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
            "UST"  // ProShares Ultra 7-10 Year Treasury ETF
        };

        protected override List<string> protectiveUniverse => new List<string>
        {
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND"  // Vanguard Total Bond Market ETF
        };

        protected override int T => 1; // (risky) top parameter
        protected override int B => 1; // breadth parameter
    }
    #endregion
}

//==============================================================================
// end of file
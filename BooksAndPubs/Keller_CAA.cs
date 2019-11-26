//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Keller_CAA
// Description: Classical Asset Allocation (CAA) strategy, as published in 
//              Wouter J. Keller, Adam Butler, and Ilya Kipnis' paper 
//              'Momentum and Markowitz: a Golden Combination'.
//              https://papers.ssrn.com/sol3/papers.cfm?abstract_id=2606884
// History:     2019iii07, FUB, created
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
using TuringTrader.BooksAndPubs;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
using TuringTrader.Support;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public abstract class Keller_CAA_Core : SubclassableAlgorithm
    {
        public override string Name => "Keller's CAA Strategy";

        #region inputs
        protected abstract double TVOL { get; }
        protected abstract List<string> RISKY_ASSETS { get; }
        protected abstract List<string> SAFE_ASSETS { get;  }
        protected virtual double MAX_RISKY_ALLOC => 0.25;
        #endregion
        #region internal data
        //private readonly string BENCHMARK = "^SPX.index";
        private readonly string BENCHMARK = Globals.BALANCED_PORTFOLIO;
        private Plotter _plotter;
        private AllocationTracker _alloc = new AllocationTracker();
        #endregion
        #region ctor
        public Keller_CAA_Core()
        {
            _plotter = new Plotter(this);
        }
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //========== initialization ==========

            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION;

            // our universe consists of risky & safe assets
            var universe = RISKY_ASSETS
                .Concat(SAFE_ASSETS).ToList();

            // add all data sources
            AddDataSource(BENCHMARK);
            foreach (var nick in universe)
                AddDataSource(nick);

            //========== simulation loop ==========

            foreach (var simTime in SimTimes)
            {
                // calculate indicators on overy bar
                Dictionary<Instrument, double> momentum = Instruments
                    .ToDictionary(
                        i => i,
                        i => (1.0 * i.Close.Momentum(21)[0]
                            + 3.0 * i.Close.Momentum(63)[0]
                            + 6.0 * i.Close.Momentum(126)[0]
                            + 12.0 * i.Close.Momentum(252)[0]) / 22.0);

                // skip if there are any instruments missing from our universe
                if (!HasInstruments(universe))
                    continue;

                // trigger rebalancing
                if (SimTime[0].Month != SimTime[1].Month) // monthly
                {
                    // calculate covariance
                    var covar = new PortfolioSupport.Covariance(Instruments, 12, 21); // 12 monthly bars

                    // calculate efficient frontier for universe
                    // note how momentum and covariance are annualized here
                    var cla = new PortfolioSupport.MarkowitzCLA(
                        Instruments.Where(i => universe.Contains(i.Nickname)),
                        i => 252.0 * momentum[i],
                        (i, j) => 252.0 / covar.BarSize * covar[i, j], // TODO: is sqrt correct?
                        i => 0.0,
                        i => SAFE_ASSETS.Contains(i.Nickname) ? 1.0 : MAX_RISKY_ALLOC);

                    // find portfolio with specified risk
                    var pf = cla.TargetVolatility(TVOL);

                    Output.WriteLine("{0:MM/dd/yyyy}: {1}", SimTime[0], pf.ToString());

                    // adjust all positions
                    _alloc.LastUpdate = SimTime[0];
                    foreach (var i in pf.Weights.Keys)
                    {
                        _alloc.Allocation[i] = pf.Weights[i];

                        int targetShares = (int)Math.Floor(NetAssetValue[0] * pf.Weights[i] / i.Close[0]);
                        int currentShares = i.Position;

                        var ticket = i.Trade(targetShares - currentShares);

                        if (ticket != null)
                        {
                            if (i.Position == 0) ticket.Comment = "open";
                            else if (targetShares == 0) ticket.Comment = "close";
                            else ticket.Comment = "rebalance";
                        }
                    }
                }

                // plotter output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.AddNavAndBenchmark(this, FindInstrument(BENCHMARK));
                    _plotter.AddStrategyHoldings(this, universe.Select(nick => FindInstrument(nick)));

                    if (IsSubclassed) AddSubclassedBar();
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                _plotter.AddTargetAllocation(_alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                _plotter.AddParameters(this);
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

    #region N=8 universe
    public abstract class Keller_CAA_N8 : Keller_CAA_Core
    {
        protected override List<string> RISKY_ASSETS => new List<string>
        {
            "SPY", // S&P 500
            "EFA", // EAFE
            "EEM", // Emerging Markets
            "QQQ", // US Technology Sector
            "EWJ", // Japanese Equities
            "HYG", // High Yield Bonds
        };

        protected override List<string> SAFE_ASSETS => new List<string>
        {
            "IEF", // 10-Year Treasuries
            "BIL", // T-Bills
        };
    }
    #endregion
    #region N=16 universe
    /*
    public abstract class Keller_CAA_N16 : Keller_CAA
    {
        public Keller_CAA_N16()
        {
            RISKY_ASSETS = new List<string>
            {
                "", // Non - Durables
                "", // Durables
                "", // Manufacturing
                "", // Energy
                "", // Technology
                "", // Telecom
                "", // Shops
                "", // Health
                "", // Utilities
                "", // Other
                "", // 10-Year Treasuries
                "", // 30-Year Treasuries
                "", // U.S. Municipal Bonds
                "", // U.S. Corporate Bonds
                "", // U.S High Yield Bonds
                "", // T-Bills
            };

            SAFE_ASSETS = new List<string>
            {
                "IEF.etf", // 10-Year Treasuries
                "BIL.etf", // T-Bills
            };
        }
    }
    */
    #endregion
    #region N=39 universe
    /*
    public abstract class Keller_CAA_N39 : Keller_CAA
    {
        public Keller_CAA_N39()
        {
            RISKY_ASSETS = new List<string>
            {
                "", // S&P 500
                "", // US Small Caps
                "", // EAFE
                "", // Emerging Markets
                "", // Japanese Equities
                "", // Non-Durables
                "", // Durables
                "", // Manufacturing
                "", // Energy
                "", // Technology
                "", // Telecom 
                "", // Shops
                "", // Health
                "", // Utilities
                "", // Other Sector
                "", // Dow Utilities
                "", // Dow Transports
                "", // Dow Industrials
                "", // FTSE US 1000
                "", // FTSE US 1500 
                "", // FTSE Global ex-US
                "", // FTSE Developed Equities
                "", // FTSE Emerging Markets
                "", // 10-Year Treasuries
                "", // 30-Year Treasuries
                "", // U.S. TIPs
                "", // U.S. Municipal Bonds
                "", // U.S. Corporate Bonds
                "", // U.S High Yield Bonds
                "", // T-Bills
                "", // Int’l Gov’t Bonds 
                "", // Japan 10-Year Gov’t Bond
                "", // Commodities (GSCI)
                "", // Gold
                "", // REITs
                "", // Mortgage REITs
                "", // FX (1x )
                "", // FX (2x)
                "", // Timber
            };
            SAFE_ASSETS = new List<string>
            {
                "IEF.etf", // 10-Year Treasuries
                "BIL.etf", // T-Bills
            };
        }
    }
    */
    #endregion

    #region N=8, TV=5%
    public class Keller_CAA_N8_TV5 : Keller_CAA_N8
    {
        public override string Name => "Keller's CAA-N=8-TV=5%";
        protected override double TVOL => 0.05;
    }
    #endregion
    #region N=8, TV=10%
    public class Keller_CAA_N8_TV10 : Keller_CAA_N8
    {
        public override string Name => "Keller's CAA-N=8-TV=10%";
        protected override double TVOL => 0.10;
    }
    #endregion
}

//==============================================================================
// end of file
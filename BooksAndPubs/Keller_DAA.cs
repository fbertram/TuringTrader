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

// compare results - website operated by Jan Willem Keuning:
// https://indexswingtrader.blogspot.com/2018/07/announcing-defensive-asset-allocation.html

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.BooksAndPubs;
using TuringTrader.Simulator;
using TuringTrader.Algorithms.Glue;
#endregion

namespace TuringTrader.BooksAndPubs
{
    public abstract class Keller_DAA_Core : AlgorithmPlusGlue
    {
        public override string Name => "Keller's DAA";

        #region inputs
        protected abstract List<string> RISKY_UNIVERSE { get; }
        protected abstract List<string> CASH_UNIVERSE { get; }
        protected abstract List<string> PROTECTIVE_UNIVERSE { get; }
        protected abstract int T { get; } // (risky) top parameter
        protected abstract int B { get; } // breadth parameter
        protected Dictionary<string, DataSource> ASSET_SUB = null; // for 'on steroids' versions
        #endregion
        #region internal data
        private readonly string BENCHMARK = Assets.PORTF_60_40;
        #endregion
        #region internal helpers
        /// <summary>
        /// substitute assets for 'on steroids' versions
        /// </summary>
        /// <param name="signal">signal asset</param>
        /// <returns>traded asset</returns>
        private Instrument AssetSub(Instrument signal)
        {
            if (ASSET_SUB == null || !ASSET_SUB.ContainsKey(signal.Nickname))
                return signal;

            return ASSET_SUB[signal.Nickname].Instrument;
        }

        #endregion

        #region public override void Run()
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            //----- initialization

            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION; // paper does not consider trade commissions

            var riskyUniverse = AddDataSources(RISKY_UNIVERSE);
            var cashUniverse = AddDataSources(CASH_UNIVERSE);
            var protectiveUniverse = AddDataSources(PROTECTIVE_UNIVERSE);
            var benchmark = AddDataSource(BENCHMARK);

            //----- simulation loop

            var monthlyBars = new Dictionary<Instrument, TimeSeries<double>>();

            foreach (DateTime simTime in SimTimes)
            {
                // skip if there are any missing instruments
                // we want to make sure our strategy has all instruments available
                if (!HasInstruments(riskyUniverse)
                || !HasInstruments(cashUniverse)
                || !HasInstruments(protectiveUniverse)
                || !HasInstrument(benchmark)
                || ASSET_SUB != null && !HasInstruments(ASSET_SUB.Values))
                    continue;

                // rebalance once per month
                // CAUTION: no indicator calculations within this block!
                if (SimTime[0].Month != NextSimTime.Month)
                {
                    // calculate 13612W momentum for all instruments
                    foreach (var i in Instruments)
                        if (!monthlyBars.ContainsKey(i))
                            monthlyBars[i] = new TimeSeries<double>();

                    foreach (var i in Instruments)
                        monthlyBars[i].Value = i.Close[0];

                    Dictionary<Instrument, double> momentum13612W = Instruments
                        .ToDictionary(
                            i => i,
                            i => 0.25 *
                                (12.0 * (monthlyBars[i][0] / monthlyBars[i][1] - 1.0)
                                + 4.0 * (monthlyBars[i][0] / monthlyBars[i][3] - 1.0)
                                + 2.0 * (monthlyBars[i][0] / monthlyBars[i][6] - 1.0)
                                + 1.0 * (monthlyBars[i][0] / monthlyBars[i][12] - 1.0)));

                    // determine number of bad assets in canary universe
                    double b = protectiveUniverse
                        .Select(ds => ds.Instrument)
                        .Sum(i => momentum13612W[i] < 0.0 ? 1.0 : 0.0);

                    // calculate cash fraction
                    //double CF = Math.Min(1.0, b / B) // standard calculation
                    double CF = Math.Min(1.0, 1.0 / T * Math.Floor(b * T / B)); // Easy Trading

                    // as part of Easy Trading, we scale back the number of 
                    // top assets as CF increases
                    int t = (int)Math.Round((1.0 - CF) * T);

                    // find T top risky assets
                    IEnumerable<Instrument> topInstruments = riskyUniverse
                        .Select(ds => ds.Instrument)
                        .OrderByDescending(i => momentum13612W[i])
                        .Take(t);

                    // find single cash/ bond asset
                    Instrument cashInstrument = cashUniverse
                        .Select(ds => ds.Instrument)
                        .OrderByDescending(i => momentum13612W[i])
                        .First();

                    // set instrument weights
                    Dictionary<Instrument, double> weights = Instruments
                        .ToDictionary(i => i, i => 0.0);

                    weights[cashInstrument] = CF;

                    foreach (Instrument i in topInstruments)
                        weights[i] += (1.0 - CF) / t;

                    foreach (Instrument i in Instruments)
                    {
                        // skip instruments not in our relevant universes
                        if (!riskyUniverse.Contains(i.DataSource) && !cashUniverse.Contains(i.DataSource))
                            continue;

                        // for the 'on steroids' versions, we run the signals
                        // as usual, but substitute some assets with leveraged
                        // counterparts for the actual trading
                        var i2 = AssetSub(i);

                        // calculate target allocations
                        Alloc.Allocation[i2] = weights[i];
                        int targetShares = (int)Math.Floor(weights[i] * NetAssetValue[0] / i2.Close[0]);

                        Order newOrder = i2.Trade(targetShares - i2.Position);

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
                        .Where(i => riskyUniverse.Contains(i.DataSource) || cashUniverse.Contains(i.DataSource))
                        .Select(i => AssetSub(i)));
                    if (Alloc.LastUpdate == SimTime[0])
                        _plotter.AddTargetAllocationRow(Alloc);
                }

                var v = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                yield return Bar.NewOHLC(
                    this.GetType().Name, SimTime[0],
                    v, v, v, v, 0);
            }

            //----- post processing

            if (!IsOptimizing)
            {
                _plotter.AddTargetAllocation(Alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                _plotter.AddParameters(this);
            }

            FitnessValue = this.CalcFitness();
        }
        #endregion
    }

    #region DAA-G4 - has subpar risk/ return
    public class Keller_DAA_G4 : Keller_DAA_Core
    {
        public override string Name => "Keller's DAA-G4";
        protected override List<string> RISKY_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };

        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
            "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
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
        protected override List<string> RISKY_UNIVERSE => new List<string>
            {
                    "SPY", // SPDR S&P 500 ETF
                    "VEA", // Vanguard FTSE Developed Markets ETF
                    "VWO", // Vanguard FTSE Emerging Markets ETF
                    "LQD", // iShares iBoxx Investment Grade Corporate Bond ETF
                    "TLT", // iShares 20+ Year Treasury Bond ETF
                    "HYG"  // iShares iBoxx High Yield Corporate Bond ETF
            };

        protected override List<string> CASH_UNIVERSE => new List<string>
            {
                    "SHY", // iShares 1-3 Year Treasury Bond ETF
                    "IEF", // iShares 7-10 Year Treasury Bond ETF
                    "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
            };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
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
        protected override List<string> RISKY_UNIVERSE => new List<string>
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
            "splice:HYG,VWEAX", // iShares iBoxx High Yield Corporate Bond ETF
            "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };

        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
            "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
        {
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "splice:BND,AGG"  // Vanguard Total Bond Market ETF
        };

        protected override int T => 6; // (risky) top parameter
        protected override int B => 2; // breadth parameter
    }
    #endregion

    #region DAA1-G4 - aggressive G4
    public class Keller_DAA1_G4 : Keller_DAA_Core
    {
        public override string Name => "Keller's DAA1-G4";
        protected override List<string> RISKY_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };

        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHV", // iShares Short Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
            "UST"  // ProShares Ultra 7-10 Year Treasury ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
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
        protected override List<string> RISKY_UNIVERSE => new List<string>
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

        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHV", // iShares Short Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
            "UST"  // ProShares Ultra 7-10 Year Treasury ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
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
        protected override List<string> RISKY_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
        };

        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHV", // iShares Short Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
            "UST"  // ProShares Ultra 7-10 Year Treasury ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
        {
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND"  // Vanguard Total Bond Market ETF
        };

        protected override int T => 1; // (risky) top parameter
        protected override int B => 1; // breadth parameter
    }
    #endregion

#if true
    // see https://alphaarchitect.com/2018/12/07/trend-following-on-steroids/
    // see https://indexswingtrader.blogspot.com/2018/12/exploring-smart-leverage-daa-on-steroids.html
    // these variants stem from 'Appendix: Overview of Smart Leverage Backtests'
    // TrendXplorer 2018
    // https://drive.google.com/drive/folders/1V0C3IHuPrc6_uUaOdXM9zYJ3iWZFqzfD?usp=sharing

    #region DAA-G4: The Non-Leveraged Benchmark
    public class Keller_DAA_G4_NL : Keller_DAA_Core
    {
        // DAA-G4 T3B1 P4=R4 C2
        // Signals: R4:SPY,VEA,VWO,BND + C2:SHY,IEF
        // Trades: R4:SPY,VEA,VWO,BND + C2:SHY,IEF
        public override string Name => "Keller's DAA-G4: Non-Leveraged Benchmark";
        protected override List<string> RISKY_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };

        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };

        protected override int T => 3; // (risky) top parameter
        protected override int B => 1; // breadth parameter
    }
    #endregion
    #region DAA-G4: Limited Double Leverage
    public class Keller_DAA_G4_L2X : Keller_DAA_Core
    {
        // DAA-G4 T3B1 R4=P4 C2
        // Signals: SPY,VEA,VWO,BND + C2:SHY,IEF
        // Trades: SSO,EFO,VWO,BND + C2:SHY,UST
        public override string Name => "Keller's DAA-G4: Limited Double Leverage";
        protected override List<string> RISKY_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };
        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };

        protected override int T => 3; // (risky) top parameter
        protected override int B => 1; // breadth parameter
        public override void Run()
        {
            ASSET_SUB = new Dictionary<string, DataSource>();

            ASSET_SUB.Add("SPY", AddDataSource("SSO"));
            ASSET_SUB.Add("VEA", AddDataSource("EFO"));
            ASSET_SUB.Add("IEF", AddDataSource("UST"));

            base.Run();
        }
    }
    #endregion
    #region DAA-G4: Expanded Double Leverage
    public class Keller_DAA_G4_E2X : Keller_DAA_Core
    {
        // DAA-G4 T3B1 R4=P4 C2
        // Signals: SPY,VEA,VWO,BND + C2:SHY,IEF
        // Trades: SSO,EFO,EET,BND + C2:SHY,UST
        public override string Name => "Keller's DAA-G4: Expanded Double Leverage";
        protected override List<string> RISKY_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };
        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };

        protected override int T => 3; // (risky) top parameter
        protected override int B => 1; // breadth parameter
        public override void Run()
        {
            ASSET_SUB = new Dictionary<string, DataSource>();

            ASSET_SUB.Add("SPY", AddDataSource("SSO"));
            ASSET_SUB.Add("VEA", AddDataSource("EFO"));
            ASSET_SUB.Add("VWO", AddDataSource("EET"));
            ASSET_SUB.Add("IEF", AddDataSource("UST"));

            base.Run();
        }
    }
    #endregion
    #region DAA-G4: Limited Triple Leverage
    public class Keller_DAA_G4_L3X : Keller_DAA_Core
    {
        // DAA-G4 T3B1 R4=P4 C2
        // Signals: SPY,VEA,VWO,BND + C2:SHY,IEF
        // Trades: UPRO,DZK,VWO,BND + C2:SHY,TYD
        public override string Name => "Keller's DAA-G4: Limited Triple Leverage";
        protected override List<string> RISKY_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };
        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };

        protected override int T => 3; // (risky) top parameter
        protected override int B => 1; // breadth parameter
        public override void Run()
        {
            ASSET_SUB = new Dictionary<string, DataSource>();

            ASSET_SUB.Add("SPY", AddDataSource("UPRO"));
            ASSET_SUB.Add("VEA", AddDataSource("DZK"));
            ASSET_SUB.Add("IEF", AddDataSource("TYD"));

            base.Run();
        }
    }
    #endregion
    #region DAA-G4: Expanded Triple Leverage
    public class Keller_DAA_G4_E3X : Keller_DAA_Core
    {
        // DAA-G4 T3B1 R4=P4 C2
        // Signals: SPY,VEA,VWO,BND + C2:SHY,IEF
        // Trades: UPRO,DZK,EDC,BND + C2:SHY,TYD
        public override string Name => "Keller's DAA-G4: Expanded Triple Leverage";
        protected override List<string> RISKY_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };
        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "VEA", // Vanguard FTSE Developed Markets ETF
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND", // Vanguard Total Bond Market ETF
        };

        protected override int T => 3; // (risky) top parameter
        protected override int B => 1; // breadth parameter
        public override void Run()
        {
            ASSET_SUB = new Dictionary<string, DataSource>();

            ASSET_SUB.Add("SPY", AddDataSource("UPRO"));
            ASSET_SUB.Add("VEA", AddDataSource("DZK"));
            ASSET_SUB.Add("VWO", AddDataSource("EDC"));
            ASSET_SUB.Add("IEF", AddDataSource("TYD"));

            base.Run();
        }
    }
    #endregion
    #region DAA-G12: The Non-Leveraged Benchmark
    public class Keller_DAA_G12_NL : Keller_DAA_Core
    {
        // DAA-G12 T6B1 R12 C2 P2 (VWO,BND)
        // Signals: SPY,QQQ,IWM,VGK,EWJ,VWO,GSG,GLD,VNQ,HYG,TLT,LQD + C2:SHY,IEF
        // Trades:  SPY,QQQ,IWM,VGK,EWJ,VWO,GSG,GLD,VNQ,HYG,TLT,LQD + C2:SHY,IEF
        public override string Name => "Keller's DAA-G12: Non-Leveraged Benchmark";
        protected override List<string> RISKY_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "QQQ", // Invesco Nasdaq-100 ETF
            "IWM", // iShares Russell 2000 ETF
            "VGK", // Vanguard FTSE Europe ETF
            "EWJ", // iShares MSCI Japan ETF
            "VWO", // Vanguard MSCI Emerging Markets ETF
            "GSG", // iShares S&P GSCI Commodity-Indexed Trust
            "GLD", // SPDR Gold Trust ETF
            "VNQ", // Vanguard Real Estate ETF
            "HYG", // iShares iBoxx High Yield Corporate Bond ETF
            "TLT", // iShares 20+ Year Treasury Bond ETF
            "LQD"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };

        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
        {
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND"  // Vanguard Total Bond Market ETF
        };

        protected override int T => 6; // (risky) top parameter
        protected override int B => 1; // breadth parameter
    }
    #endregion
    #region DAA-G12: Limited Double Leverage
    public class Keller_DAA_G12_L2X : Keller_DAA_Core
    {
        // DAA-G12 T6B1 R12 C3 P2 (VWO,BND)
        // Signals: SPY,QQQ,IWM,VGK,EWJ,VWO,GSG,GLD,VNQ,HYG,LQD,TLT + C2:SHY,IEF
        // Trades:  SSO,QLD,UWM,VGK,EWJ,VWO,GSG,GLD,URE,HYG,LQD,UBT + C2:SHY,UST
        public override string Name => "Keller's DAA-G12: Limited Double Leverage";
        protected override List<string> RISKY_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "QQQ", // Invesco Nasdaq-100 ETF
            "IWM", // iShares Russell 2000 ETF
            "VGK", // Vanguard FTSE Europe ETF
            "EWJ", // iShares MSCI Japan ETF
            "VWO", // Vanguard MSCI Emerging Markets ETF
            "GSG", // iShares S&P GSCI Commodity-Indexed Trust
            "GLD", // SPDR Gold Trust ETF
            "VNQ", // Vanguard Real Estate ETF
            "HYG", // iShares iBoxx High Yield Corporate Bond ETF
            "LQD", // iShares iBoxx Investment Grade Corporate Bond ETF
            "TLT", // iShares 20+ Year Treasury Bond ETF
        };

        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
        {
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND"  // Vanguard Total Bond Market ETF
        };

        protected override int T => 6; // (risky) top parameter
        protected override int B => 1; // breadth parameter
        public override void Run()
        {
            ASSET_SUB = new Dictionary<string, DataSource>();

            ASSET_SUB.Add("SPY", AddDataSource("SSO"));
            ASSET_SUB.Add("QQQ", AddDataSource("QLD"));
            ASSET_SUB.Add("IWM", AddDataSource("UWM"));
            ASSET_SUB.Add("VNQ", AddDataSource("URE"));
            ASSET_SUB.Add("TLT", AddDataSource("UBT"));
            ASSET_SUB.Add("IEF", AddDataSource("UST"));

            base.Run();
        }
    }
    #endregion
    #region DAA-G12: Expanded Double Leverage
    public class Keller_DAA_G12_E2X : Keller_DAA_Core
    {
        // DAA-G12 T6B1 R12 C3 P2 (VWO,BND)
        // Signals: SPY,QQQ,IWM,VGK,EWJ,VWO,GSG,GLD,VNQ,HYG,LQD,TLT + C2:SHY,IEF
        // Trades:  SSO,QLD,UWM,VGK,EWJ,EET,GSG,GLD,URE,HYG,LQD,UBT + C2:SHY,UST
        public override string Name => "Keller's DAA-G12: Expanded Double Leverage";
        protected override List<string> RISKY_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "QQQ", // Invesco Nasdaq-100 ETF
            "IWM", // iShares Russell 2000 ETF
            "VGK", // Vanguard FTSE Europe ETF
            "EWJ", // iShares MSCI Japan ETF
            "VWO", // Vanguard MSCI Emerging Markets ETF
            "GSG", // iShares S&P GSCI Commodity-Indexed Trust
            "GLD", // SPDR Gold Trust ETF
            "VNQ", // Vanguard Real Estate ETF
            "HYG", // iShares iBoxx High Yield Corporate Bond ETF
            "LQD", // iShares iBoxx Investment Grade Corporate Bond ETF
            "TLT", // iShares 20+ Year Treasury Bond ETF
        };

        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
        {
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND"  // Vanguard Total Bond Market ETF
        };

        protected override int T => 6; // (risky) top parameter
        protected override int B => 1; // breadth parameter
        public override void Run()
        {
            ASSET_SUB = new Dictionary<string, DataSource>();

            ASSET_SUB.Add("SPY", AddDataSource("SSO"));
            ASSET_SUB.Add("QQQ", AddDataSource("QLD"));
            ASSET_SUB.Add("IWM", AddDataSource("UWM"));
            ASSET_SUB.Add("VWO", AddDataSource("EET"));
            ASSET_SUB.Add("VNQ", AddDataSource("URE"));
            ASSET_SUB.Add("TLT", AddDataSource("UBT"));
            ASSET_SUB.Add("IEF", AddDataSource("UST"));

            base.Run();
        }
    }
    #endregion
    #region DAA-G12: Limited Triple Leverage
    public class Keller_DAA_G12_L3X : Keller_DAA_Core
    {
        // DAA-G12 T6B1 R12 C3 P2 (VWO,BND)
        // Signals: SPY, QQQ, IWM,VGK,EWJ,VWO,GSG,GLD,VNQ,HYG,LQD,TLT + C2:SHY,IEF
        // Trades:  UPRO,TQQQ,TNA,VGK,EWJ,VWO,GSG,GLD,DRN,HYG,LQD,TMF + C2:SHY,TYD
        public override string Name => "Keller's DAA-G12: Limited Triple Leverage";
        protected override List<string> RISKY_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "QQQ", // Invesco Nasdaq-100 ETF
            "IWM", // iShares Russell 2000 ETF
            "VGK", // Vanguard FTSE Europe ETF
            "EWJ", // iShares MSCI Japan ETF
            "VWO", // Vanguard MSCI Emerging Markets ETF
            "GSG", // iShares S&P GSCI Commodity-Indexed Trust
            "GLD", // SPDR Gold Trust ETF
            "VNQ", // Vanguard Real Estate ETF
            "HYG", // iShares iBoxx High Yield Corporate Bond ETF
            "LQD", // iShares iBoxx Investment Grade Corporate Bond ETF
            "TLT", // iShares 20+ Year Treasury Bond ETF
        };

        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
        {
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND"  // Vanguard Total Bond Market ETF
        };

        protected override int T => 6; // (risky) top parameter
        protected override int B => 1; // breadth parameter
        public override void Run()
        {
            ASSET_SUB = new Dictionary<string, DataSource>();

            ASSET_SUB.Add("SPY", AddDataSource("UPRO"));
            ASSET_SUB.Add("QQQ", AddDataSource("TQQQ"));
            ASSET_SUB.Add("IWM", AddDataSource("TNA"));
            ASSET_SUB.Add("VNQ", AddDataSource("DRN"));
            ASSET_SUB.Add("TLT", AddDataSource("TMF"));
            ASSET_SUB.Add("IEF", AddDataSource("TYD"));

            base.Run();
        }
    }
    #endregion
    #region DAA-G12: Expanded Triple Leverage
    public class Keller_DAA_G12_E3X : Keller_DAA_Core
    {
        // DAA-G12 T6B1 R12 C3 P2 (VWO,BND)
        // Signals: SPY, QQQ, IWM,VGK,EWJ,VWO,GSG,GLD,VNQ,HYG,LQD,TLT + C2:SHY,IEF
        // Trades:  UPRO,TQQQ,TNA,VGK,EWJ,EDC,GSG,GLD,DRN,HYG,LQD,TMF + C2:SHY,TYD
        public override string Name => "Keller's DAA-G12: Expanded Triple Leverage";
        protected override List<string> RISKY_UNIVERSE => new List<string>
        {
            "SPY", // SPDR S&P 500 ETF
            "QQQ", // Invesco Nasdaq-100 ETF
            "IWM", // iShares Russell 2000 ETF
            "VGK", // Vanguard FTSE Europe ETF
            "EWJ", // iShares MSCI Japan ETF
            "VWO", // Vanguard MSCI Emerging Markets ETF
            "GSG", // iShares S&P GSCI Commodity-Indexed Trust
            "GLD", // SPDR Gold Trust ETF
            "VNQ", // Vanguard Real Estate ETF
            "HYG", // iShares iBoxx High Yield Corporate Bond ETF
            "LQD", // iShares iBoxx Investment Grade Corporate Bond ETF
            "TLT", // iShares 20+ Year Treasury Bond ETF
        };

        protected override List<string> CASH_UNIVERSE => new List<string>
        {
            "SHY", // iShares 1-3 Year Treasury Bond ETF
            "IEF", // iShares 7-10 Year Treasury Bond ETF
        };

        protected override List<string> PROTECTIVE_UNIVERSE => new List<string>
        {
            "VWO", // Vanguard FTSE Emerging Markets ETF
            "BND"  // Vanguard Total Bond Market ETF
        };

        protected override int T => 6; // (risky) top parameter
        protected override int B => 1; // breadth parameter
        public override void Run()
        {
            ASSET_SUB = new Dictionary<string, DataSource>();

            ASSET_SUB.Add("SPY", AddDataSource("UPRO"));
            ASSET_SUB.Add("QQQ", AddDataSource("TQQQ"));
            ASSET_SUB.Add("IWM", AddDataSource("TNA"));
            ASSET_SUB.Add("VWO", AddDataSource("EDC"));
            ASSET_SUB.Add("VNQ", AddDataSource("DRN"));
            ASSET_SUB.Add("TLT", AddDataSource("TMF"));
            ASSET_SUB.Add("IEF", AddDataSource("TYD"));

            base.Run();
        }
    }
    #endregion
#endif
}

//==============================================================================
// end of file
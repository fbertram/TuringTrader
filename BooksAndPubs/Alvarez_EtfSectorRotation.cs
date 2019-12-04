//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Alvarez_EtfSectorRotation
// Description: Strategy, as published on Cesar Alvarez' blog
//              https://alvarezquanttrading.com/blog/etf-sector-rotation/
// History:     2019iii18, FUB, created
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TuringTrader.Indicators;
using TuringTrader.Simulator;

namespace TuringTrader.BooksAndPubs
{
    public class Alvarez_EtfSectorRotation : Algorithm
    {
        public override string Name => "ETF Sector Rotation";

        private static readonly string[] UNIVERSE =
        {
            Assets.STOCKS_US_SECT_DISCRETIONARY,
            Assets.STOCKS_US_SECT_STAPLES,
            Assets.STOCKS_US_SECT_ENERGY,
            Assets.STOCKS_US_SECT_FINANCIAL,
            Assets.STOCKS_US_SECT_HEALTH_CARE,
            Assets.STOCKS_US_SECT_INDUSTRIAL,
            Assets.STOCKS_US_SECT_MATERIALS,
            Assets.STOCKS_US_SECT_TECHNOLOGY,
            Assets.STOCKS_US_SECT_UTILITIES,
            //Assets.STOCKS_US_SECT_COMMUNICATION, // Communication Services
            //Assets.STOCKS_US_SECT_REAL_ESTATE, // Real Estate
        };
        private static readonly string SAFE_INSTRUMENT = Assets.BONDS_US_TREAS_30Y;
        private static readonly string BENCHMARK = Assets.STOCKS_US_LG_CAP;
        private static readonly int RANK1_DAYS = 252;
        private static readonly int RANK2_DAYS = 126;

        private Plotter _plotter = new Plotter();

        public override void Run()
        {
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;

            var universe = AddDataSources(UNIVERSE);
            var safeInstrument = AddDataSource(SAFE_INSTRUMENT);
            var benchmark = AddDataSource(BENCHMARK);

            Deposit(1e6);
            CommissionPerShare = 0.015;

            foreach (var s in SimTimes)
            {
                //----- skip until all required instruments are valid
                if (!HasInstruments(universe) 
                || !HasInstrument(safeInstrument)
                || !HasInstrument(benchmark))
                    continue;


                //----- memorize our momentum
                // its good practice to do this, to make sure
                // indicators are only evaluated once
                var momentum1 = universe
                    .ToDictionary(
                        ds => ds.Instrument,
                        ds => ds.Instrument.Close.Momentum(RANK1_DAYS)[0]);

                var momentum2 = universe
                    .ToDictionary(
                        ds => ds.Instrument,
                        ds => ds.Instrument.Close.Momentum(RANK2_DAYS)[0]);

                //----- rank universe by momentum
                var rank1 = universe
                    .OrderByDescending(ds => momentum1[ds.Instrument])
                    .Select((ds, n) => new { instr = ds.Instrument, rank = n, mom = momentum1[ds.Instrument] })
                    .ToDictionary(
                        i => i.instr,
                        i => i);

                var rank2 = universe
                    .OrderByDescending(ds => momentum2[ds.Instrument])
                    .Select((ds, n) => new { instr = ds.Instrument, rank = n, mom = momentum2[ds.Instrument] })
                    .ToDictionary(
                        i => i.instr,
                        i => i);

                var rank3 = universe
                    .OrderBy(ds => 1.001 * rank1[ds.Instrument].rank + rank2[ds.Instrument].rank) // use rank1 as tie break
                    .Select((ds, n) => new { instr = ds.Instrument, rank = n, sum = 1.001 * rank1[ds.Instrument].rank + rank2[ds.Instrument].rank })
                    .ToDictionary(
                        i => i.instr,
                        i => i);

                //----- select our 2 top ranking instruments
#if true
                // this is what Cesar Alvarez seems to be describing
                // in his blog post. however, the results are nowhere close
                // to what he  published.
                var top2 = rank3
                    .OrderBy(i => i.Value.rank)
                    .Take(2)
                    .ToDictionary(
                        i => i.Key,
                        i => i.Value);
#else
                // this is probably what Cesar Alvarez has simulated,
                // as the results seem to match those published
                // on the blog closely.
                // this is chosing the 2 _worst_ ranked sectors,
                // making this a mean-reversion strategy
                var top2 = rank3
                    .OrderByDescending(i => i.Value.rank)
                    .Take(2)
                    .ToDictionary(
                        i => i.Key,
                        i => i.Value);
#endif

                //----- assign weights
                var weights = universe
                    .ToDictionary(
                        ds => ds.Instrument,
                        ds => top2.ContainsKey(ds.Instrument)
                            ? (ds.Instrument.Close[0] > ds.Instrument.Close[252] ? 0.5 : 0.0)
                            : 0.0);

                weights[safeInstrument.Instrument] = 1.0 - weights.Sum(i => i.Value);

                //----- trade instruments
                if (SimTime[0].Month != SimTime[1].Month)
                {
                    foreach (var i in weights.Keys)
                    {
                        var targetShares = (int)Math.Floor(weights[i] * NetAssetValue[0] / i.Close[0]);
                        i.Trade(targetShares - i.Position);
                    }
                }

                //---- plot output
                _plotter.SelectChart(Name, "Date");
                _plotter.SetX(SimTime[0]);
                _plotter.Plot(Name, NetAssetValue[0]);
                _plotter.Plot(benchmark.Instrument.Name, benchmark.Instrument.Close[0]);

                _plotter.SelectChart("Strategy Positions", "Date");
                _plotter.SetX(SimTime[0]);
                foreach (var ds in universe)
                    _plotter.Plot(ds.Instrument.Symbol, ds.Instrument.Position * ds.Instrument.Close[0] / NetAssetValue[0]);
            }
        }

        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
    }
}

//==============================================================================
// end of file
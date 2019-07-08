//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Alvarez_EtfSectorRotation
// Description: Strategy, as published on Cesar Alvarez' blog
//              https://alvarezquanttrading.com/blog/etf-sector-rotation/
// History:     2019iii18, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Indicators;
using TuringTrader.Simulator;

namespace BooksAndPubs
{
    public class Alvarez_EtfSectorRotation : Algorithm
    {
        private static readonly string[] UNIVERSE =
        {
            "XLY", // consumer discretionary
            "XLP", // consumer staples
            "XLE", // energy
            "XLF", // financials
            "XLV", // health care
            "XLI", // industrials
            "XLB", // materials
            "XLK", // technology
            "XLU", // utilities
        };
        private static readonly string SAFE_INSTRUMENT = "TLT"; // 20+ year treasury bonds
        private static readonly string BENCHMARK = "$SPX"; // S&P 500
        private static readonly int RANK1_DAYS = 252;
        private static readonly int RANK2_DAYS = 126;

        private Plotter _plotter = new Plotter();
        private List<Instrument> _universe = null;
        private Instrument _safe_instrument = null;
        private Instrument _benchmark = null;

        public override void Run()
        {
            StartTime = DateTime.Parse("01/01/1990");
            EndTime = DateTime.Now - TimeSpan.FromDays(5);

            foreach (var nick in UNIVERSE)
                AddDataSource(nick);
            AddDataSource(SAFE_INSTRUMENT);
            AddDataSource(BENCHMARK);

            Deposit(1e6);
            CommissionPerShare = 0.015;

            foreach (var s in SimTimes)
            {
                //----- skip until all required instruments are valid
                if (_universe == null)
                {
                    if (!HasInstrument(BENCHMARK)
                    || !HasInstrument(SAFE_INSTRUMENT)
                    || !HasInstruments(UNIVERSE))
                        continue;
                }

                //----- memorize our instruments
                _universe = _universe ?? Instruments
                    .Where(i => UNIVERSE.Contains(i.Nickname))
                    .ToList();
                _safe_instrument = _safe_instrument ?? FindInstrument(SAFE_INSTRUMENT);
                _benchmark = _benchmark ?? FindInstrument(BENCHMARK);

                //----- memorize our momentum
                // its good practice to do this, to make sure
                // indicators are only evaluated once
                var momentum1 = _universe
                    .ToDictionary(
                        i => i,
                        i => i.Close.Momentum(RANK1_DAYS)[0]);

                var momentum2 = _universe
                    .ToDictionary(
                        i => i,
                        i => i.Close.Momentum(RANK2_DAYS)[0]);

                //----- rank universe by momentum
                var rank1 = _universe
                    .OrderByDescending(i => momentum1[i])
                    .Select((i, n) => new { instr = i, rank = n, mom = momentum1[i] })
                    .ToDictionary(
                        i => i.instr,
                        i => i);

                var rank2 = _universe
                    .OrderByDescending(i => momentum2[i])
                    .Select((i, n) => new { instr = i, rank = n, mom = momentum2[i] })
                    .ToDictionary(
                        i => i.instr,
                        i => i);

                var rank3 = _universe
                    .OrderBy(i => 1.001 * rank1[i].rank + rank2[i].rank) // use rank1 as tie break
                    .Select((i, n) => new { instr = i, rank = n, sum = 1.001 * rank1[i].rank + rank2[i].rank })
                    .ToDictionary(
                        i => i.instr,
                        i => i);

                //----- select our 2 top ranking instruments
#if true
                // this is what Cesar Alvarez seems to be describing
                // in his blog post. however, the results are nowhere close
                // to what he has published.
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
                var weights = _universe
                    .ToDictionary(
                        i => i,
                        i => top2.ContainsKey(i)
                            ? (i.Close[0] > i.Close[252] ? 0.5 : 0.0)
                            : 0.0);

                weights[_safe_instrument] = 1.0 - weights.Sum(i => i.Value);

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
                _plotter.SelectChart(Name, "date");
                _plotter.SetX(SimTime[0]);
                _plotter.Plot("NAV", NetAssetValue[0]);
                _plotter.Plot(_benchmark.Symbol, _benchmark.Close[0]);

                _plotter.SelectChart("Holdings", "date");
                _plotter.SetX(SimTime[0]);
                foreach (var i in _universe)
                    _plotter.Plot(i.Symbol, i.Position * i.Close[0] / NetAssetValue[0]);
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
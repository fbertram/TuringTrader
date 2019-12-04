//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Benchmarks
// Description: Simple benchmarking portfolios.
// History:     2019xii04, FUB, created
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
using System.Linq;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    /// <summary>
    /// 60/40 Portfolio Benchmark. This could also be achieved with
    /// VBIAX or VBINX
    /// </summary>
    public class Benchmark_60_40 : SubclassableAlgorithm
    {
        public override string Name => "60/40 Portfolio";

        #region internal data
        private static readonly string STOCKS = Assets.STOCKS_US_LG_CAP;
        private static readonly string BONDS = Assets.BONDS_US_TOTAL;
        private static readonly string BENCHMARK = Assets.STOCKS_US_LG_CAP;
        private static readonly double STOCK_ALLOC = 0.6;

        private Plotter _plotter;
        #endregion
        #region ctor
        public Benchmark_60_40()
        {
            _plotter = new Plotter(this);
        }
        #endregion
        #region public override void Run()
        public override void Run()
        {
            StartTime = SubclassedStartTime ?? Globals.START_TIME;
            EndTime = SubclassedEndTime ?? Globals.END_TIME;

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = 0.0; // benchmark w/o commissions

            var stocks = AddDataSource(STOCKS);
            var bonds = AddDataSource(BONDS);
            var bench = AddDataSource(BENCHMARK);

            foreach (var s in SimTimes)
            {
                if (!HasInstrument(stocks) || !HasInstrument(bonds) || !HasInstrument(bench))
                    continue;

                int targetStockShares = (int)Math.Floor(NetAssetValue[0] * STOCK_ALLOC / stocks.Instrument.Close[0]);
                int targetBondShares = (int)Math.Floor(NetAssetValue[0] * (1.0 - STOCK_ALLOC) / bonds.Instrument.Close[0]);

                stocks.Instrument.Trade(targetStockShares - stocks.Instrument.Position);
                bonds.Instrument.Trade(targetBondShares - bonds.Instrument.Position);

                _plotter.SelectChart(Name, "date");
                _plotter.SetX(SimTime[0].Date);
                _plotter.Plot(Name, NetAssetValue[0]);
                _plotter.Plot(bench.Instrument.Name, bench.Instrument.Close[0]);

                AddSubclassedBar();
            }
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
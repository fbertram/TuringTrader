//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo09_WalkForwardOptimization
// Description: demonstrate simple strategy w/ walk-forward-optimization.
// History:     2020viiii15, FUB, created
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
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TuringTrader.Simulator;
#endregion

namespace Demos
{
    #region Demo09_WalkForwardOptimization
    public class Demo09_WalkForwardOptimization : Algorithm
    {
        #region optimizable parameters
        [OptimizerParam(0, 100, 5)]
        public int STOCK_PCNT = 60;
        #endregion
        #region internal data
        private Plotter _plotter = new Plotter();
        #endregion

        #region OptimizeSettings - walk-forward-optimization
        private void OptimizeSettings()
        {
            // we only optimize settings on the top instance,
            // not those used for walk-forward optimization
            if (!IsOptimizing)
            {
                // enable optimizer parameters
                foreach (var s in OptimizerParams)
                    s.Value.IsEnabled = true;

                // run optimization
                var optimizer = new OptimizerGrid(this, false);
                var end = SimTime[0];
                var start = end - TimeSpan.FromDays(90);
                optimizer.Run(start, end);

                // apply parameters from best result
                var best = optimizer.Results
                    .OrderByDescending(r => r.Fitness)
                    .FirstOrDefault();
                optimizer.SetParametersFromResult(best);
            }
        }
        #endregion
        #region Run - algorithm core
        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            StartTime = startTime != null ? (DateTime)startTime : DateTime.Parse("01/01/2007", CultureInfo.InvariantCulture);
            EndTime = endTime != null ? (DateTime)endTime : DateTime.Now - TimeSpan.FromDays(5);
            WarmupStartTime = StartTime - TimeSpan.FromDays(90);

            CommissionPerShare = 0.015;
            Deposit(1e6);

            var stocks = AddDataSource("SPY");
            var bonds = AddDataSource("TLT");

            bool firstOptimization = true;
            foreach (var s in SimTimes)
            {
                // re-tune parameters on a monthly schedule
                if (firstOptimization || NextSimTime.Month != SimTime[0].Month)
                    OptimizeSettings();
                firstOptimization = false;

                // rebalance on a monthly schedule
                if (NextSimTime.Month != SimTime[0].Month)
                {
                    var stockPcnt = STOCK_PCNT / 100.0;
                    var stockShares = (int)Math.Floor(NetAssetValue[0] * stockPcnt / stocks.Instrument.Close[0]);
                    stocks.Instrument.Trade(stockShares - stocks.Instrument.Position);

                    var bondPcnt = 1.0 - stockPcnt;
                    var bondShares = (int)Math.Floor(NetAssetValue[0] * bondPcnt / bonds.Instrument.Close[0]);
                    bonds.Instrument.Trade(bondShares - bonds.Instrument.Position);
                }

                // strategy output
                if (!IsOptimizing && TradingDays > 0)
                {
                    _plotter.SelectChart("Net Asset Value", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Stock/Bond Strategy", NetAssetValue[0]);
                    _plotter.Plot("S&P 500", stocks.Instrument.Close[0]);

                    _plotter.SelectChart("Stock Percentage", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Stock Percentage", 100.0 * stocks.Instrument.Position * stocks.Instrument.Close[0] / NetAssetValue[0]);
                }
            }

            // fitness value used for walk-forward-optimization
            FitnessValue = NetAssetValue[0] / NetAssetValueMaxDrawdown;

            yield break;
        }
        #endregion
        #region Report - output chart
        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
        #endregion
    }
    #endregion
}

//==============================================================================
// end of file
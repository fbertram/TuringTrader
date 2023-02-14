//==============================================================================
// Project:     TuringTrader, demo algorithms
// Name:        Demo08_CustomData
// Description: demonstrate implementation of custom data source.
// History:     2020vii13, FUB, created
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
using System.Threading;
using TuringTrader.Simulator;
#endregion

namespace Demos
{
    #region Demo08_CustomData
    public class Demo08_CustomData : Algorithm
    {
        #region custom data source
        private class MyDataSource : DataSource
        {
            public MyDataSource() : base(new Dictionary<DataSourceParam, string>())
            {
                // initialize the info property
                Info[DataSourceParam.nickName] = "MySource";
                Info[DataSourceParam.name] = "my custom data source";
                Info[DataSourceParam.ticker] = "MYBAR";
            }
            public override IEnumerable<Bar> LoadData(DateTime startTime, DateTime endTime)
            {
                // note: TuringTrader's built-in data sources store their
                // data in the global cache. curious coders are encouraged
                // to review the code of those data sources to learn how
                // to do that.

                for (DateTime t = startTime; t <= endTime; t += TimeSpan.FromDays(1))
                {
                    var v = t.Day + 31.0 * t.Month + 366.0 * t.Year;
                    var b = Bar.NewOHLC(
                        Info[DataSourceParam.ticker], t, // ticker + timestamp
                        v, v, v, v, 100);                // OHLC + volume

                    yield return b;
                }
            }
        }
        #endregion

        #region algorithm using custom data source
        private Plotter _plotter = new Plotter();
        public override void Run()
        {
            StartTime = DateTime.Parse("01/01/2008", CultureInfo.InvariantCulture);
            EndTime = DateTime.Now.Date - TimeSpan.FromDays(5);

            var ds = new MyDataSource();
            AddDataSource(ds);

            foreach (var s in SimTimes)
            {
                _plotter.SelectChart(Name, "date");
                _plotter.SetX(SimTime[0]);
                _plotter.Plot(ds.Instrument.Name, ds.Instrument.Close[0]);
            }
        }

        public override void Report()
        {
            _plotter.OpenWith("SimpleChart");
        }
        #endregion

        #region validate simulator timestamps
        protected override bool IsValidSimTime(DateTime timestamp)
        {
            // TuringTrader's default implementation for IsValidSimTime
            // recoginizes U.S. stock exchange trading hours:
            // Mo - Fr, 9:30am - 4pm

            // when creating your own data sources, you might also want
            // to adjust these hours.

            return true;
        }
        #endregion
    }
    #endregion
}
//==============================================================================
// end of file
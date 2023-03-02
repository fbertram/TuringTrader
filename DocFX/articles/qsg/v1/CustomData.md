# Creating Custom Data Sources

unfortunately, we didn't write this article yet. However, here is some fully functioning demo code:

```c#

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using TuringTrader.Simulator;
#endregion

namespace Demos
{
    #region Demo08_DataSources
    public class Demo_CustomDataSource : Algorithm
    {
        #region custom data source
        private class MyDataSource : DataSource
        {
            public MyDataSource() : base (new Dictionary<DataSourceParam, string>())
            {
                // initialize the info property
                Info[DataSourceParam.nickName] = "MySource";
                Info[DataSourceParam.name] = "my custom data source";
                Info[DataSourceParam.ticker] = "MYBAR";
            }
            public override void LoadData(DateTime startTime, DateTime endTime)
            {
                // create an enumerable of bars
                // and assign it to the Data property

                // note: TuringTrader's built-in data sources store their
                // data in the global cache. curious coders are encouraged
                // to review the code of those data sources to learn how
                // to do that.

                var data = new List<Bar>();

                for (DateTime t = startTime; t <= endTime; t += TimeSpan.FromDays(1))
                {
                    var v = t.Day + 31.0 * t.Month + 366.0 * t.Year;
                    var b = Bar.NewOHLC(
                        Info[DataSourceParam.ticker], t, // ticker + timestamp
                        v, v, v, v, 100);                // OHLC + volume
                    data.Add(b);
                }

                Data = data;
                FirstTime = startTime;
                LastTime = endTime;
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
    }
    #endregion
}

```


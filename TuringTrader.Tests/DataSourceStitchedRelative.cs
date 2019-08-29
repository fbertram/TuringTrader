using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using TuringTrader.Simulator;

namespace TuringTrader.Tests
{
    class DataSourceStitchedRelative : DataSource
    {
        public List<string> Symbols;

        public DataSourceStitchedRelative(Dictionary<DataSourceParam, string> info) : base(info)
        {

        }

        override public void LoadData(DateTime startTime, DateTime endTime)
        {

        }
    }

    [TestClass]
    class TestDataSourceStitchedRelative
    {
        [TestMethod]
        public void Test_DataRetrieval()
        {
            var info = new Dictionary<DataSourceParam, string>
            {
                { DataSourceParam.ticker, "SPY++" },
            };
            var ds = new DataSourceStitchedRelative(info);
            ds.Symbols = new List<string> { "SPY", "$SPXTR" };

            ds.LoadData(DateTime.Parse("01/01/1970"), DateTime.Parse("01/03/2019"));
        }
    }
}

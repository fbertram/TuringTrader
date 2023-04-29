//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        OptionSupport
// Description: unit test for option support class.
// History:     2019i14, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
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

#if false
// does not work as of 2023iv17. It seems something w/ the datasources has changed,
// which is unrelated to the purpose of this test case

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Simulator;
using TuringTrader.Support;

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class OptionSupport
    {
        #region class TestSimulator
        private class TestSimulator : Algorithm
        {
            private List<DataSource> _testDataSources;
            private double _iv;
            private double _delta;
            private double _gamma;
            private double _theta;
            private double _vega;
            private double _riskFree;
            private double _divYield;

            public TestSimulator(List<DataSource> testDataSources, double riskFree, double divYield, double iv, double delta, double gamma, double theta, double vega)
            {
                _testDataSources = testDataSources;

                _riskFree = riskFree;
                _divYield = divYield;

                _iv = iv;
                _delta = delta;
                _gamma = gamma;
                _theta = theta;
                _vega = vega;

                Run();
            }

            public override void Run()
            {
                StartTime = DateTime.Parse("01/01/2000");
                EndTime = DateTime.Parse("12/31/2019");

                foreach (var dataSource in _testDataSources)
                    AddDataSource(dataSource);

                foreach (DateTime simTime in SimTimes)
                {
                    Instrument underlying = Instruments
                        .Where(i => i.IsOption == false)
                        .First();

                    Instrument option = Instruments
                        .Where(i => i.IsOption == true)
                        .First();

                    double riskFreeRate = _riskFree;
                    double dividendYield = _divYield;

                    var iv = option.BlackScholesImplied(riskFreeRate, dividendYield);
                    var greeks = option.BlackScholes(iv.Volatility, riskFreeRate, dividendYield);

#if false
                   Output.WriteLine("impliedVol = {0:F8}, delta = {1:F8}, gamma = {2:F8}, theta = {3:F8}, vega = {4:F8}",
                        iv.Volatility, greeks.Delta, greeks.Gamma, greeks.Theta, greeks.Vega);
#else
                    //Assert.IsTrue(Math.Abs(iv.Price - greeks.Price) < 1e-5);
                    Assert.IsTrue(Math.Abs(iv.Volatility - _iv) < 1e-5);
                    Assert.IsTrue(Math.Abs(greeks.Delta - _delta) < 1e-5);
                    Assert.IsTrue(Math.Abs(greeks.Gamma - _gamma) < 1e-5);
                    Assert.IsTrue(Math.Abs(greeks.Theta - _theta) < 1e-5);
                    Assert.IsTrue(Math.Abs(greeks.Vega - _vega) < 1e-5);
#endif
                }
            }
        }
        #endregion
        #region class TestVector
        private class TestVector
        {
            //--- stimuli
            // underlying
            public DateTime quoteDate;
            public double underlyingLast;
            // market
            public double riskFreeRate;
            public double dividendYield;
            // option
            public DateTime expiration;
            public double strike;
            public bool isPut;
            public double bid;
            public double ask;

            //--- responses
            public double impliedVol;
            public double delta;
            public double gamma;
            public double theta;
            public double vega;

        }
        #endregion

        #region public void Test_PriceAndGreeks()
        [TestMethod]
        public void Test_PriceAndGreeks()
        {
            List<TestVector> testVectors = new List<TestVector>
            {
                new TestVector
                {
                    quoteDate = DateTime.Parse("10/01/2015"),
                    underlyingLast = 1921.42,
                    riskFreeRate = 0.024, dividendYield = 0.00, //0.018,
                    expiration = DateTime.Parse("10/16/2015"),
                    strike = 1845, isPut = false, bid = 85.80, ask = 90.00,
                    // impliedVol = 0.2538, delta = 0.798, gamma = 0.0029, theta = -351.149, vega = 107.226 // from historical data
                    impliedVol = 0.23699109, delta = 0.81315737, gamma = 0.00291045, theta = -337.13293066, vega = 104.57753610
                },
                new TestVector
                {
                    quoteDate = DateTime.Parse("10/01/2015"),
                    underlyingLast = 1921.42,
                    riskFreeRate = 0.024, dividendYield = 0.00, //0.018,
                    expiration = DateTime.Parse("10/16/2015"),
                    strike = 1980, isPut = false, bid = 6.80, ask = 8.20,
                    // impliedVol = 0.177, delta = 0.2018, gamma = 0.0042, theta = -242.777, vega = 107.183 // from historical data
                    impliedVol = 0.17021597, delta = 0.20473715, gamma = 0.00428357, theta = -238.35915581, vega = 110.54824762
                },
                new TestVector
                {
                    quoteDate = DateTime.Parse("10/01/2015"),
                    underlyingLast = 1921.42,
                    riskFreeRate = 0.024, dividendYield = 0.00, //0.018,
                    expiration = DateTime.Parse("10/16/2015"),
                    strike = 1845, isPut = true, bid = 9.40, ask = 11.60,
                    // impliedVol = 0.2472, delta = -0.1961, gamma = 0.0029, theta = -330.323, vega = 105.344 // from historical data
                    impliedVol = 0.24491080, delta = -0.19423270, gamma = 0.00288422, theta = -310.13506432, vega = 107.09810115
                },
                new TestVector
                {
                    quoteDate = DateTime.Parse("10/01/2015"),
                    underlyingLast = 1921.42,
                    riskFreeRate = 0.024, dividendYield = 0.00, //0.018,
                    expiration = DateTime.Parse("10/16/2015"),
                    strike = 1980, isPut = true, bid = 63.20, ask = 67.90,
                    // impliedVol = 0.1734, delta = -0.8032, gamma = 0.0042, theta = -227.891, vega = 105.562 // from historical data
                    impliedVol = 0.18275260, delta = -0.77809756, gamma = 0.00418151, theta = -220.34075530, vega = 115.86234498
                },
                new TestVector
                {
                    quoteDate = DateTime.Parse("01/04/2016"),
                    underlyingLast = 1993.680054,
                    riskFreeRate = 0.024, dividendYield = 0.00,
                    expiration = DateTime.Parse("02/26/2016"),
                    strike = 300, isPut = false, bid = 1695.00, ask = 1695.00,
                    //impliedVol = 1.9811, delta = 0.9973, gamma = 0.000004, theta = -0.064148, vega = 0.047015 // from historical data
                    impliedVol = 1.72686453, delta = 0.99934408, gamma = 0.00000174, theta = -17.45819022, vega = 1.73456198
                },
            };

            foreach (var testVector in testVectors)
            {
                //--- create data source for underlying
                Dictionary<DataSourceParam, string> underlyingInfos = new Dictionary<DataSourceParam, string>
                {
                    { DataSourceParam.name, "S&P 500 Index" }
                };

                List<Bar> underlyingBars = new List<Bar>
                {   new Bar(
                        "SPX", testVector.quoteDate,
                        testVector.underlyingLast, testVector.underlyingLast, testVector.underlyingLast, testVector.underlyingLast, 100, true,
                        default(double), default(double), default(long), default(long), false,
                        default(DateTime), default(double), default(bool))
                };
                DataSource underlyingDataSource = new DataSourceFromBars(underlyingBars, underlyingInfos);

                //--- create data source for option
                Dictionary<DataSourceParam, string> optionInfos = new Dictionary<DataSourceParam, string>
                {
                    { DataSourceParam.name, "S&P 500 Index Options" },
                    { DataSourceParam.optionUnderlying, "SPX" }
                };
                List<Bar> optionBars = new List<Bar>
                {   new Bar(
                        "SPX_Option", testVector.quoteDate,
                        default(double), default(double), default(double), default(double), default(long), false,
                        testVector.bid, testVector.ask, 100, 100, true,
                        testVector.expiration, testVector.strike, testVector.isPut)
                };
                DataSource optionDataSource = new DataSourceFromBars(optionBars, optionInfos);

                //--- run test
                TuringTrader.Simulator.SimulatorCore callSim = new TestSimulator(
                    new List<DataSource> { underlyingDataSource, optionDataSource },
                    testVector.riskFreeRate, testVector.dividendYield,
                    testVector.impliedVol, testVector.delta, testVector.gamma, testVector.theta, testVector.vega
                );
            }
        }
        #endregion
    }
}

#endif

//==============================================================================
// end of file
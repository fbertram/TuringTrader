//==============================================================================
// Project:     Trading Simulator
// Name:        IndicatorsMomentum
// Description: test momentum indicators
// History:     2018xi27, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TuringTrader.Simulator;

namespace Tests
{
    [TestClass]
    public class IndicatorsMomentum
    {
        [TestMethod]
        public void Test_LinearRegression()
        {
            /*
            https://stackoverflow.com/questions/5083465/fast-efficient-least-squares-fit-algorithm-in-c
            REAL x[6]= {1, 2, 4,  5,  10, 20};
            REAL y[6]= {4, 6, 12, 15, 34, 68};
            m=3.43651 b=-0.888889 r=0.999192    
            */

            ITimeSeries<double> series1 = IndicatorsBasic.Lambda(
                (t) => 100.0 + Math.Sin(2.0 * Math.PI * t / 10.0),
                0);
            double result1 = series1.LinRegression(10)[0];
            Assert.IsTrue(Math.Abs(result1 - 0.18653) < 1e-4);

            ITimeSeries<double> series2 = IndicatorsBasic.Lambda(
                (t) => 100.0 -2.5 * t,
                0);
            double result2 = series2.LinRegression(10)[0];
            Assert.IsTrue(Math.Abs(result2 - 2.5000) < 1e-4);
        }
    }
}

//==============================================================================
// end of file
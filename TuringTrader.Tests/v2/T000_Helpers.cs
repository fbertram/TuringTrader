//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        T000_Helpers
// Description: Unit test helper class.
// History:     2022xi30, FUB, created
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

#region libraries
using System;
#endregion

namespace TuringTrader.SimulatorV2.Tests
{
    public class T000_Helpers
    {
        public class StepResponse : Algorithm
        {
            public double Amplitude = 1.0;
            public override void Run()
            {
                //StartDate = DateTime.Parse("2022-01-02T16:00-05:00");
                //EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                SimLoop(() =>
                {
                    return new OHLCV(0.0, 0.0, 0.0, IsFirstBar ? 0.0 : Amplitude, 0.0);
                });
            }
        }

        public class NyquistFrequency : Algorithm
        {
            public double Amplitude = 1.0;
            public override void Run()
            {
                //StartDate = DateTime.Parse("2022-01-02T16:00-05:00");
                //EndDate = DateTime.Parse("2022-01-31T16:00-05:00");
                WarmupPeriod = TimeSpan.FromDays(0);

                var isZero = false;
                SimLoop(() =>
                {
                    isZero = !isZero;
                    return new OHLCV(0.0, 0.0, 0.0, isZero ? 0.0 : Amplitude, 0.0);
                });
            }
        }

        public class DoNothing : Algorithm { }
    }
}

//==============================================================================
// end of file
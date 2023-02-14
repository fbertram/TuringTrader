//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Assets/Benchmarks
// Description: Definitions for common benchmarks.
// History:     2022xi09, FUB, created
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

namespace TuringTrader.SimulatorV2.Assets
{
    /// <summary>
    /// Collection of common benchmarks.
    /// </summary>
    public class Benchmark
    {
        /// <summary>
        /// Vanilla 60/40 benchmark, created from 60% SPY and 40% AGG.
        /// </summary>
        public const string PORTFOLIO_60_40 = "algo:Benchmark_60_40";
    }
}

//==============================================================================
// end of file

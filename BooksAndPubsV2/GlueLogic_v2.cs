//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        GlueLogic_v2
// Description: Glue logic shared by algorithms.
// History:     2023ii11, FUB, created
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

using System;

namespace TuringTrader.GlueV2
{
    public class AlgorithmConstants
    {
        public static readonly DateTime START_DATE = DateTime.Parse("2007-01-01T16:00-05:00"); // 4pm in New York
        public static readonly DateTime END_DATE = DateTime.Now.Date - TimeSpan.FromDays(5);
        public static readonly double FRICTION = 0.0005; // 0.05% of transaction
    }
}

//==============================================================================
// end of file

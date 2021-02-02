//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Keller_LAA
// Description: Lethargic Asset Allocation (FAA) strategy, as published in 
//              Wouter J. Keller's paper 
//              'Lazy Momentum with Growth-Trend timing: 
//              Resilient Asset Allocation (RAA)'
//              https://ssrn.com/abstract=3752294
// History:     2021i20, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2021, Bertram Solutions LLC
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
using System.Linq;
using TuringTrader.Algorithms.Glue;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.BooksAndPubs
{
    #region Keller RAA Core
    public abstract class Keller_RAA_Core : AlgorithmPlusGlue
    {
    }
    #endregion
}

//==============================================================================
// end of file

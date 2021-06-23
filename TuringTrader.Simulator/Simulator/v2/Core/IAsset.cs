//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        IAsset
// Description: Asset interface.
// History:     2021iv24, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2021, Bertram Enterprises LLC
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TuringTrader.Simulator.v2
{
    /// <summary>
    /// Interface for tradeable assets.
    /// </summary>
    public interface IAsset
    {
        public Algorithm Algorithm { get; }
        public string CacheId { get; }
        public Task<object> Data { get; }
    }
}

//==============================================================================
// end of file

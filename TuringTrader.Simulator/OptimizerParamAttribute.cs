//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        OptimizerParamAttribute
// Description: optimizer parameter
// History:     2018x09, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
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

#region Libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// attribute class to set optimzation range of field or property
    /// </summary>
    public class OptimizerParamAttribute : Attribute
    {
        /// <summary>
        /// Start value of optimizer parameter.
        /// </summary>
        public readonly int Start;
        /// <summary>
        /// End value of optimizer parameter.
        /// </summary>
        public readonly int End;
        /// <summary>
        /// Step size of optimizer parameter.
        /// </summary>
        public readonly int Step;

        /// <summary>
        /// Create and initialize optimizer parameter attribute.
        /// </summary>
        /// <param name="start">start value</param>
        /// <param name="end">end value</param>
        /// <param name="increment">step size</param>
        public OptimizerParamAttribute(int start, int end, int increment)
        {
            Start = start;
            End = end;
            Step = increment;
        }
    }
}

//==============================================================================
// end of file
//==============================================================================
// Project:     Trading Simulator
// Name:        OptimizerParamAttribute
// Description: optimizer parameter
// History:     2018x09, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
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
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

namespace FUB_TradingSim
{
    /// <summary>
    /// attribute class to set optimzation range of field or property
    /// </summary>
    public class OptimizerParamAttribute : Attribute
    {
        public bool Enabled { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public int Increment { get; set; }

        public OptimizerParamAttribute(int start, int end, int increment)
        {
            Enabled = false;
            Start = start;
            End = end;
            Increment = increment;
        }
    }
}

//==============================================================================
// end of file
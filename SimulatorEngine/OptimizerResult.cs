//==============================================================================
// Project:     Trading Simulator
// Name:        OptimizerResult
// Description: optimizer result
// History:     2018x09, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region Libaries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Container to store parameters and fitness of a single optimiation
    /// iteration.
    /// </summary>
    public class OptimizerResult
    {
        /// <summary>
        /// Parameter values for this iteration.
        /// </summary>
        public Dictionary<string, int> Parameters = new Dictionary<string, int>();

        /// <summary>
        /// Parameter values for this iteration, as string.
        /// </summary>
        public string ParametersAsString
        {
            get
            {
                string retval = "";
                foreach (var parameter in Parameters.OrderBy(p => p.Key))
                    retval += string.Format("{0}={1} ", parameter.Key, parameter.Value);
                return retval;
            }
        }

        /// <summary>
        /// Net asset value at end of this iteration.
        /// </summary>
        public double? NetAssetValue { get; set; }

        /// <summary>
        /// Maximum drawdown over the course of this iteration.
        /// </summary>
        public double? MaxDrawdown { get; set; }

        /// <summary>
        /// Fitness value at end of this iteration.
        /// </summary>
        public double? Fitness { get; set; }
    }
}

//==============================================================================
// end of file
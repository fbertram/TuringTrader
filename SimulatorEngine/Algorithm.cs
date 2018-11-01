//==============================================================================
// Project:     Trading Simulator
// Name:        Algorithm
// Description: Base class for trading algorithms
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
#endregion

namespace FUB_TradingSim
{
    /// <summary>
    /// base class for trading algorithms
    /// </summary>
    public abstract class Algorithm : SimulatorCore
    {
        #region public Algorithm()
        public Algorithm()
        {
            // create a dictionary of optimizer parameters
            OptimizerParams = new Dictionary<string, OptimizerParam>();
            foreach (OptimizerParam param in OptimizerParam.GetParams(this))
                OptimizerParams[param.Name] = param;

            GlobalSettings.MostRecentAlgorithm = Name;
        }
        #endregion

        override public void Run() { }
        override public void Report() { }

        public /*readonly*/ Dictionary<string, OptimizerParam> OptimizerParams;
        #region public string OptimizerParamsAsString
        public string OptimizerParamsAsString
        {
            get
            {
                string retval = "";
                foreach (var parameter in OptimizerParams.Values.OrderBy(p => p.Name))
                {
                    retval += retval.Length > 0 ? ", " : "";
                    retval += string.Format("{0}={1}", parameter.Name, parameter.Value);
                }
                return retval;
            }
        }
        #endregion
        public bool IsOptimizing = false;
        #region public double FitnessValue
        public double FitnessValue
        {
            get;
            protected set;
        }
        #endregion
    }
}
//==============================================================================
// end of file
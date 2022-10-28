//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        AlgorithmApi
// Description: Interface between the TuringTrader application and algorithms.
// History:     2021iv23, FUB, created
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

#if false

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace TuringTrader.Simulator.v2
{
    /// <summary>
    /// Dummy implementation of external interface to trading algorithms.
    /// </summary>
    public abstract class AlgorithmApi : IAlgorithm
    {
        public virtual string Name => this.GetType().Name;

        //----- instantiation
        #region public Algorithm()
        /// <summary>
        /// Initialize trading algorithm. Most trading algorithms will
        /// only do very little here; the majority of the initialization
        /// should be performed in Run(), to allow multiple runs of
        /// the same instance.
        /// </summary>
        protected AlgorithmApi()
        {
            // create a dictionary of optimizer parameters
            OptimizerParams = new Dictionary<string, OptimizerParam>();
            foreach (OptimizerParam param in OptimizerParam.GetParams(this))
                OptimizerParams[param.Name] = param;
        }
        #endregion
        #region public Algorithm Clone()
        /// <summary>
        /// Clone algorithm, including all optimizer parameters. The application uses
        /// this method to clone the 'master' instance, and create new algorithm 
        /// instances before running them.
        /// </summary>
        /// <returns>new algorithm instance</returns>
        public IAlgorithm Clone()
        {
            Type algoType = GetType();
            AlgorithmApi clonedInstance = (AlgorithmApi)Activator.CreateInstance(algoType);

            // apply optimizer values to new instance
            foreach (OptimizerParam parameter in OptimizerParams.Values)
            {
                clonedInstance.OptimizerParams[parameter.Name].IsEnabled = parameter.IsEnabled;
                clonedInstance.OptimizerParams[parameter.Name].Start = parameter.Start;
                clonedInstance.OptimizerParams[parameter.Name].End = parameter.End;
                clonedInstance.OptimizerParams[parameter.Name].Step = parameter.Step;
                clonedInstance.OptimizerParams[parameter.Name].Value = parameter.Value;
            }

            return clonedInstance;
        }
        #endregion

        //----- running & reporting
        public virtual void Run() { }
        public virtual void Report() { }
        public virtual double Progress => 0.0;

        //----- optimization
        public Dictionary<string, OptimizerParam> OptimizerParams { get; private set; }
        #region public string OptimizerParamsAsString
        /// <summary>
        /// String representation of the current settings of all
        /// optimizable parameters.
        /// </summary>
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
        public virtual bool IsOptimizerParamsValid => true;
        public bool IsOptimizing { get; set; }
        public virtual double FitnessReturn => 0.0;
        public virtual double FitnessRisk => 0.0;
        public virtual double FitnessValue => 0.0;
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#endif

//==============================================================================
// end of file

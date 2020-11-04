//==============================================================================
// Project:     Trading Simulator
// Name:        Algorithm
// Description: Base class for trading algorithms
// History:     2018ix10, FUB, created
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

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Abstract base class for trading algorithms. All Turing Trader algorithms
    /// must be derived from this class, and override the Run() and Report()
    /// methods.
    /// </summary>
    public abstract class Algorithm : SimulatorCore
    {
        #region public Algorithm()
        /// <summary>
        /// Initialize trading algorithm. Most trading algorithms will
        /// only do very little here; the majority of the initialization
        /// should be performed in Run(), to allow multiple runs of
        /// the same instance.
        /// </summary>
        protected Algorithm()
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
        public Algorithm Clone()
        {
            Type algoType = GetType();
            Algorithm clonedInstance = (Algorithm)Activator.CreateInstance(algoType);

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

        #region public bool CanRunAsChild
        /// <summary>
        /// True, if this algorithm can be run as a child algorithm.
        /// </summary>
        public virtual bool CanRunAsChild => false;
        #endregion
        #region public bool IsDataSource
        /// <summary>
        /// Field indicating if this algorithm instance is used as
        /// a datasource. This information can be used to turn-off
        /// CPU or memory intensive operations, e.g., the generation
        /// of plots and logs.
        /// </summary>
        public bool IsDataSource { get; set; } = false;
        #endregion

        #region public virtual void Run()
        /// <summary>
        /// Entry point for trading algorithm, simple interface.
        /// All algorithms override either this method, or the subclassable
        /// version of it with their own implementation. Algorithms are not
        /// required to call the base class implementation of this method. 
        /// This method is called only once per instance. Nonetheless,
        /// care should be taken that the implementation of this method 
        /// initializes/ resets all parameters, to allow multiple runs.
        /// </summary>
        public virtual void Run() 
        {
            // Unnecessary assignment of a value to 'noLazyExec'
            #pragma warning disable IDE0059
                var noLazyExec = Run(null, null)
                    .ToList();
            #pragma warning restore IDE0059
        }
        #endregion
        #region public virtual IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        /// <summary>
        /// Entry point for trading algorithm, subclassable interface.
        /// All algorithms override either this method, or the subclassable
        /// version of it with their own implementation. Algorithms are not
        /// required to call the base class implementation of this method. 
        /// This method is called only once per instance. Nonetheless,
        /// care should be taken that the implementation of this method 
        /// initializes/ resets all parameters, to allow multiple runs.
        /// </summary>
        /// <param name="startTime">simulation start time</param>
        /// <param name="endTime">simulation end time</param>
        /// <returns>enumerable of bars, representing the algorithm result</returns>
        public virtual IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
            Run();

            yield break;
        }
        #endregion
        #region public virtual void Report()
        /// <summary>
        /// Create report. This method can be called after calling Run(), to
        /// create and display a custom report. Typically, trading algorithms
        /// override this method with their own implementation. Algorithms are
        /// not required to call the base class implementation of this method.
        /// </summary>
        public virtual void Report() { }
        #endregion
        #region public double Progress
        /// <summary>
        /// Progress indicator, ranging from 0 to 100. This progress indicator
        /// is based on the time stamp of the current bar being processed, which
        /// may or may not be a good indicator of CPU time.
        /// </summary>
        public double Progress
        {
            get
            {
                try
                {
                    double doneDays = (SimTime[0] - (DateTime)WarmupStartTime).TotalDays;
                    double totalDays = ((DateTime)EndTime - (DateTime)WarmupStartTime).TotalDays;
                    return 100.0 * doneDays / totalDays;
                }
// CA1031: Modify get_Progress to catch a more specific exception type, or rethrow the exception
#pragma warning disable CA1031
                catch (Exception)
                {
                    return 0.0;
                }
#pragma warning restore CA1031
            }
        }
        #endregion

        #region public Dictionary<string, OptimizerParam> OptimizerParams
        /// <summary>
        /// Container holding all optimizable parameters, along with
        /// their settings.
        /// </summary>
        public /*readonly*/ Dictionary<string, OptimizerParam> OptimizerParams;
        #endregion
        #region public virtual bool CheckParametersValid()
        /// <summary>
        /// Check, if current parameterset is valid. This is used to weed out
        /// illegal parameter combinations during grid optimization.
        /// </summary>
        /// <returns>true, if parameter set valid</returns>
        public virtual bool CheckParametersValid()
        {
            return true;
        }
        #endregion
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
        #region public bool IsOptimizing
        /// <summary>
        /// Field indicating if this algorithm instance is
        /// being optimized. Algorithms should use this to reduce
        /// unnecessary calculations and output, to speed up optimization
        /// and conserve memory.
        /// </summary>
        public bool IsOptimizing = false;
        #endregion
        #region public double FitnessValue
        /// <summary>
        /// Custom fitness value. Algorithms should use this, to report
        /// the fitness of the current algorithm settings to the optimizer.
        /// Outside of optimization, this field has no relevance.
        /// </summary>
        public double FitnessValue
        {
            get;
            protected set;
        }
        #endregion

        #region public string SubclassedParam
        /// <summary>
        /// Field w/ optional additional parameters for subclassed parameters. 
        /// This field is populated from the data source's nickname used to
        /// instantiate the algorithm.
        /// </summary>
        public string SubclassedParam = null;
        #endregion
    }
}
//==============================================================================
// end of file
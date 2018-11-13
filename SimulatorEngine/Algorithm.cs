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
        public Algorithm()
        {
            // create a dictionary of optimizer parameters
            OptimizerParams = new Dictionary<string, OptimizerParam>();
            foreach (OptimizerParam param in OptimizerParam.GetParams(this))
                OptimizerParams[param.Name] = param;

            GlobalSettings.MostRecentAlgorithm = Name;
        }
        #endregion

        #region override public void Run()
        /// <summary>
        /// Main entry point for trading algorithms, containing all proprietary
        /// trading logic. All trading algorithms override this method with their
        /// own implementation. Trading algorithms are not required to call the
        /// base class implementation of this method. This method may be called
        /// multiple times for the same instance. Care should be taken, that the
        /// implementation of this method initializes all parameters, to allow
        /// multiple runs.
        /// </summary>
        override public void Run() { }
        #endregion
        #region virtual public void Report()
        /// <summary>
        /// Create report. This method can be called after calling Run(), to
        /// create and display a custom report. Typically, trading algorithms
        /// override this method with their own implementation. Algorithms are
        /// not required to call the base class implementation of this method.
        /// </summary>
        virtual public void Report() { }
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
                    double totalDays = (EndTime - (DateTime)WarmupStartTime).TotalDays;
                    return 100.0 * doneDays / totalDays;
                }
                catch (Exception)
                {
                    return 0.0;
                }
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
    }
}
//==============================================================================
// end of file
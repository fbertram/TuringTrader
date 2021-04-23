//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        IAlgorithm
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

#region libraries
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Application interface to trading algorithms.
    /// </summary>
    public interface IAlgorithm
    {
        /// <summary>
        /// Return class type name. This method will return the name of the
        /// derived class, typically a proprietary algorithm derived from
        /// Algorithm.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Clone algorithm, including all optimizer parameters. The application uses
        /// this method to clone the 'master' instance, and create new algorithm 
        /// instances before running them.
        /// </summary>
        /// <returns>new algorithm instance</returns>
        public IAlgorithm Clone();

        /// <summary>
        /// Entry point for trading algorithm. This method is called only once 
        /// per instance. Nonetheless, care should be taken that the 
        /// implementation of this method initializes/ resets all parameters, 
        /// to allow multiple runs.
        /// </summary>
        public void Run();

        /// <summary>
        /// Create report. This method can be called after calling Run(), to
        /// create and display a custom report. Typically, trading algorithms
        /// override this method with their own implementation. Algorithms are
        /// not required to call the base class implementation of this method.
        /// </summary>
        public void Report();

        /// <summary>
        /// Progress indicator, ranging from 0 to 100. This progress indicator
        /// is based on the time stamp of the current bar being processed, which
        /// may or may not be a good indicator of CPU time.
        /// </summary>
        public double Progress { get; }

        /// <summary>
        /// Container holding all optimizable parameters, along with
        /// their settings.
        /// </summary>
        public Dictionary<string, OptimizerParam> OptimizerParams { get; }

        /// <summary>
        /// Check, if current parameter set is valid. This is used to weed out
        /// illegal parameter combinations during grid optimization.
        /// </summary>
        /// <returns>true, if parameter set valid</returns>
        public bool IsOptimizerParamsValid { get; }

        /// <summary>
        /// String representation of the current settings of all
        /// optimizable parameters.
        /// </summary>
        public string OptimizerParamsAsString { get; }

        /// <summary>
        /// Field indicating if this algorithm instance is
        /// being optimized. Algorithms should use this to reduce
        /// unnecessary calculations and output, to speed up optimization
        /// and conserve memory.
        /// </summary>
        public bool IsOptimizing { set; }

        /// <summary>
        /// Custom fitness value representing the return-aspect of the algorithm.
        /// </summary>
        public double FitnessReturn { get; }

        /// <summary>
        /// Custom fitness value representing the risk-aspect of the algorithm.
        /// </summary>
        public double FitnessRisk { get; }
        /// <summary>
        /// Custom fitness value representing a blend of return and risk.
        /// Algorithms should use this, to report the fitness of the 
        /// current algorithm settings to the optimizer.
        /// Outside of optimization, this field has no relevance.
        /// </summary>
        public double FitnessValue { get; }
    }
}

//==============================================================================
// end of file

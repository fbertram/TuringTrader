//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        OptimizerGrid
// Description: exhaustive parameter optimizer
// History:     2018ix20, FUB, created
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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{

    /// <summary>
    /// Exhaustive grid optimizer. This optimizer iterate through all possible 
    /// parameter combinations in a brute-force fashion. Individual iterations
    /// are executed in parallel, as far as possible.
    /// </summary>
    public class OptimizerGrid
    {
        #region internal data
        private MTJobQueue _jobQueue = new MTJobQueue();
        private readonly object _optimizerLock = new object();
        private int _numIterationsTotal;
        private int _numIterationsCompleted;
        private double? _maxFitness = null;
        private DateTime _startTime;
        #endregion
        #region internal helpers
        #region private void RunIteration()
        private void RunIteration()
        {
            if (!MasterInstance.CheckParametersValid())
            {
                // parameters are invalid. skip this iteration
                _numIterationsTotal--;
                return;
            }

            // create algorithm instance to run
            Algorithm instanceToRun = MasterInstance.Clone();

            // mark this as an optimizer run
            instanceToRun.IsOptimizing = true;

            // create result entry
            OptimizerResult result = new OptimizerResult();
            foreach (OptimizerParam parameter in MasterInstance.OptimizerParams.Values)
                result.Parameters[parameter.Name] = parameter.Value;
            result.Fitness = null;
            Results.Add(result);

            // run algorithm with these values
            _jobQueue.QueueJob(() =>
            {
                try
                {
                    instanceToRun.Run();

                    result.NetAssetValue = instanceToRun.NetAssetValue[0];
                    result.MaxDrawdown = instanceToRun.NetAssetValueMaxDrawdown;
                    result.Fitness = instanceToRun.FitnessValue;
                    instanceToRun = null;
                }

                catch (Exception)
                {
                    // we ignore any exeption while running the algo
                }

                finally
                {
                    lock (_optimizerLock)
                    {
                        _numIterationsCompleted++;

                        TimeSpan t = DateTime.Now - _startTime;
                        TimeSpan eta = TimeSpan.FromSeconds(
                            (_numIterationsTotal - _numIterationsCompleted)
                            * t.TotalSeconds / _numIterationsCompleted);

                        if (result.Fitness != null)
                            _maxFitness = _maxFitness != null
                                ? Math.Max((double)_maxFitness, (double)result.Fitness)
                                : (double)result.Fitness;

                        Output.WriteLine("GridOptimizer: {0} of {1} iterations completed, max fitness = {2:F4}, eta = {3}h{4}m{5}s",
                            _numIterationsCompleted, _numIterationsTotal, _maxFitness,
                            Math.Floor(eta.TotalHours), eta.Minutes, eta.Seconds);
                    }
                }
            });
        }
        #endregion
        #region private void IterateLevel(int level)
        private void IterateLevel(int level)
        {
            OptimizerParam parameter = MasterInstance.OptimizerParams.Values
                    .Skip(level)
                    .FirstOrDefault();

            if (parameter != default(OptimizerParam))
            {
                if (parameter.IsEnabled)
                {
                    for (int value = parameter.Start; value <= parameter.End; value += parameter.Step)
                    {
                        parameter.Value = value;
                        IterateLevel(level + 1);
                    }
                }
                else
                {
                    IterateLevel(level + 1);
                }
            }
            else
            {
                RunIteration();
            }
        }
        #endregion
        #endregion

        #region public static int NumIterations(Algorithm algo)
        /// <summary>
        /// Number of optimizer iterations
        /// </summary>
        /// <param name="algo">algorithm to optimize</param>
        /// <returns># of iterations</returns>
        public static int NumIterations(Algorithm algo)
        {
            // figure out total number of iterations
            int numIterationsTotal = 1;
            foreach (OptimizerParam parameter in algo.OptimizerParams.Values)
            {
                int iterationsThisLevel = 0;
                if (parameter.IsEnabled)
                {
                    for (int i = parameter.Start; i <= parameter.End; i += parameter.Step)
                        iterationsThisLevel++;
                }
                else
                {
                    iterationsThisLevel = 1;
                }

                numIterationsTotal *= iterationsThisLevel;
            }

            return numIterationsTotal;
        }
        #endregion

        #region public OptimizerGrid(Algorithm masterInstance)
        /// <summary>
        /// Crearte and initialize new grid optimizer instance.
        /// </summary>
        /// <param name="masterInstance">algorithm to optimize</param>
        public OptimizerGrid(Algorithm masterInstance)
        {
            MasterInstance = masterInstance;
        }
        #endregion
        #region public Algorithm MasterInstance
        /// <summary>
        /// Master instance of algorithm to optimize.
        /// </summary>
        public Algorithm MasterInstance = null;
        #endregion

        #region public void Run()
        /// <summary>
        /// Run optimization.
        /// </summary>
        public void Run()
        {
            _startTime = DateTime.Now;

            // create new results list
            Results = new List<OptimizerResult>();

            // figure out total number of iterations
            _numIterationsCompleted = 0;
            _numIterationsTotal = NumIterations(MasterInstance);
            Output.WriteLine("GridOptimizer: total of {0} iterations", _numIterationsTotal);

            // create and queue iterations
            IterateLevel(0);

            // wait for completion
            _jobQueue.WaitForCompletion();

            TimeSpan t = DateTime.Now - _startTime;
            Output.WriteLine("GridOptimizer: finished after {0}h{1}m{2}s @ {3} iterations/hour",
                Math.Floor(t.TotalHours), t.Minutes, t.Seconds,
                Math.Round(_numIterationsTotal / t.TotalHours));
        }
        #endregion
        #region public double Progress
        /// <summary>
        /// Progress of optimization as a double between 0 and 100.
        /// </summary>
        public double Progress
        {
            get
            {
                if (_numIterationsTotal > 0)
                    return 100.0 * _numIterationsCompleted / _numIterationsTotal;
                else
                    return 0.0;
            }
        }
        #endregion

        #region List<OptimizerResult> Results
        /// <summary>
        /// Return list of optimization results.
        /// </summary>
        public List<OptimizerResult> Results;
        #endregion
        #region public void ResultsToExcel(string excelPath)
        /// <summary>
        /// Export optimizer results to Excel.
        /// </summary>
        /// <param name="excelPath"></param>
        public void ResultsToExcel(string excelPath)
        {
            Plotter logger = new Plotter();

            logger.SelectChart("Optimizer Results", "iteration");

            for (int i = 0; i < Results.Count; i++)
            {
                OptimizerResult result = Results[i];

                logger.SetX(i);
                logger.Plot("NetAssetValue", (result.NetAssetValue != null) ? string.Format("{0}", result.NetAssetValue) : "");
                logger.Plot("Fitness", (result.Fitness != null) ? string.Format("{0}", result.Fitness) : "");

                foreach (var parameter in result.Parameters)
                    logger.Plot(parameter.Key, parameter.Value);
            }

            logger.OpenWith(excelPath);
        }
        #endregion
        #region public void SetParametersFromResult(OptimizerResult result)
        /// <summary>
        /// Set master instance to parameters from optimzation result.
        /// </summary>
        /// <param name="result">optimization result</param>
        public void SetParametersFromResult(OptimizerResult result)
        {
            foreach (var parameter in result.Parameters)
                MasterInstance.OptimizerParams[parameter.Key].Value = parameter.Value;
        }
        #endregion
    }
}

//==============================================================================
// end of file
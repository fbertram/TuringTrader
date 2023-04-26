﻿//==============================================================================
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

// ENABLE_V2_DATA_SHARING: if defined: allow cloned instances to share data.
//                         otherwise, all data are private.
#define ENABLE_V2_DATA_SHARING

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader.Optimizer
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
        private DateTime _optimizationStart;
        private DateTime? _algoStart;
        private DateTime? _algoEnd;
        private bool _verbose;
        private SimulatorV2.Cache _v2DataCache;
        #endregion
        #region internal helpers
        #region private void RunIteration()
        private void RunIteration()
        {
            if (!MasterInstance.IsOptimizerParamsValid)
            {
                // parameters are invalid. skip this iteration
                _numIterationsTotal--;
                return;
            }

            // create algorithm instance to run
            IAlgorithm instanceToRun = MasterInstance.Clone();

            // mark this as an optimizer run
            instanceToRun.IsOptimizing = true;

#if ENABLE_V2_DATA_SHARING
            // use shared data cache (v2 algorithms only)
            var instanceV2 = (instanceToRun as SimulatorV2.Algorithm);
            if (instanceV2 != null)
            {
                instanceV2.DataCache = _v2DataCache;
            }
#endif

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
                    var v1Instance = instanceToRun as Algorithm;
                    var v2Instance = instanceToRun as SimulatorV2.Algorithm;

                    if (v1Instance != null)
                    {
                        // make sure to enter the extended Run method
                        // the default implementation will forward
                        // to the simple Run method, if required
                        // also, we need to convert the result to a list,
                        // in order to circumvent lazy execution
                        var noLazyExec = v1Instance.Run(_algoStart, _algoEnd)
                            .ToList();

                    }
                    if (v2Instance != null)
                    {
                        // launching v2 algorithms is less convoluted
                        v2Instance.StartDate = _algoStart;
                        v2Instance.EndDate = _algoEnd;
                        v2Instance.Run();
                    }

                    result.NetAssetValue = instanceToRun.FitnessReturn;
                    result.MaxDrawdown = instanceToRun.FitnessRisk;
                    result.Fitness = instanceToRun.FitnessValue;
                    instanceToRun = null;
                }

                catch (Exception e)
                {
                    // we ignore any exeption while running the algo
                    lock (_optimizerLock)
                    {
                        if (_verbose) Output.WriteLine("{0}: Iteration failed. {1}", this.GetType().Name, e.Message);
                    }
                }

                finally
                {
                    lock (_optimizerLock)
                    {
                        _numIterationsCompleted++;

                        TimeSpan t = DateTime.Now - _optimizationStart;
                        TimeSpan eta = TimeSpan.FromSeconds(
                            (_numIterationsTotal - _numIterationsCompleted)
                            * t.TotalSeconds / _numIterationsCompleted);

                        if (result.Fitness != null && double.IsFinite((double)result.Fitness))
                            _maxFitness = _maxFitness != null
                                ? Math.Max((double)_maxFitness, (double)result.Fitness)
                                : (double)result.Fitness;

                        if (_verbose) Output.WriteLine("GridOptimizer: {0} of {1} iterations completed, max fitness = {2:F4}, eta = {3}h{4}m{5}s",
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
        public static int NumIterations(IAlgorithm algo)
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
        /// <param name="verbose">show progress information</param>
        public OptimizerGrid(IAlgorithm masterInstance, bool verbose = true)
        {
            MasterInstance = masterInstance;
            _verbose = verbose;
        }
        #endregion
        #region public Algorithm MasterInstance
        /// <summary>
        /// Master instance of algorithm to optimize.
        /// </summary>
        public IAlgorithm MasterInstance = null;
        #endregion

        #region public void Run()
        /// <summary>
        /// Run optimization.
        /// </summary>
        /// <param name="startTime">optimization start time</param>
        /// <param name="endTime">optimization end time</param>
        public void Run(DateTime? startTime = null, DateTime? endTime = null)
        {
            _algoStart = startTime;
            _algoEnd = endTime;

            _optimizationStart = DateTime.Now;

            // create new results list
            Results = new List<OptimizerResult>();

            // figure out total number of iterations
            _numIterationsCompleted = 0;
            _numIterationsTotal = NumIterations(MasterInstance);
            if (_verbose) Output.WriteLine("GridOptimizer: total of {0} iterations", _numIterationsTotal);

            // create and queue iterations
            _v2DataCache = new SimulatorV2.Cache();
            IterateLevel(0);

            // wait for completion
            _jobQueue.WaitForCompletion();

            TimeSpan t = DateTime.Now - _optimizationStart;
            if (_verbose) Output.WriteLine("GridOptimizer: finished after {0}h{1}m{2}s @ {3} iterations/hour",
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
//==============================================================================
// Project:     Trading Simulator
// Name:        OptimizerGrid
// Description: exhaustive parameter optimizer
// History:     2018ix20, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
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

namespace FUB_TradingSim
{
    #region public class OptimizerParamAttribute
    /// <summary>
    /// attribute class to set optimzation range of field or property
    /// </summary>
    public class OptimizerParamAttribute : Attribute
    {
        public readonly int Start;
        public readonly int End;
        public readonly int Increment;

        public OptimizerParamAttribute(int start, int end, int increment)
        {
            Start = start;
            End = end;
            Increment = increment;
        }
    }
    #endregion

    #region public class OptimizerResult
    /// <summary>
    /// container to store parameters and fitness of optimiation iteration
    /// </summary>
    public class OptimizerResult
    {
        public Dictionary<string, int> Parameters = new Dictionary<string, int>();
        public double? NetAssetValue;
        public double? Fitness;
    }
    #endregion

    #region public static class OptimizerSupport
    public static class OptimizerSupport
    {
        public static void FindValues(this Algorithm algo, Dictionary<string, int> dict)
        {
            Type algoType = algo.GetType();

            IEnumerable<PropertyInfo> properties = algoType.GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(OptimizerParamAttribute)));

            foreach (PropertyInfo property in properties)
                dict[property.Name] = (int)property.GetValue(algo);

            IEnumerable<FieldInfo> fields = algoType.GetFields()
                .Where(p => Attribute.IsDefined(p, typeof(OptimizerParamAttribute)));

            foreach (FieldInfo field in fields)
                dict[field.Name] = (int)field.GetValue(algo);
        }

        public static OptimizerParamAttribute GetAttribute(this Algorithm algo, string name)
        {
            Type algoType = algo.GetType();

            PropertyInfo property = algoType.GetProperties()
                .Where(p => p.Name == name)
                .FirstOrDefault();

            if (property != null)
                return (OptimizerParamAttribute)property.GetCustomAttribute(typeof(OptimizerParamAttribute));

            FieldInfo field = algoType.GetFields()
                .Where(f => f.Name == name)
                .FirstOrDefault();

            if (field != null)
                return (OptimizerParamAttribute)field.GetCustomAttribute(typeof(OptimizerParamAttribute));

            throw new Exception(string.Format("GetAttribute: parameter {0} not found", name));
        }

        public static void SetValue(this Algorithm algo, string name, int value)
        {
            Type algoType = algo.GetType();

            PropertyInfo property = algoType.GetProperties()
                .Where(p => p.Name == name)
                .FirstOrDefault();

            if (property != null)
                property.SetValue(algo, value);

            FieldInfo field = algoType.GetFields()
                .Where(f => f.Name == name)
                .FirstOrDefault();

            if (field != null)
                field.SetValue(algo, value);

            if (property == null && field == null)
                throw new Exception(string.Format("SetValue: parameter {0} not found", name));
        }

        public static int GetValue(this Algorithm algo, string name)
        {
            Type algoType = algo.GetType();

            PropertyInfo property = algoType.GetProperties()
                .Where(p => p.Name == name)
                .FirstOrDefault();

            if (property != null)
                return (int)property.GetValue(algo);

            FieldInfo field = algoType.GetFields()
                .Where(f => f.Name == name)
                .FirstOrDefault();

            if (field != null)
                return (int)field.GetValue(algo);

            throw new Exception(string.Format("GetValue: parameter {0} not found", name));
        }
    }
    #endregion

    /// <summary>
    /// class to run exhaustive optimization
    /// </summary>
    public class OptimizerGrid
    {
        #region internal data
        private Algorithm _algorithm;
        private Dictionary<string, int> _parameters;
        private MTJobQueue _jobQueue = new MTJobQueue();
        private int _numIterationsTotal;
        private int _numIterationsCompleted;
        #endregion

        #region private void RunIteration(bool firstRun = true)
        private Algorithm RunIteration(bool firstRun = true)
        {
            // create algorithm instance to run
            Type algoType = _algorithm.GetType();
            Algorithm instanceToRun = (Algorithm)Activator.CreateInstance(algoType);

            // apply optimizer values to new instance
            foreach (var parameter in _parameters)
                instanceToRun.SetValue(parameter.Key, parameter.Value);

            if (firstRun)
            {
                // mark this as an optimizer run
                instanceToRun.IsOptimizing = true;

                // create result entry
                OptimizerResult result = new OptimizerResult();
                foreach (var parameter in _parameters)
                    result.Parameters[parameter.Key] = parameter.Value;
                result.Fitness = null;
                Results.Add(result);

                // run algorithm with these values
                _jobQueue.QueueJob(() =>
                {
                    instanceToRun.Run();
                    result.NetAssetValue = instanceToRun.NetAssetValue[0];
                    result.Fitness = instanceToRun.FitnessValue;
                    instanceToRun = null;
                    _numIterationsCompleted++;
                    Debug.WriteLine("{0} of {1} optimizer iterations completed",
                        _numIterationsCompleted, _numIterationsTotal);
                });
            }
            else
            {
                // this is for re-runs
                instanceToRun.Run();
            }

            return instanceToRun;
        }
        #endregion
        #region private void IterateLevel(int level)
        private void IterateLevel(int level)
        {
            string name = _parameters
                    .Select(p => p.Key)
                    .Skip(level)
                    .FirstOrDefault();

            if (name != default(string))
            {
                OptimizerParamAttribute param = _algorithm.GetAttribute(name);

                for (int value = param.Start; value <= param.End; value += param.Increment)
                {
                    _parameters[name] = value;
                    IterateLevel(level + 1);
                }
            }
            else
            {
                RunIteration();
            }
        }
        #endregion

        #region public OptimizerExhaustive(Algorithm algorithm)
        public OptimizerGrid(Algorithm algorithm)
        {
            _algorithm = algorithm;
        }
        #endregion
        #region public void Run()
        public void Run()
        {
            // create new results list
            Results = new List<OptimizerResult>();

            // gather info about algorithm to optimize
            _parameters = new Dictionary<string, int>();
            _algorithm.FindValues(_parameters);

            // figure out total number of iterations
            _numIterationsCompleted = 0;
            _numIterationsTotal = 1;
            foreach (string param in _parameters.Keys)
            {
                OptimizerParamAttribute paramAttribute = _algorithm.GetAttribute(param);

                int iterationsThisLevel = 0;
                for (int i = paramAttribute.Start; i <= paramAttribute.End; i += paramAttribute.Increment)
                    iterationsThisLevel++;

                _numIterationsTotal *= iterationsThisLevel;
            }

            // create and queue iterations
            IterateLevel(0);

            // wait for completion
            _jobQueue.WaitForCompletion();
        }
        #endregion

        public List<OptimizerResult> Results;
        #region public void ResultsToExcel(string excelPath)
        public void ResultsToExcel(string excelPath)
        {
            Logger logger = new Logger();

            logger.SelectPlot("Optimizer Results", "iteration");

            for (int i = 0; i < Results.Count; i++)
            {
                OptimizerResult result = Results[i];

                logger.SetX(i);
                logger.Log("NetAssetValue", (result.NetAssetValue != null) ? string.Format("{0}", result.NetAssetValue) : "");
                logger.Log("Fitness", (result.Fitness != null) ? string.Format("{0}", result.Fitness) : "");

                foreach (var parameter in result.Parameters)
                    logger.Log(parameter.Key, parameter.Value);
            }

            logger.OpenWithExcel(excelPath);
        }
        #endregion
        #region public Algorithm ReRun(OptimizerResult result)
        public Algorithm ReRun(OptimizerResult result)
        {
            foreach (var parameter in result.Parameters)
                _parameters[parameter.Key] = parameter.Value;

            return RunIteration(false);
        }
        #endregion
    }
}

//==============================================================================
// end of file
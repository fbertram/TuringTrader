//==============================================================================
// Project:     Trading Simulator
// Name:        OptimizerExhaustive
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
        public double? Fitness;
    }
    #endregion

    /// <summary>
    /// class to run exhaustive optimization
    /// </summary>
    public class OptimizerExhaustive
    {
        #region internal data
        private Algorithm _algorithm;
        private Type _algoType;
        private List<PropertyInfo> _properties;
        private List<FieldInfo> _fields;
        private Dictionary<int, string> _names = new Dictionary<int, string>();
        private Dictionary<int, int> _values = new Dictionary<int, int>();
        private MultiThreadedJobQueue _jobQueue = new MultiThreadedJobQueue();
        #endregion

        #region private void SetPropertyValue(Algorithm instance, PropertyInfo property, double value)
        private void SetPropertyValue(Algorithm instance, PropertyInfo property, double value)
        {
            if (property.PropertyType == typeof(int))
            {
                property.SetValue(instance, (int)value);
            }
            else
            {
                throw new Exception(string.Format("optimizer property of type {0} not supported", property.PropertyType));
            }
        }
        #endregion
        #region private void SetFieldValue(Algorithm instance, FieldInfo field, double value)
        private void SetFieldValue(Algorithm instance, FieldInfo field, double value)
        {
            if (field.FieldType == typeof(int))
            {
                field.SetValue(instance, (int)value);
            }
            else
            {
                throw new Exception(string.Format("optimizer field of type {0} not supported", field.FieldType));
            }
        }
        #endregion
        #region private void RunIteration(bool firstRun = true)
        private Algorithm RunIteration(bool firstRun = true)
        {
            // create algorithm instance to run
            Algorithm instanceToRun = (Algorithm)Activator.CreateInstance(_algoType);

            // apply optimizer values
            for (int i = 0; i < _properties.Count + _fields.Count; i++)
            {
                if (i < _properties.Count)
                    SetPropertyValue(instanceToRun, _properties[i], _values[i]);
                else
                    SetFieldValue(instanceToRun, _fields[i - _properties.Count], _values[i]);
            }

            if (firstRun)
            {
                // create result entry
                OptimizerResult result = new OptimizerResult();
                for (int i = 0; i < _properties.Count + _fields.Count; i++)
                {
                    result.Parameters[_names[i]] = _values[i];
                }
                result.Fitness = null;
                Results.Add(result);

                // run algorithm with these values
                _jobQueue.QueueJob(() =>
                {
                    instanceToRun.Run();
                    result.Fitness = instanceToRun.FitnessValue;
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
            if (level < _properties.Count)
            {
                PropertyInfo property = _properties[level];
                OptimizerParamAttribute propertyParams =
                    (OptimizerParamAttribute)property.GetCustomAttribute(typeof(OptimizerParamAttribute));

                for (int i = propertyParams.Start; i <= propertyParams.End; i += propertyParams.Increment)
                {
                    _names[level] = property.Name;
                    _values[level] = i;
                    IterateLevel(level + 1);
                }
            }
            else if (level - _properties.Count < _fields.Count)
            {
                FieldInfo field = _fields[level - _properties.Count];
                OptimizerParamAttribute propertyParams =
                    (OptimizerParamAttribute)field.GetCustomAttribute(typeof(OptimizerParamAttribute));

                for (int i = propertyParams.Start; i <= propertyParams.End; i += propertyParams.Increment)
                {
                    _names[level] = field.Name;
                    _values[level] = i;
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
        public OptimizerExhaustive(Algorithm algorithm)
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
            _algoType = _algorithm.GetType();

            _properties = _algoType.GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(OptimizerParamAttribute)))
                .ToList();

            _fields = _algoType.GetFields()
                .Where(p => Attribute.IsDefined(p, typeof(OptimizerParamAttribute)))
                .ToList();

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
            {
                int index = _names
                    .Where(n => n.Value == parameter.Key)
                    .Select(n => n.Key)
                    .First();

                _values[index] = parameter.Value;
            }

            return RunIteration();
        }
        #endregion
    }
}

//==============================================================================
// end of file
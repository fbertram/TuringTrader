//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        OptimizerParam
// Description: optimizer parameter combining property/field w/ its attribute
// History:     2018x10, FUB, created
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

#region Libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Container holding optimizer parameter information, collected from
    /// the field/ property and the [OptimizerParam] attribute.
    /// </summary>
    public class OptimizerParam
    {
        #region internal data
        private readonly IAlgorithm _algorithm;
        private readonly OptimizerParamAttribute _attribute;
        #endregion

        #region public static IEnumerable<OptimizerParam> GetParams(Algorithm algo)
        /// <summary>
        /// Retrieve all optimizable parameters for algorithm.
        /// </summary>
        /// <param name="algo">input algorithm</param>
        /// <returns>optimizable parameters</returns>
        public static IEnumerable<OptimizerParam> GetParams(IAlgorithm algo)
        {
            Type algoType = algo.GetType();

            IEnumerable<PropertyInfo> properties = algoType.GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(OptimizerParamAttribute)));

            foreach (PropertyInfo property in properties)
                yield return new OptimizerParam(algo, property.Name);

            IEnumerable<FieldInfo> fields = algoType.GetFields()
                .Where(p => Attribute.IsDefined(p, typeof(OptimizerParamAttribute)));

            foreach (FieldInfo field in fields)
                yield return new OptimizerParam(algo, field.Name);

            yield break;
        }
        #endregion

        #region public OptimizerParam(Algorithm algorithm, string name)
        /// <summary>
        /// Construct and initialize new optimizer param for algorithm. This is for internal
        /// use by the optimizer and should not be called by user applications.
        /// </summary>
        /// <param name="algorithm">parent algorithm</param>
        /// <param name="name">name of parameter</param>
        public OptimizerParam(IAlgorithm algorithm, string name)
        {
            _algorithm = algorithm;
            Name = name;
            IsEnabled = false;

            Type algoType = _algorithm.GetType();

            PropertyInfo property = algoType.GetProperties()
                .Where(p => p.Name == name)
                .FirstOrDefault();

            if (property != null)
                _attribute = (OptimizerParamAttribute)property.GetCustomAttribute(typeof(OptimizerParamAttribute));

            FieldInfo field = algoType.GetFields()
                .Where(f => f.Name == name)
                .FirstOrDefault();

            if (_attribute == null && field != null)
                _attribute = (OptimizerParamAttribute)field.GetCustomAttribute(typeof(OptimizerParamAttribute));

            if (_attribute == null)
                throw new Exception(string.Format("OptimizerParam: parameter {0} not found", name));

            Start = _attribute.Start;
            End = _attribute.End;
            Step = _attribute.Step;
        }
        #endregion

        #region public string Name
        /// <summary>
        /// Name of optimizer parameter.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }
        #endregion
        #region public bool IsEnabled
        /// <summary>
        /// Flag indicating enabled status of optimizer parameter.
        /// </summary>
        public bool IsEnabled
        {
            get;
            set;
        }
        #endregion
        #region public int Value
        /// <summary>
        /// Current value of optimizer parameter.
        /// </summary>
        public int Value
        {
            get
            {
                Type algoType = _algorithm.GetType();

                PropertyInfo property = algoType.GetProperties()
                    .Where(p => p.Name == Name)
                    .FirstOrDefault();

                if (property != null)
                    return (int)property.GetValue(_algorithm);

                FieldInfo field = algoType.GetFields()
                    .Where(f => f.Name == Name)
                    .FirstOrDefault();

                if (field != null)
                    return (int)field.GetValue(_algorithm);

                throw new Exception(string.Format("OptimizerParam: parameter {0} not found", Name));
            }

            set
            {
                Type algoType = _algorithm.GetType();

                PropertyInfo property = algoType.GetProperties()
                    .Where(p => p.Name == Name)
                    .FirstOrDefault();

                if (property != null)
                    property.SetValue(_algorithm, value);

                FieldInfo field = algoType.GetFields()
                    .Where(f => f.Name == Name)
                    .FirstOrDefault();

                if (field != null)
                    field.SetValue(_algorithm, value);

                if (property == null && field == null)
                    throw new Exception(string.Format("OptimizerParam: parameter {0} not found", Name));
            }
        }
        #endregion
        #region public int Start
        /// <summary>
        /// Starting value of optimizer parameter.
        /// </summary>
        public int Start
        {
            get;
            set;
        }
        #endregion
        #region public int End
        /// <summary>
        /// Ending value of optimizer parameter.
        /// </summary>
        public int End
        {
            get;
            set;
        }
        #endregion
        #region public int Step
        /// <summary>
        /// Step size of optimizer parameter.
        /// </summary>
        public int Step
        {
            get;
            set;
        }
        #endregion
    }
}

//==============================================================================
// end of file
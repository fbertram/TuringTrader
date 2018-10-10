//==============================================================================
// Project:     Trading Simulator
// Name:        OptimizerParam
// Description: optimizer parameter combining property/field w/ its attribute
// History:     2018x10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region Libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    public class OptimizerParam
    {
        #region internal data
        private readonly Algorithm _algorithm;
        private readonly OptimizerParamAttribute _attribute;
        #endregion

        #region public static IEnumerable<OptimizerParam> GetParams(Algorithm algo)
        public static IEnumerable<OptimizerParam> GetParams(Algorithm algo)
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
        public OptimizerParam(Algorithm algorithm, string name)
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
        public string Name
        {
            get;
            private set;
        }
        #endregion
        #region public bool IsEnabled
        public bool IsEnabled
        {
            get;
            set;
        }
        #endregion
        #region public int Value
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
        public int Start
        {
            get;
            set;
        }
        #endregion
        #region public int End
        public int End
        {
            get;
            set;
        }
        #endregion
        #region public int Step
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
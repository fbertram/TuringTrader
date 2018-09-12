//==============================================================================
// Project:     Trading Simulator
// Name:        Bar
// Description: data structure for single instrument bar
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL v3.0
//==============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    public class Bar
    {
        public Bar(
            string symbol,
            DateTime timeStamp,
            Dictionary<DataSourceValue, double> values,
            Dictionary<DataSourceValue, string> strings)
        {
            Symbol = symbol;
            TimeStamp = timeStamp;
            Values = values;
            Strings = strings;
        }

        public readonly string Symbol;
        public readonly DateTime TimeStamp;
        public readonly Dictionary<DataSourceValue, double> Values;
        public readonly Dictionary<DataSourceValue, string> Strings;
    }
}
//==============================================================================
// end of file
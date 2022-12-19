//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Account
// Description: Account class.
// History:     2022x25, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
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

using System;

namespace TuringTrader.SimulatorV2
{
    /// <summary>
    /// Type of quote, currently double. By using this abstract type, 
    /// the v2 engine decouples its implementation from the underlying value type.
    /// In the future, we might consider changing this type to decimal.
    /// </summary>
    public struct FloatWrapper /* IComparable<Quote> */ /* IConvertible */ /* IFormattable */
    {
        // see here: https://stackoverflow.com/questions/38198739/c-sharp-creating-a-custom-double-type

        //----- internal value type and constructor to match
        private readonly double _value;

        public FloatWrapper(double value)
        {
            _value = value;
        }

        //----- implicit conversions to create Quote from standard types
        public static implicit operator FloatWrapper(int value) => new FloatWrapper(value);
        public static implicit operator FloatWrapper(long value) => new FloatWrapper(value);
        public static implicit operator FloatWrapper(float value) => new FloatWrapper(value);
        public static implicit operator FloatWrapper(double value) => new FloatWrapper(value);
        public static explicit operator FloatWrapper(decimal value) => new FloatWrapper((double)value);

        //----- explicit conversions to create standard types from Quote
        public static explicit operator int(FloatWrapper value) => (int)value._value;
        public static explicit operator long(FloatWrapper value) => (long)value._value;
        public static explicit operator float(FloatWrapper value) => (float)value._value;
        public static explicit operator double(FloatWrapper value) => (double)value._value;
        public static explicit operator decimal(FloatWrapper value) => (decimal)value._value;

        //----- arithmetic operators
        public static FloatWrapper operator +(FloatWrapper a) => a;
        public static FloatWrapper operator -(FloatWrapper a) => new FloatWrapper(-a._value);

        public static FloatWrapper operator +(FloatWrapper a, FloatWrapper b) => new FloatWrapper(a._value + b._value);
        public static FloatWrapper operator -(FloatWrapper a, FloatWrapper b) => new FloatWrapper(a._value - b._value);
        public static FloatWrapper operator *(FloatWrapper a, FloatWrapper b) => new FloatWrapper(a._value * b._value);
        public static FloatWrapper operator /(FloatWrapper a, FloatWrapper b) => b._value != 0 ? new FloatWrapper(a._value / b._value) : throw new DivideByZeroException();

        //----- relational operators
        public static bool operator ==(FloatWrapper a, FloatWrapper b) => a._value == b._value;
        public static bool operator !=(FloatWrapper a, FloatWrapper b) => a._value != b._value;
        public static bool operator >(FloatWrapper a, FloatWrapper b) => a._value > b._value;
        public static bool operator >=(FloatWrapper a, FloatWrapper b) => a._value >= b._value;
        public static bool operator <(FloatWrapper a, FloatWrapper b) => a._value < b._value;
        public static bool operator <=(FloatWrapper a, FloatWrapper b) => a._value <= b._value;
    }
}

//==============================================================================
// end of file

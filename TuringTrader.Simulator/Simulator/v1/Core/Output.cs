//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        Output
// Description: output methods
// History:     2018ix11, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
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
using System.Diagnostics;
using System.Linq;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Class providing formatted text output.
    /// </summary>
    public class Output
    {
        /// <summary>
        /// Debug output event.
        /// </summary>
        public static Action<string> WriteEvent;

        /// <summary>
        /// Write formatted debug output.
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">list or arguments</param>
        public static void Write(string format, params object[] args)
        {
            string message = args.Count() > 0
                ? string.Format(format, args)
                : format;

            if (WriteEvent == null)
                Debug.Write(message);
            else
                WriteEvent(message);
        }

        /// <summary>
        /// Write formatted debug output, and start new line.
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">list of arguments</param>
        public static void WriteLine(string format, params object[] args)
        {
            Write(format, args);
            Write(Environment.NewLine);
        }
    }
}

//==============================================================================
// end of file
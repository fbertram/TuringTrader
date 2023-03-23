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

using System;

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Class providing formatted text output.
    /// </summary>
    public static class Output
    {
        /// <summary>
        /// Debug output event. The application should attach an event
        /// handler here to redirect the messages to a console.
        /// </summary>
        public static Action<string> WriteEvent
        {
            get => SimulatorV2.Output.WriteEvent;
            set => SimulatorV2.Output.WriteEvent = value;
        }

        /// <summary>
        /// Enumeration of output modes.
        /// </summary>
        public enum DisplayModeType
        {
            /// <summary>
            /// show errors (and throw an exception), but suppress warnings 
            /// and informational messages.
            /// </summary>
            errorsOnly,
            /// <summary>
            /// show errors and first occurrence of warnings, but suppress 
            /// repeated warnings and all informational messages.
            /// </summary>
            errorsAndWarningsOnce,
            /// <summary>
            /// show errors and all warnings, but suppress informational messages.
            /// </summary>
            errorsAndWarnings,
            /// <summary>
            /// show all messages including errors, warnings, and info.
            /// </summary>
            errorsWarningsAndInfo,
        };

        /// <summary>
        /// Current output mode.
        /// </summary>
        public static DisplayModeType DisplayMode
            => (DisplayModeType)Enum.Parse(typeof(DisplayModeType), SimulatorV2.Output.DisplayMode.ToString());

        /// <summary>
        /// Display informational message. These messages will only be shown
        /// if DisplayMode is set to errorsWarningsAndInfo.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteInfo(string format, params object[] args)
            => SimulatorV2.Output.WriteInfo(format, args);

        /// <summary>
        /// Display warning message. These messages will only be shown
        /// if DisplayMode is set to errorsWarningsAndInfo, errorsAndWarnings,
        /// or errorsAndWarningsOnce. For errorsAndWarningsOnce, the message
        /// will only be shown the first time, repeated warnings with the
        /// same text will be suppressed.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteWarning(string format, params object[] args)
            => SimulatorV2.Output.WriteWarning(format, args);

        /// <summary>
        /// Display error message. Note that this method will also throw an exception.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <exception cref="Exception"></exception>
        public static void ThrowError(string format, params object[] args)
            => SimulatorV2.Output.ThrowError(format, args);

        /// <summary>
        /// Display output message. This is a legacy method, and will be removed
        /// from the API soon. Use Info, Warning, or Error instead.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Write(string format, params object[] args)
            => SimulatorV2.Output.Write(format, args);

        /// <summary>
        /// Display output message on a new line. This is a legacy method, 
        /// and will be removed from the API soon. Use Info, Warning, or 
        /// Error instead.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(string format, params object[] args)
            => SimulatorV2.Output.WriteLine(format, args);
    }
}

//==============================================================================
// end of file
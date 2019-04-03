//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        LogAnalysis
// Description: Analysis of simulation logs.
// History:     2019ii04, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Helper class to analyze order log.
    /// </summary>
    public class LogAnalysis
    {
        #region public class Position
        /// <summary>
        /// Container to hold historic position information
        /// </summary>
        public class Position
        {
            /// <summary>
            /// position symbol
            /// </summary>
            public string Symbol;

            /// <summary>
            /// position quantity
            /// </summary>
            public int Quantity;

            /// <summary>
            /// order log entry for position entry
            /// </summary>
            public LogEntry Entry;

            /// <summary>
            /// order log entry for position exit
            /// </summary>
            public LogEntry Exit;
        }
        #endregion

        #region public static List<Position> GroupPositions(List<LogEntry> log, bool lifo = true)
        /// <summary>
        /// Analyse log files and transform entries and exits into positions held.
        /// </summary>
        /// <param name="log">input log</param>
        /// <param name="lifo">grouping method. true (default) = last in/ first out. false = first in/ first out</param>
        /// <returns>container w/ positions</returns>
        public static List<Position> GroupPositions(List<LogEntry> log, bool lifo = true)
        {
            Dictionary<string, List<Position>> entries = new Dictionary<string, List<Position>>();
            List<Position> positions = new List<Position>();

            //----- walk through order log
            foreach (LogEntry order in log)
            {
                if (!entries.ContainsKey(order.Symbol))
                    entries[order.Symbol] = new List<Position>();

                int openQuantity = entries[order.Symbol]
                    .Sum(i => i.Quantity);
                int remainingQuantity = order.OrderTicket.Quantity;

                while (remainingQuantity != 0)
                {
                    //--- ignore
                    if (order.Action == LogEntryAction.Deposit
                    || order.Action == LogEntryAction.Withdrawal)
                        break;

                    //--- add new position
                    if (order.Action == LogEntryAction.Buy && openQuantity >= 0
                    || order.Action == LogEntryAction.Sell && openQuantity <= 0)
                    {
                        entries[order.Symbol].Add(new Position
                        {
                            Symbol = order.Symbol,
                            Quantity = order.OrderTicket.Quantity,
                            Entry = order,
                        });

                        remainingQuantity = 0;
                    }

                    //--- (partially) close existing position
                    if (order.Action == LogEntryAction.Sell && openQuantity > 0
                    || order.Action == LogEntryAction.Buy && openQuantity < 0
                    || order.Action == LogEntryAction.Expiry)
                    {
                        if (!entries.ContainsKey(order.Symbol)
                        || entries[order.Symbol].Count() == 0)
                        {
                            throw new Exception(
                                string.Format("LogAnalysis.GroupPositions: no matching entry found for symbol {0}", 
                                    order.Symbol));
                        }

                        Position entryOrder = lifo
                            ? entries[order.Symbol].Last()   // LIFO
                            : entries[order.Symbol].First(); // FIFO

                        // create a new position
                        int closeFromEntry = remainingQuantity < 0
                            ? -Math.Min(Math.Abs(remainingQuantity), entryOrder.Quantity) // close long
                            : Math.Min(remainingQuantity, Math.Abs(entryOrder.Quantity)); // close short

                        positions.Add(new Position
                        {
                            Symbol = order.Symbol,
                            Quantity = -closeFromEntry,
                            Entry = entryOrder.Entry,
                            Exit = order,
                        });

                        remainingQuantity -= closeFromEntry;

                        // adjust or remove entry
                        entryOrder.Quantity += closeFromEntry;
                        if (entryOrder.Quantity <= 0)
                            entries[order.Symbol].Remove(entryOrder);
                    }
                }
            }

            return positions;
        }
        #endregion
    }
}

//==============================================================================
// end of file
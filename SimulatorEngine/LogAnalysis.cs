//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        LogAnalysis
// Description: Analysis of simulation logs.
// History:     2019ii04, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2019, Bertram Solutions LLC
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

            foreach (LogEntry order in log)
            {
                switch (order.Action)
                {
                    case LogEntryAction.Buy:
                        if (!entries.ContainsKey(order.Symbol))
                            entries[order.Symbol] = new List<Position>();

                        entries[order.Symbol].Add(new Position
                        {
                            Symbol = order.Symbol,
                            Quantity = order.OrderTicket.Quantity,
                            Entry = order,
                        });
                        break;

                    case LogEntryAction.Sell:
                        int totalQuantity = -order.OrderTicket.Quantity;
                        while (totalQuantity > 0)
                        {
                            if (!entries.ContainsKey(order.Symbol)
                            || entries[order.Symbol].Count() == 0)
                                throw new Exception("LogAnalysis.GroupPositions: no entry found");

                            Position entryOrder = lifo
                                ? entries[order.Symbol].Last()  // LIFO
                                : entries[order.Symbol].First();// FIFO

                            int sellFromEntry = Math.Min(totalQuantity, entryOrder.Quantity);

                            positions.Add(new Position
                            {
                                Symbol = order.Symbol,
                                Quantity = sellFromEntry,
                                Entry = entryOrder.Entry,
                                Exit = order,
                            });

                            totalQuantity -= sellFromEntry;

                            entryOrder.Quantity -= sellFromEntry;
                            if (entryOrder.Quantity <= 0)
                                entries[order.Symbol].Remove(entryOrder);
                        }
                        break;
                }
            }

            return positions;
        }
        #endregion

#if false
        public static void Run(Algorithm algo, Action<ITimeSeries<DateTime>, ITimeSeries<double>> report)
        {
            _afterTaxSimulator afterTaxSim = new _afterTaxSimulator(algo, report);
            afterTaxSim.Run();
        }

        private class _afterTaxSimulator : SimulatorCore
        {
            private Algorithm _algo;
            private Action<ITimeSeries<DateTime>, ITimeSeries<double>> _report;


            public _afterTaxSimulator(Algorithm algo, Action<ITimeSeries<DateTime>, ITimeSeries<double>> report)
            {
                _algo = algo;
                _report = report;
            }

            override public void Run()
            {
                //----- re-run simulation

                // copy simulation setup from parent
                CloneSimSetup(_algo);

                List<LogEntry>.Enumerator logEnum = _algo.Log.GetEnumerator();

                bool hasTransactions = logEnum.MoveNext();
                if (!hasTransactions)
                    return;

                foreach (DateTime simTime in SimTimes)
                {
                    // QueueTime might be default(DateTime) for initial deposit
                    while (hasTransactions && logEnum.Current.OrderTicket.QueueTime <= SimTime[0])
                    {
                        LogEntry transaction = logEnum.Current;
                        Order order = transaction.OrderTicket;
                        order.Instrument = order.Type != OrderType.cash
                            ? Instruments.Where(i => i.Symbol == transaction.Symbol).First()
                            : null;

                        QueueOrder(order);

                        hasTransactions = logEnum.MoveNext();
                    }

                    _report(SimTime, NetAssetValue);
                }
            }
        }
#endif
    }
}

//==============================================================================
// end of file
//==============================================================================
// Project:     Trading Simulator
// Name:        LogAnalysis
// Description: Analysis of simulation logs.
// History:     2019ii04, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
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
    /// Class to simulate after-tax NAV for United States.
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
            /// position quantity
            /// </summary>
            public int Quantity;

            /// <summary>
            /// date of purchase
            /// </summary>
            public DateTime BuyDate;

            /// <summary>
            /// fill price of purchae
            /// </summary>
            public double BuyFill;

            /// <summary>
            /// commission paid for purchase
            /// </summary>
            public double BuyCommission;

            /// <summary>
            /// date of liquidation
            /// </summary>
            public DateTime SellDate;

            /// <summary>
            /// fill price of liquidation
            /// </summary>
            public double SellPrice;

            /// <summary>
            /// commission paid for liquidation
            /// </summary>
            public double SellCommission;
        }
        #endregion
        #region public static Dictionary<string, List<Position>> GroupPositions(List<LogEntry> log)
        /// <summary>
        /// Analyse log files and transform entries and exits into positions held.
        /// </summary>
        /// <param name="log">input log</param>
        /// <returns>container w/ positions</returns>
        public static Dictionary<string, List<Position>> GroupPositions(List<LogEntry> log)
        {
            Dictionary<string, List<Position>> buyTickets = new Dictionary<string, List<Position>>();
            Dictionary<string, List<Position>> holdTickets = new Dictionary<string, List<Position>>();

            foreach (LogEntry logEntry in log)
            {
                switch (logEntry.Action)
                {
                    case LogEntryAction.Buy:
                        if (!buyTickets.ContainsKey(logEntry.Symbol))
                            buyTickets[logEntry.Symbol] = new List<Position>();

                        buyTickets[logEntry.Symbol].Add(new Position
                        {
                            Quantity = logEntry.OrderTicket.Quantity,

                            BuyDate = logEntry.BarOfExecution.Time,
                            BuyFill = logEntry.FillPrice,
                            BuyCommission = logEntry.Commission,
                        });
                        break;

                    case LogEntryAction.Sell:
                        int totalQuantity = -logEntry.OrderTicket.Quantity;
                        while (totalQuantity > 0)
                        {
                            Position entry = false
                                ? buyTickets[logEntry.Symbol].First() // FIFO
                                : buyTickets[logEntry.Symbol].Last(); // LIFO

                            int sellFromEntry = Math.Min(totalQuantity, entry.Quantity);

                            if (!holdTickets.ContainsKey(logEntry.Symbol))
                                holdTickets[logEntry.Symbol] = new List<Position>();

                            holdTickets[logEntry.Symbol].Add(new Position
                            {
                                Quantity = sellFromEntry,

                                BuyDate = entry.BuyDate,
                                BuyFill = entry.BuyFill,
                                BuyCommission = entry.BuyCommission,

                                SellDate = logEntry.BarOfExecution.Time,
                                SellPrice = logEntry.FillPrice,
                                SellCommission = logEntry.Commission,
                            });

                            totalQuantity -= sellFromEntry;

                            entry.Quantity -= sellFromEntry;
                            if (entry.Quantity <= 0)
                                buyTickets[logEntry.Symbol].Remove(entry);
                        }
                        break;
                }
            }

            return holdTickets;
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
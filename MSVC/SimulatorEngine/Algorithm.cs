//==============================================================================
// Project:     Trading Simulator
// Name:        Algorithm
// Description: Base class for trading algorithms
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL v3.0
//==============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    public enum ReportType { FitnessValue, Plot, Excel };

    public abstract partial class Algorithm
    {
        //---------- internal helpers
        private void ExecOrder(Order ticket)
        {
            Instrument instrument = ticket.Instrument;
            Bar execBar = instrument[0];
            double price = execBar.Values[DataSourceValue.close];

            // add position
            if (!Positions.ContainsKey(instrument))
                Positions[instrument] = 0;
            Positions[instrument] += ticket.Quantity;
            if (Positions[instrument] == 0)
                Positions.Remove(instrument);

            // add log entry
            LogEntry log = new LogEntry()
            {
                OrderTicket = ticket,
                BarOfExecution = execBar,
                FillPrice = price,
                Commission = 0.00
            };
            Log.Add(log);

            // remove order from pending list
            PendingOrders = PendingOrders
                .Where(o => o != ticket)
                .ToList();
        }

        //---------- for use by trading applications
        public Algorithm()
        {
        }
        virtual public void Run()
        {

        }
        virtual public object Report(ReportType reportType)
        {
            return FitnessValue;
        }
        public double FitnessValue
        {
            get;
            protected set;
        }

        //---------- for use by algorithms
        protected string DataPath
        {
            set
            {
                if (!Directory.Exists(value))
                    throw new Exception(string.Format("invalid data path {0}", value));

                DataSource.DataPath = value;
            }

            get
            {
                return DataSource.DataPath;
            }
        }

        protected List<DataSource> DataSources = new List<DataSource>();

        protected DateTime StartTime;
        protected DateTime EndTime;

        protected IEnumerable<DateTime> SimTime
        {
            get
            {
                Dictionary<DataSource, bool> hasData = new Dictionary<DataSource, bool>();

                foreach (DataSource instr in DataSources)
                {
                    instr.LoadData(StartTime);
                    instr.BarEnumerator.Reset();
                    hasData[instr] = instr.BarEnumerator.MoveNext();
                }

                while (hasData.Select(x => x.Value ? 1 : 0).Sum() > 0)
                {
                    DateTime simTime = DataSources
                        .Where(i => hasData[i])
                        .Min(i => i.BarEnumerator.Current.TimeStamp);

                    foreach (DataSource instr in DataSources)
                    {
                        while (hasData[instr] && instr.BarEnumerator.Current.TimeStamp == simTime)
                        {
                            if (!Instruments.ContainsKey(instr.BarEnumerator.Current.Symbol))
                                Instruments[instr.BarEnumerator.Current.Symbol] = new Instrument(this);
                            Instruments[instr.BarEnumerator.Current.Symbol].Value = instr.BarEnumerator.Current;
                            hasData[instr] = instr.BarEnumerator.MoveNext();
                        }
                    }

                    List<Order> ordersOpenNextBar = PendingOrders
                        .Where(o => o.Execution == OrderExecution.openNextBar)
                        .ToList();

                    foreach (Order order in ordersOpenNextBar)
                        ExecOrder(order);

                    yield return simTime;

                    List<Order> ordersCloseThisBar = PendingOrders
                        .Where(o => o.Execution == OrderExecution.closeThisBar)
                        .ToList();

                    foreach (Order order in ordersCloseThisBar)
                        ExecOrder(order);
                }

                yield break;
            }
        }
        protected Dictionary<string, Instrument> Instruments = new Dictionary<string, Instrument>();

        public List<Order> PendingOrders = new List<Order>();
        public Dictionary<Instrument, int> Positions = new Dictionary<Instrument, int>();
        public List<LogEntry> Log = new List<LogEntry>();

        protected double Cash;
        public double NetAssetValue
        {
            get
            {
                double nav = Cash;
                foreach (var instrument in Positions.Keys)
                {
                    nav += Positions[instrument] * instrument.Close[0];
                }

                return nav;
            }
        }
    }
}
//==============================================================================
// end of file
//==============================================================================
// Project:     Trading Simulator
// Name:        AlgoBase
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

    public abstract class AlgoBase
    {
        public string DataPath
        {
            set
            {
                if (!Directory.Exists(value))
                    throw new Exception(string.Format("invalid data path {0}", value));

                InstrumentDataBase.DataPath = value;
            }

            get
            {
                return InstrumentDataBase.DataPath;
            }
        }

        public void AddInstrument(string ticker)
        {
            Instruments.Add(InstrumentDataBase.New(ticker));
        }

        public List<InstrumentDataBase> Instruments
        {
            get;
            private set;
        }

        public DateTime StartTime;
        public DateTime EndTime;

        protected Dictionary<string, TimeSeries<Bar>> Bars = new Dictionary<string, TimeSeries<Bar>>();

        public double FitnessValue
        {
            get;
            protected set;
        }

        protected IEnumerable<DateTime> SimTime
        {
            get
            {
                Dictionary<InstrumentDataBase, bool> hasData = new Dictionary<InstrumentDataBase, bool>();

                foreach (InstrumentDataBase instr in Instruments)
                {
                    instr.LoadData(StartTime);
                    instr.BarEnumerator.Reset();
                    hasData[instr] = instr.BarEnumerator.MoveNext();
                }

                while(hasData.Select(x => x.Value ? 1 : 0).Sum() > 0)
                {
                    DateTime simTime = Instruments
                        .Where(i => hasData[i])
                        .Min(i => i.BarEnumerator.Current.TimeStamp);

                    foreach (InstrumentDataBase instr in Instruments)
                    {
                        while (hasData[instr] && instr.BarEnumerator.Current.TimeStamp == simTime)
                        {
                            if (!Bars.ContainsKey(instr.BarEnumerator.Current.Symbol))
                                Bars[instr.BarEnumerator.Current.Symbol] = new TimeSeries<Bar>();
                            Bars[instr.BarEnumerator.Current.Symbol].Value = instr.BarEnumerator.Current;

                            hasData[instr] = instr.BarEnumerator.MoveNext();
                        }
                    }

                    yield return simTime;
                }

                yield break;
            }
        }

        public AlgoBase()
        {
            Instruments = new List<InstrumentDataBase>();
        }

        virtual public void Run()
        {

        }

        virtual public object Report(ReportType reportType)
        {
            return FitnessValue;
        }
    }
}
//==============================================================================
// end of file
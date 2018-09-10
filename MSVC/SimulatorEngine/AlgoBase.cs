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

        public DateTime StartDate;
        public DateTime EndDate;
        public DateTime SimDate;

        public double FitnessValue
        {
            get;
            protected set;
        }

        protected IEnumerable<BarCollection> Bars
        {
            get
            {
                Dictionary<InstrumentDataBase, bool> hasData = new Dictionary<InstrumentDataBase, bool>();

                foreach (InstrumentDataBase instr in Instruments)
                {
                    instr.LoadData(StartDate);
                    instr.BarEnumerator.Reset();
                    hasData[instr] = instr.BarEnumerator.MoveNext();
                }

                while(hasData.Select(x => x.Value ? 1 : 0).Sum() > 0)
                {
                    SimDate = Instruments
                        .Where(i => hasData[i])
                        .Min(i => i.BarEnumerator.Current.TimeStamp);

                    BarCollection currentBars = new BarCollection();
                    foreach (InstrumentDataBase instr in Instruments)
                    {
                        while (hasData[instr] && instr.BarEnumerator.Current.TimeStamp == SimDate)
                        {
                            currentBars[instr.BarEnumerator.Current.Symbol] = instr.BarEnumerator.Current;
                            hasData[instr] = instr.BarEnumerator.MoveNext();
                        }
                    }

                    if (currentBars.Count > 0)
                        yield return currentBars;
                }

                yield break;
            }
        }

        public AlgoBase()
        {
            SimDate = default(DateTime);
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    enum ReportType { FitnessValue, Plot, Excel };

    abstract class AlgoBase
    {
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

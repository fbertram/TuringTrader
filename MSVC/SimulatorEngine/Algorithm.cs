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
        //---------- public API for use by trading application
        public Algorithm()
        {
            Instruments = new List<Instrument>();
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

        //---------- protected API for use by algorithms
        protected string DataPath
        {
            set
            {
                if (!Directory.Exists(value))
                    throw new Exception(string.Format("invalid data path {0}", value));

                Instrument.DataPath = value;
            }

            get
            {
                return Instrument.DataPath;
            }
        }

        protected void AddInstrument(string ticker)
        {
            Instruments.Add(Instrument.New(ticker));
        }
        protected List<Instrument> Instruments
        {
            get;
            private set;
        }

        protected DateTime StartTime;
        protected DateTime EndTime;

        protected IEnumerable<DateTime> SimTime
        {
            get
            {
                Dictionary<Instrument, bool> hasData = new Dictionary<Instrument, bool>();

                foreach (Instrument instr in Instruments)
                {
                    instr.LoadData(StartTime);
                    instr.BarEnumerator.Reset();
                    hasData[instr] = instr.BarEnumerator.MoveNext();
                }

                while (hasData.Select(x => x.Value ? 1 : 0).Sum() > 0)
                {
                    DateTime simTime = Instruments
                        .Where(i => hasData[i])
                        .Min(i => i.BarEnumerator.Current.TimeStamp);

                    foreach (Instrument instr in Instruments)
                    {
                        while (hasData[instr] && instr.BarEnumerator.Current.TimeStamp == simTime)
                        {
                            if (!Bars.ContainsKey(instr.BarEnumerator.Current.Symbol))
                                Bars[instr.BarEnumerator.Current.Symbol] = new BarSeries();
                            Bars[instr.BarEnumerator.Current.Symbol].Value = instr.BarEnumerator.Current;
                            hasData[instr] = instr.BarEnumerator.MoveNext();
                        }
                    }

                    yield return simTime;
                }

                yield break;
            }
        }
        protected Dictionary<string, BarSeries> Bars = new Dictionary<string, BarSeries>();
    }
}
//==============================================================================
// end of file
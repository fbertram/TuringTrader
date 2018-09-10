//==============================================================================
// Project:     Trading Simulator
// Name:        InstrumentDataBase
// Description: base class for instrument data
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL v3.0
//==============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    public enum InstrumentDataField { infoPath, dataPath, name, symbol, ticker, date, time, open, high, low, close, volume, bid, ask, bidSize, askSize };

    public abstract class InstrumentDataBase
    {
        public static string DataPath = @".\Data";

        public Dictionary<InstrumentDataField, string> Info
        {
            get;
            protected set;
        }

        public InstrumentDataBase(Dictionary<InstrumentDataField, string> info)
        {
            Info = info;
        }

        static public InstrumentDataBase New(string ticker)
        {
            // check for info file
            string infoPathName = string.Format(@"{0}\{1}.inf", DataPath, ticker);
                    
            if (!File.Exists(infoPathName))
                throw new Exception("failed to locate data source info for " + ticker);

            // create info structure
            Dictionary<InstrumentDataField, string> infos = new Dictionary<InstrumentDataField, string>();
            infos[InstrumentDataField.ticker] = ticker;
            infos[InstrumentDataField.symbol] = ticker;
            infos[InstrumentDataField.infoPath] = DataPath;

            // load info file
            string[] lines = File.ReadAllLines(infoPathName);
            foreach (string line in lines)
            {
                int idx = line.IndexOf('=');

                try
                {
                    InstrumentDataField key = (InstrumentDataField)
                        Enum.Parse(typeof(InstrumentDataField), line.Substring(0, idx), true);

                    string value = line.Substring(idx + 1);

                    infos[key] = value;
                }
                catch (Exception)
                {
                    throw new Exception(string.Format("error parsing data source info for {0}: line '{1}", ticker, line));
                }
            }

            // instantiate data source
            return new InstrumentDataCsv(infos);
        }

        abstract public IEnumerator<Bar> BarEnumerator
        {
            get;
        }

        abstract public void LoadData(DateTime startTime);
    }
}
//==============================================================================
// end of file
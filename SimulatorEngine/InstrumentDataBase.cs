using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUB_TradingSim
{
    enum InstrumentDataField { infoPath, dataPath, name, symbol, ticker, date, time, open, high, low, close, volume, bid, ask, bidSize, askSize };

    abstract class InstrumentDataBase
    {
        static private readonly string _infoPath = @"\..\..\DataInfo\";

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
            string infoPath = string.Format(@"{0}{1}", Directory.GetCurrentDirectory(), _infoPath);
            string infoPathName = string.Format(@"{0}{1}.inf", infoPath, ticker);
                    
            if (!File.Exists(infoPathName))
                throw new Exception("failed to locate data source info for " + ticker);

            // create info structure
            Dictionary<InstrumentDataField, string> infos = new Dictionary<InstrumentDataField, string>();
            infos[InstrumentDataField.ticker] = ticker;
            infos[InstrumentDataField.symbol] = ticker;
            infos[InstrumentDataField.infoPath] = infoPath;

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

        abstract public IEnumerator<BarType> BarEnumerator
        {
            get;
        }

        abstract public void LoadData(DateTime startTime);
    }
}

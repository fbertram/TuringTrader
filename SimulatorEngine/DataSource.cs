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
    public enum DataSourceValue
    {
        infoPath, dataPath,
        nickName, name, ticker, symbol,
        date, time,
        open, high, low, close, bid, ask,
        volume, bidSize, askSize,
        optionExpiration, optionStrike, optionRight, optionUnderlying
    };

    public abstract class DataSource
    {
        public static string DataPath = @".\Data";

        public Dictionary<DataSourceValue, string> Info
        {
            get;
            protected set;
        }

        protected DataSource(Dictionary<DataSourceValue, string> info)
        {
            Info = info;
        }

        public bool IsOption
        {
            get
            {
                return Info.ContainsKey(DataSourceValue.optionExpiration);
            }
        }
        public string OptionUnderlying
        {
            get
            {
                return Info[DataSourceValue.optionUnderlying];
            }
        }

        static public DataSource New(string nickname)
        {
            // check for info file
            string infoPathName = string.Format(@"{0}\{1}.inf", DataPath, nickname);
                    
            if (!File.Exists(infoPathName))
                throw new Exception("failed to locate data source info for " + nickname);

            // create info structure
            Dictionary<DataSourceValue, string> infos = new Dictionary<DataSourceValue, string>();
            infos[DataSourceValue.nickName] = nickname;
            infos[DataSourceValue.ticker] = nickname;
            infos[DataSourceValue.symbol] = nickname;
            infos[DataSourceValue.infoPath] = DataPath;

            // load info file
            string[] lines = File.ReadAllLines(infoPathName);
            foreach (string line in lines)
            {
                int idx = line.IndexOf('=');

                try
                {
                    DataSourceValue key = (DataSourceValue)
                        Enum.Parse(typeof(DataSourceValue), line.Substring(0, idx), true);

                    string value = line.Substring(idx + 1);

                    infos[key] = value;
                }
                catch (Exception)
                {
                    throw new Exception(string.Format("error parsing data source info for {0}: line '{1}", nickname, line));
                }
            }

            // instantiate data source
            return new DataSourceCsv(infos);
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
//==============================================================================
// Project:     Trading Simulator
// Name:        DataSource
// Description: base class for instrument data
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    #region public enum DataSourceValue
    public enum DataSourceValue
    {
        infoPath, dataPath,
        nickName, name, ticker, symbol,
        date, time,
        open, high, low, close, bid, ask,
        volume, bidSize, askSize,
        optionExpiration, optionStrike, optionRight, optionUnderlying
    };
    #endregion

    /// <summary>
    /// data source, providing a bar enumerator for one or more instruments
    /// </summary>
    public abstract class DataSource
    {
        public static string DataPath = @".\Data";

        //----- object factory
        #region static public DataSource New(string nickname)
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
        #endregion
        #region protected DataSource(Dictionary<DataSourceValue, string> info)
        protected DataSource(Dictionary<DataSourceValue, string> info)
        {
            Info = info;
        }
        #endregion

        //----- data source info
        #region public Dictionary<DataSourceValue, string> Info
        public Dictionary<DataSourceValue, string> Info
        {
            get;
            protected set;
        }
        #endregion
        #region public bool IsOption
        public bool IsOption
        {
            get
            {
                return Info.ContainsKey(DataSourceValue.optionExpiration);
            }
        }
        #endregion
        #region public string OptionUnderlying
        public string OptionUnderlying
        {
            get
            {
                return Info[DataSourceValue.optionUnderlying];
            }
        }
        #endregion

        //----- abstract methods to be implemented by derived classes
        #region abstract public IEnumerator<Bar> BarEnumerator
        abstract public IEnumerator<Bar> BarEnumerator
        {
            get;
        }
        #endregion
        abstract public void LoadData(DateTime startTime, DateTime endTime);
    }
}
//==============================================================================
// end of file
//==============================================================================
// Project:     Trading Simulator
// Name:        Bar
// Description: data structure for single instrument bar
// History:     2018ix11, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    public class Bar
    {
        #region Bar(...)
        public Bar(Dictionary<DataSourceValue, string> info, string[] items)
        {
            Symbol = info[DataSourceValue.ticker];
            DateTime date = default(DateTime);
            DateTime time = default(DateTime);
            HasOHLC = false;
            HasBidAsk = false;

            foreach (var mapping in info)
            {
                string mappedString = string.Format(mapping.Value, items);

                switch (mapping.Key)
                {
                    // for stocks, symbol matches ticker
                    // for options, the symbol adds expiry, right, and strike to the ticker
                    //case DataSourceValue.symbol: Symbol = mappedString;                break;
                    case DataSourceValue.date:   date = DateTime.Parse(mappedString);  break;
                    case DataSourceValue.time:   time = DateTime.Parse(mappedString);  break;

                    case DataSourceValue.open:  Open = double.Parse(mappedString);  HasOHLC = true; break;
                    case DataSourceValue.high:  High = double.Parse(mappedString);  HasOHLC = true; break;
                    case DataSourceValue.low:   Low = double.Parse(mappedString);   HasOHLC = true; break;
                    case DataSourceValue.close: Close = double.Parse(mappedString); HasOHLC = true; break;
                    case DataSourceValue.volume: Volume = long.Parse(mappedString); break;

                    case DataSourceValue.bid:   Bid = double.Parse(mappedString); HasBidAsk = true; break;
                    case DataSourceValue.ask:   Ask = double.Parse(mappedString); HasBidAsk = true; break;
                    case DataSourceValue.bidSize: BidVolume = long.Parse(mappedString); break;
                    case DataSourceValue.askSize: AskVolume = long.Parse(mappedString); break;

                    case DataSourceValue.optionStrike:     OptionStrike = double.Parse(mappedString);          break;
                    case DataSourceValue.optionExpiration: OptionExpiry = DateTime.Parse(mappedString);        break;
                    case DataSourceValue.optionRight:      OptionIsPut = Regex.IsMatch(mappedString, "^[pP]"); break;
                }
            }

            Time = date.Date + time.TimeOfDay;

            IsOption = OptionStrike != default(double);
            if (IsOption)
            {
                Symbol = string.Format("{0}{1:yyMMdd}{2}{3:D8}",
                            info[DataSourceValue.ticker],
                            OptionExpiry,
                            OptionIsPut ? "P" : "C",
                            (int)Math.Floor(1000.0 * OptionStrike));
            }
        }
        #endregion

        public readonly string Symbol;
        public readonly DateTime Time;

        public readonly double Open;
        public readonly double High;
        public readonly double Low;
        public readonly double Close;
        public readonly long Volume;
        public readonly bool HasOHLC;

        public readonly double Bid;
        public readonly double Ask;
        public readonly long BidVolume;
        public readonly long AskVolume;
        public readonly bool HasBidAsk;

        public readonly bool IsOption;
        public readonly DateTime OptionExpiry;
        public readonly double OptionStrike;
        public readonly bool OptionIsPut;
    }
}
//==============================================================================
// end of file
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
    /// <summary>
    /// container holding info for single bar
    /// </summary>
    public class Bar
    {
        #region Bar(...)
        public Bar(
            string symbol, DateTime time,
            double open, double high, double low, double close, long volume, bool hasOHLC,
            double bid, double ask, long bidVolume, long askVolume, bool hasBidAsk,
            DateTime optionExpiry, double optionStrike, bool optionIsPut)
        {
            Symbol = symbol;
            Time = time;

            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            HasOHLC = hasOHLC;

            Bid = bid;
            Ask = ask;
            BidVolume = bidVolume;
            AskVolume = askVolume;
            HasBidAsk = hasBidAsk;
            IsBidAskValid = 5.0 * Bid > Ask
                && BidVolume > 0
                && AskVolume > 0;

            OptionExpiry = optionExpiry;
            OptionStrike = optionStrike;
            OptionIsPut = optionIsPut;
            IsOption = optionStrike != default(double);
            if (IsOption)
            {
                Symbol = string.Format("{0}{1:yyMMdd}{2}{3:D8}",
                            Symbol,
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
        public readonly bool IsBidAskValid;

        public readonly DateTime OptionExpiry;
        public readonly double OptionStrike;
        public readonly bool OptionIsPut;
        public readonly bool IsOption;
    }
}
//==============================================================================
// end of file
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

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Container class holding all fields of a single bar, most notably
    /// time stamps and price information. Bar objects are read-only in nature,
    /// therefore all values need to be provided during object construction.
    /// </summary>
    public class Bar
    {
        #region public Bar(...)
        /// <summary>
        /// Create and initialize a bar object.
        /// </summary>
        /// <param name="ticker">ticker, most often same as symbol</param>
        /// <param name="time">Initializer for Time field</param>
        /// <param name="open">Initializer for Open field</param>
        /// <param name="high">Initializer for High field</param>
        /// <param name="low">Initializer for Low field</param>
        /// <param name="close">Initializer for Close field</param>
        /// <param name="volume">Initializer for Volume field</param>
        /// <param name="hasOHLC">Initializer for HasOHLC field</param>
        /// <param name="bid">Initializer for Bid field</param>
        /// <param name="ask">Initializer for Ask field</param>
        /// <param name="bidVolume">Initializer for BidVolume field</param>
        /// <param name="askVolume">Initializer for AskVolume field</param>
        /// <param name="hasBidAsk">Initializer for HasBidAsk field</param>
        /// <param name="optionExpiry">Initializer for OptionExpiry field</param>
        /// <param name="optionStrike">Initializer for OptionStrike field</param>
        /// <param name="optionIsPut">Initializer for OptionIsPut field</param>
        public Bar(
            string ticker, DateTime time,
            double open, double high, double low, double close, long volume, bool hasOHLC,
            double bid, double ask, long bidVolume, long askVolume, bool hasBidAsk,
            DateTime optionExpiry, double optionStrike, bool optionIsPut)
        {
            Symbol = ticker; // default value, changed for options below
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

        #region public readonly string Symbol
        /// <summary>
        /// Fully qualified instrument symbol. Examples are AAPL, or
        /// XSP080119C00152000.
        /// </summary>
        public readonly string Symbol;
        #endregion
        #region public readonly DateTime Time
        /// <summary>
        /// Time stamp, with date and time
        /// </summary>
        public readonly DateTime Time;
        #endregion

        #region public readonly double Open
        /// <summary>
        /// Open price.
        /// </summary>
        public readonly double Open;
        #endregion
        #region public readonly double High
        /// <summary>
        /// High price.
        /// </summary>
        public readonly double High;
        #endregion
        #region public readonly double Low
        /// <summary>
        /// Low price.
        /// </summary>
        public readonly double Low;
        #endregion
        #region public readonly double Close
        /// <summary>
        /// Close price.
        /// </summary>
        public readonly double Close;
        #endregion
        #region public readonly long Volume
        /// <summary>
        /// Trading volume.
        /// </summary>
        public readonly long Volume;
        #endregion
        #region public readonly bool HasOHLC
        /// <summary>
        /// Flag indicating availability of Open/ High/ Low/ Close pricing.
        /// </summary>
        public readonly bool HasOHLC;
        #endregion

        #region public readonly double Bid
        /// <summary>
        /// Bid price.
        /// </summary>
        public readonly double Bid;
        #endregion
        #region public readonly double Ask
        /// <summary>
        /// Asking price.
        /// </summary>
        public readonly double Ask;
        #endregion
        #region public readonly long BidVolume
        /// <summary>
        ///  Bid volume.
        /// </summary>
        public readonly long BidVolume;
        #endregion
        #region public readonly long AskVolume
        /// <summary>
        ///  Ask volume.
        /// </summary>
        public readonly long AskVolume;
        #endregion
        #region public readonly bool HasBidAsk;
        /// <summary>
        /// Flag indicating availability of Bid/ Ask pricing.
        /// </summary>
        public readonly bool HasBidAsk;
        #endregion
        #region public readonly bool IsBidAskValid
        /// <summary>
        /// Flag indicating validity of Bid/ Ask pricing.
        /// </summary>
        public readonly bool IsBidAskValid;
        #endregion

        #region public readonly DateTime OptionExpiry
        /// <summary>
        /// Only valid for options: Option expiry date.
        /// </summary>
        public readonly DateTime OptionExpiry;
        #endregion
        #region public readonly double OptionStrike
        /// <summary>
        /// Only valid for options: Option strike price. 
        /// </summary>
        public readonly double OptionStrike;
        #endregion
        #region public readonly bool OptionIsPut
        /// <summary>
        /// Only valid for options: true for puts, false for calls.
        /// </summary>
        public readonly bool OptionIsPut;
        #endregion
        #region public readonly bool IsOption
        /// <summary>
        /// Flag indicating validity of option fields.
        /// </summary>
        public readonly bool IsOption;
        #endregion
    }
}
//==============================================================================
// end of file
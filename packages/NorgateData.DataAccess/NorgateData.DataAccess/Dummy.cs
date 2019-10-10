//==============================================================================
// Project:     Norgate Data dummy dll
// Name:        Dummy.cs
// Description: dummy DLL for Norgate Data, modeled after API v4.0.7.105
//              this is required, so that we don't need to redistribute
//              Norgate's original DLLs as part of TuringTrader.
// History:     2019i06, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NorgateData.DataAccess
{
    public class RecOHLC
    {
        public DateTime Date;
        public float? Open;
        public float? High;
        public float? Low;
        public float? Close;
        public float? Volume;
        public float? Aux1;
        public float? Aux2;
        public float? Aux3;
        public int? Status;

        public RecOHLC() { }
        public RecOHLC(DateTime date, float? open, float? high, float? low, float? close, float? volume, float? turnover, int? status) { }
        public RecOHLC(DateTime date, float? open, float? high, float? low, float? close, float? volume, float? aux1, float? aux2, float? aux3, int? status) { }
    }

    public class RecIndicator
    {
        public DateTime Date;
        public double? value;
    }

    public class OperationResult
    {
        public static readonly OperationResult Success;
        public static readonly OperationResult Warning;
        public static readonly OperationResult AccessDenied;
        public static readonly OperationResult DatabaseNotAvailable;
        public static readonly OperationResult DatabaseCorruption;

        public OperationResult() { }

        public string ErrorMessage { get; }

        public int GetErrorCode() { return 0; }
        public bool IsSuccess() { return false; }
    }

    public enum AdjustmentType
    {
        None = 0,
        Capital = 1,
        CapitalSpecial = 2,
        TotalReturn = 3
    }

    public enum PaddingType
    {
        None = 0,
        Reserved1 = 1,
        Reserved2 = 2,
        AllMarketDays = 3,
        AllWeekDays = 4,
        AllCalendarDays = 5
    }

    public static class Api
    {
        public static DateTime LastDatabaseUpdateTime { get; }
        public static DateTime GetSecondLastQuotedDate(string symbol) { return default(DateTime); }
        public static AdjustmentType SetAdjustmentType { get; set; }
        public static PaddingType SetPaddingType { get; set; }
        public static OperationResult GetData(string symbol, out List<RecOHLC> result, DateTime fromDate, DateTime toDate) { result = null; return null; }
        public static string GetSecurityName(Int32 symbol) { return default(string); }
        public static string GetSecurityName(string symbol) { return default(string); }
        public static OperationResult GetWatchlist(string watchlistName, out WatchListLibrary.Watchlist watchlist) { watchlist = null; return null; }
        public static OperationResult GetIndexConstituentTimeSeries(int assetid, out List<RecIndicator> indexConstituentTimeSeries, string indexname, DateTime startdate, DateTime enddate, PaddingType paddingSetting) { indexConstituentTimeSeries = null;  return null; }
    }
}

namespace NorgateData.WatchListLibrary
{
    public enum WatchListType
    {
        Dynamic = 0,
        Static = 1,
        Set = 2,
        Defaults = 3
    }

    public class Security
    {
        public int AssetID;

        public string Symbol { get; set; }
        public string Name { get; set; }
    }
    public class SecurityList : List<Security>, IList<Security>, ICollection<Security>, IEnumerable<Security>
    {
    }

    public class Watchlist
    {
        public string Name;
        public WatchListType WatchListType;

        public DataAccess.OperationResult GetSecurityList(out SecurityList items) { items = null;  return null; }
    }
}

//==============================================================================
// end of file
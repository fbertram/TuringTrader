//==============================================================================
// Project:     TuringTrader, FXMacroData calendar sample
// Name:        Program.cs
// Description: Sample strategy using FXMacroData macro-event blackout dates
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using TuringTrader.SimulatorV2;
using TuringTrader.SimulatorV2.Indicators;
#endregion

namespace FxMacroDataCalendar
{
    internal sealed class MacroBlackoutSample : Algorithm
    {
        private const string AssetName = "$SPX";
        private const string CalendarCurrency = "USD";
        private const int FastTrendDays = 50;
        private const int SlowTrendDays = 200;
        private const string MarketTimeZoneId = "Eastern Standard Time";
        private static readonly DateTime DefaultStartDate = DateTimeOffset
            .Parse("2007-01-01T16:00:00-05:00", CultureInfo.InvariantCulture)
            .DateTime;

        private readonly ReleaseCalendarClient _calendarClient = new ReleaseCalendarClient();
        private readonly TimeZoneInfo _marketTimeZone = FindMarketTimeZone();
        private HashSet<DateTime> _blackoutDates = new HashSet<DateTime>();

        public override string Name => "FXMacroData Macro Blackout Sample";

        public override void Run()
        {
            StartDate = StartDate ?? DefaultStartDate;
            EndDate = EndDate ?? TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _marketTimeZone)
                .Date
                .AddHours(16);
            WarmupPeriod = TimeSpan.FromDays(365);

            _blackoutDates = new HashSet<DateTime>(
                _calendarClient.TopTierBlackoutDates(CalendarCurrency, this, _marketTimeZone));

            SimLoop(() =>
            {
                var asset = Asset(AssetName);
                var trendSignal = asset.Close.EMA(FastTrendDays)[0] > asset.Close.EMA(SlowTrendDays)[0];
                var targetWeight = trendSignal ? 1.0 : 0.0;

                if (IsMacroBlackout(SimDate))
                    targetWeight = 0.0;

                if (Math.Abs(asset.Position - targetWeight) > 0.05)
                    asset.Allocate(targetWeight, OrderType.openNextBar);

                if (!IsOptimizing)
                {
                    Plotter.SelectChart(Name, "Date");
                    Plotter.SetX(SimDate);
                    Plotter.Plot(Name, NetAssetValue);
                    Plotter.Plot(AssetName, asset.Close[0]);
                    Plotter.Plot("Macro blackout", IsMacroBlackout(SimDate) ? 1.0 : 0.0);
                }
            });

            if (!IsOptimizing)
            {
                Plotter.AddTargetAllocation();
                Plotter.AddHistoricalAllocations();
                Plotter.AddTradeLog();
            }
        }

        private bool IsMacroBlackout(DateTime simDate)
        {
            return _blackoutDates.Contains(simDate.Date);
        }

        private static TimeZoneInfo FindMarketTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(MarketTimeZoneId);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException || ex is InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            }
        }
    }

    internal sealed class ReleaseCalendarClient
    {
        private const string CalendarUrl = "https://fxmacrodata.com/api/v1/calendar/";
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

        public IReadOnlyList<DateTime> TopTierBlackoutDates(
            string currency,
            Algorithm parentAlgorithm,
            TimeZoneInfo marketTimeZone)
        {
            if (parentAlgorithm.StartDate == null || parentAlgorithm.EndDate == null)
                throw new InvalidOperationException("Set StartDate and EndDate before loading FXMacroData calendar events.");

            var calendarEvents = FetchCalendar(currency, (DateTime)parentAlgorithm.StartDate, (DateTime)parentAlgorithm.EndDate);
            if (calendarEvents.Count == 0 && parentAlgorithm.EndDate.Value.Date < DateTime.UtcNow.Date)
                throw new InvalidOperationException(
                    "FXMacroData does not currently provide confirmed historical release timestamps for this range. "
                    + "Do not run a historical blackout backtest until the calendar has coverage for the requested period.");

            return calendarEvents
                .Where(IsTopTier)
                .Select(item => LocalEventDate(item, marketTimeZone))
                .Where(date => date != DateTime.MinValue)
                .Distinct()
                .OrderBy(date => date)
                .ToArray();
        }

        private static IReadOnlyList<CalendarEvent> FetchCalendar(string currency, DateTime startDate, DateTime endDate)
        {
            var url = CalendarUrl
                + Uri.EscapeDataString(currency.ToUpperInvariant())
                + "?start_date="
                + Uri.EscapeDataString(startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                + "&end_date="
                + Uri.EscapeDataString(endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            using var client = new HttpClient { Timeout = RequestTimeout };
            var json = client.GetStringAsync(url).Result;
            var payload = JsonSerializer.Deserialize<CalendarResponse>(json);
            return payload?.Data ?? new List<CalendarEvent>();
        }

        private static bool IsTopTier(CalendarEvent item)
        {
            return item.TopTierForCurrency || item.MarketTier == 1;
        }

        private static DateTime LocalEventDate(CalendarEvent item, TimeZoneInfo marketTimeZone)
        {
            if (!string.IsNullOrWhiteSpace(item.AnnouncementDatetimeUtc)
                && DateTimeOffset.TryParse(
                    item.AnnouncementDatetimeUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var utcTimestamp))
            {
                return TimeZoneInfo.ConvertTime(utcTimestamp, marketTimeZone).Date;
            }

            if (!string.IsNullOrWhiteSpace(item.Date)
                && DateTime.TryParse(
                    item.Date,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var date))
            {
                return date.Date;
            }

            return DateTime.MinValue;
        }
    }

    internal sealed class CalendarResponse
    {
        [JsonPropertyName("data")]
        public List<CalendarEvent> Data { get; set; } = new List<CalendarEvent>();
    }

    internal sealed class CalendarEvent
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("announcement_datetime_utc")]
        public string AnnouncementDatetimeUtc { get; set; } = string.Empty;

        [JsonPropertyName("market_tier")]
        public int MarketTier { get; set; }

        [JsonPropertyName("top_tier_for_currency")]
        public bool TopTierForCurrency { get; set; }
    }

    internal static class Program
    {
        private static void Main()
        {
            var algo = new MacroBlackoutSample();
            algo.Run();

            Console.WriteLine(
                "{0}: {1} bars, {2} trades.",
                algo.Name,
                algo.EquityCurve.Count,
                algo.Account.TradeLog.Count);
        }
    }
}

//==============================================================================
// end of file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FxMacroDataCalendar;

internal static class Program
{
    private static void Main()
    {
        var events = FetchCalendar("USD", "2026-07-01", "2026-07-20");

        Console.WriteLine("Top-tier USD macro blackout dates:");
        foreach (var item in events.Where(e => e.TopTierForCurrency || e.MarketTier == 1))
            Console.WriteLine($"  {EventDate(item)}: {item.Name}");
    }

    private static IReadOnlyList<CalendarEvent> FetchCalendar(string currency, string startDate, string endDate)
    {
        var url = "https://fxmacrodata.com/api/v1/calendar/"
            + currency
            + "?start_date="
            + Uri.EscapeDataString(startDate)
            + "&end_date="
            + Uri.EscapeDataString(endDate);

        using var client = new HttpClient();
        var json = client.GetStringAsync(url).Result;
        var payload = JsonSerializer.Deserialize<CalendarResponse>(json);
        return payload?.Data ?? Array.Empty<CalendarEvent>();
    }

    private static string EventDate(CalendarEvent item)
    {
        var value = item.AnnouncementDatetimeUtc ?? item.Date ?? string.Empty;
        return value.Length > 10 ? value.Substring(0, 10) : value;
    }
}

internal sealed class CalendarResponse
{
    [JsonPropertyName("data")]
    public List<CalendarEvent> Data { get; set; } = new();
}

internal sealed class CalendarEvent
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("announcement_datetime_utc")]
    public string? AnnouncementDatetimeUtc { get; set; }

    [JsonPropertyName("market_tier")]
    public int MarketTier { get; set; }

    [JsonPropertyName("top_tier_for_currency")]
    public bool TopTierForCurrency { get; set; }
}

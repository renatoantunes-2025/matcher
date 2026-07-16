using System.Globalization;

namespace MatchR.Api;

public static class Helpers
{
    public static string Initials(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpper(p[0])));

    public static string FormatDate(DateTime utc)
    {
        var local = utc; // stored/displayed as-is; server and DB assumed to share the same local time zone
        var isToday = local.Date == DateTime.UtcNow.Date;
        return isToday
            ? $"Hoje, {local:HH:mm}"
            : local.ToString("dd MMM, HH:mm", new CultureInfo("pt-BR"));
    }

    public static string FormatPrice(decimal value) =>
        value.ToString("C0", new CultureInfo("pt-BR"));
}

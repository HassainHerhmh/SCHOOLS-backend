using System.Text.RegularExpressions;

namespace SchoolsManagement.Api.Services;

public static class BusMapsUrlHelper
{
    public static string? NormalizeUrl(string? raw)
    {
        var trimmed = raw?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public static string ToNavigationUrl(string rawUrl)
    {
        var url = rawUrl.Trim();
        if (url.Contains("/maps/dir/", StringComparison.OrdinalIgnoreCase)
            || url.Contains("maps.app.goo.gl", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (TryParseCoordinates(url, out var lat, out var lng))
        {
            return $"https://www.google.com/maps/dir/?api=1&destination={lat},{lng}";
        }

        return url;
    }

    public static bool TryParseCoordinates(string? rawUrl, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return false;
        }

        var atMatch = Regex.Match(rawUrl, @"@(-?\d+\.\d+),\s*(-?\d+\.\d+)");
        if (atMatch.Success
            && double.TryParse(atMatch.Groups[1].Value, out latitude)
            && double.TryParse(atMatch.Groups[2].Value, out longitude))
        {
            return true;
        }

        var qMatch = Regex.Match(rawUrl, @"[?&]q=(-?\d+\.\d+),\s*(-?\d+\.\d+)");
        if (qMatch.Success
            && double.TryParse(qMatch.Groups[1].Value, out latitude)
            && double.TryParse(qMatch.Groups[2].Value, out longitude))
        {
            return true;
        }

        return false;
    }
}

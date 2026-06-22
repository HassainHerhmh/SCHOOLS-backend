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

        var patterns = new[]
        {
            @"@(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?)",
            @"[?&]q=(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?)",
            @"[?&]query=(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?)",
            @"[?&]ll=(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?)",
            @"[?&]destination=(-?\d+(?:\.\d+)?),\s*(-?\d+(?:\.\d+)?)",
            @"!3d(-?\d+(?:\.\d+)?)!4d(-?\d+(?:\.\d+)?)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(rawUrl, pattern, RegexOptions.IgnoreCase);
            if (match.Success
                && double.TryParse(match.Groups[1].Value, out latitude)
                && double.TryParse(match.Groups[2].Value, out longitude))
            {
                return true;
            }
        }

        return false;
    }
}

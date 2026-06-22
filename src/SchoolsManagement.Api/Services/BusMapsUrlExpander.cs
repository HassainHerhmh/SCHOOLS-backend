using System.Net;

namespace SchoolsManagement.Api.Services;

public class BusMapsUrlExpander
{
    private readonly IHttpClientFactory _httpClientFactory;

    public BusMapsUrlExpander(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<string?> NormalizeForStorageAsync(string? rawUrl, CancellationToken ct = default)
    {
        var url = BusMapsUrlHelper.NormalizeUrl(rawUrl);
        if (url is null)
        {
            return null;
        }

        if (BusMapsUrlHelper.TryParseCoordinates(url, out _, out _))
        {
            return url;
        }

        if (!IsShortMapsUrl(url))
        {
            return url;
        }

        var expanded = await ExpandShortUrlAsync(url, ct);
        return BusMapsUrlHelper.NormalizeUrl(expanded) ?? url;
    }

    public async Task<(double Latitude, double Longitude)?> ResolveCoordinatesAsync(string? rawUrl, CancellationToken ct = default)
    {
        var url = await NormalizeForStorageAsync(rawUrl, ct);
        if (url is null)
        {
            return null;
        }

        if (BusMapsUrlHelper.TryParseCoordinates(url, out var lat, out var lng))
        {
            return (lat, lng);
        }

        return null;
    }

    private static bool IsShortMapsUrl(string url) =>
        url.Contains("maps.app.goo.gl", StringComparison.OrdinalIgnoreCase)
        || url.Contains("goo.gl/maps", StringComparison.OrdinalIgnoreCase)
        || url.Contains("goo.gl/", StringComparison.OrdinalIgnoreCase);

    private async Task<string?> ExpandShortUrlAsync(string url, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(BusMapsUrlExpander));
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SchoolsManagement.Api/1.0");

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            return response.RequestMessage?.RequestUri?.ToString()
                   ?? response.Headers.Location?.ToString();
        }
        catch
        {
            return null;
        }
    }
}

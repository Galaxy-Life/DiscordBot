using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AdvancedBot.Core.Services;

public class CloudflareService : IDisposable
{
    private readonly HttpClient _client;

    public CloudflareService()
    {
        _client = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(10),
        };

        var test = Environment.GetEnvironmentVariable("CLOUDFLARE__TOKEN");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", test);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public async Task<bool> PurgeStagingCacheAsync()
    {
        try
        {
            var content = JsonContent.Create(new { hosts = new[] { "staging.galaxylifegame.net" } });
            using var response = await _client.PostAsync($"https://api.cloudflare.com/client/v4/zones/c8f15f27bfa4a7f1b0e93191d485695a/purge_cache", content);

            var result = await response.Content.ReadFromJsonAsync<CloudflareResponse>();
            if (result is null) return false;

            if (!result.Success)
                Console.WriteLine("Cloudflare CDN purge failed: {0}", result.Errors);

            return result.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }

    private record CloudflareResponse(bool Success, CloudflareError[] Errors);
    private record CloudflareError(int Code, string Message);
}

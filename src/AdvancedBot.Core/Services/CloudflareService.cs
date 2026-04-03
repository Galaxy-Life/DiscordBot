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
            using var response = await _client.PostAsync($"https://api.cloudflare.com/client/v4/zones/f1541c5eb98fe6c7f84dce02b2660da1/purge_cache", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }
}

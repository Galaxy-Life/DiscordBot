using AdvancedBot.Core.Entities;
using Phoenix.Api;
using Phoenix.ApiWrapper;
using Phoenix.ApiWrapper.Entities;

namespace AdvancedBot.Core.Services;

public class PhoenixWrapperService
{
    private PhoenixCredentials _credentials;
    private ApiClient _defaultClient;

    private readonly List<ulong> _powerUsers =
    [
        202095042372829184, // svr333
        942849642931032164, // lifecoder
        180676108088246272, // lodethebig
        356060824223350784, // andyvv.
        275698828974489612, // magniolya
        424689465450037278  // bryan
    ];

    public PhoenixWrapperService(PhoenixCredentials credentials)
    {
        _credentials = credentials;

        var client = new PhoenixClients(
            new HttpClient() { Timeout = TimeSpan.FromSeconds(10) },
            new PhoenixApiClientOptions
            {
                TokenEndpoint = new Uri("https://accounts.phoenixnetwork.net/api/oauth/token"),
                ClientId = _credentials.ClientId,
                ClientSecret = _credentials.ClientSecret,
                Scopes = [],
            }).PhoenixClient ?? throw new Exception("Failed to create default profile");

        _defaultClient = client;
    }

    public Dictionary<ulong, ApiClient> PhoenixClients { get; set; } = [];

    public ApiClient GetClient(ulong discordId)
    {
        if (!_powerUsers.Contains(discordId))
        {
            return _defaultClient;
        }

        if (PhoenixClients.ContainsKey(discordId))
        {
            return PhoenixClients[discordId];
        }

        var phoenix = new PhoenixClients(
            new HttpClient() { Timeout = TimeSpan.FromSeconds(10) },
            new PhoenixApiClientOptions
            {
                TokenEndpoint = new Uri("https://accounts.phoenixnetwork.net/api/oauth/token"),
                ClientId = _credentials.ClientId,
                ClientSecret = _credentials.ClientSecret,
                Scopes = ["accounts:edit", "accounts:moderate", "accounts:read", "token_exchange:obo"],
                EnableTokenExchange = true,
                SubjectId = discordId.ToString(),
                SubjectProvider = "discord",
            });

        if (phoenix.PhoenixClient == null)
        {
            throw new Exception($"Failed to initialize PhoenixClient On Behalf Of <@{discordId}> ({discordId})");
        }

        PhoenixClients.Add(discordId, phoenix.PhoenixClient);
        return phoenix.PhoenixClient;
    }
}

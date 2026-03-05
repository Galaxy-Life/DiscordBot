using AdvancedBot.Core.Entities;
using Phoenix.Api;
using Phoenix.ApiWrapper;
using Phoenix.ApiWrapper.Entities;

namespace AdvancedBot.Core.Services;

public class PhoenixWrapperService
{
    private PhoenixCredentials _credentials;

    public PhoenixWrapperService(PhoenixCredentials credentials)
    {
        _credentials = credentials;
    }

    public Dictionary<ulong, ApiClient> PhoenixClients { get; set; } = [];

    public ApiClient GetClient(ulong discordId)
    {
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

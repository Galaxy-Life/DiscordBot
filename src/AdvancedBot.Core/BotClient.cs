using Discord;
using Discord.WebSocket;
using AdvancedBot.Core.Services.Commands;
using AdvancedBot.Core.Services.DataStorage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands;
using AdvancedBot.Core.Services;
using GL.NET;
using Discord.Net;
using Newtonsoft.Json;
using System.Linq;

namespace AdvancedBot.Core
{
    public class BotClient
    {
        private DiscordSocketClient _client;
        private CustomCommandService _commands;
        private IServiceProvider _services;

        public BotClient(CustomCommandService commands = null, DiscordSocketClient client = null)
        {
            _client = client ?? new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 1000
            });

            _commands = commands ?? new CustomCommandService(new CustomCommandServiceConfig
            {
                CaseSensitiveCommands = false,
                LogLevel = LogSeverity.Info,
                BotInviteIsPrivate = false,
                RepositoryUrl = "https://github.com/Galaxy-Life/DiscordBot"
            });
        }

        public async Task InitializeAsync()
        {
            _services = ConfigureServices();

            _client.Ready += OnReadyAsync;

            _client.Log += LogAsync;
            _commands.Log += LogAsync;

            var token = Environment.GetEnvironmentVariable("Token");

            await Task.Delay(10).ContinueWith(t => _client.LoginAsync(TokenType.Bot, token));
            await _client.StartAsync();

            await _services.GetRequiredService<CommandHandlerService>().InitializeAsync();
            await Task.Delay(-1);
        }

        private async Task LogAsync(LogMessage msg)
            => Console.WriteLine($"{msg.Source}: {msg.Message}");

        private async Task OnReadyAsync()
        {
            await _client.SetGameAsync("Galaxy Life");

            //await FirstTimeCommandAdder();
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<LiteDBHandler>()
                .AddSingleton<GuildAccountService>()
                .AddSingleton<PaginatorService>()
                .AddSingleton<CommandPermissionService>()
                .AddSingleton<GLAsyncClient>()
                .BuildServiceProvider();
        }

        private async Task FirstTimeCommandAdder()
        {
            var statusCommand = new SlashCommandBuilder() {  IsDMEnabled = false };
            statusCommand.WithName("status");
            statusCommand.WithDescription("Shows the current status of the game servers");

            var profileCommand = new SlashCommandBuilder() { IsDMEnabled = false };
            profileCommand.WithName("profile");
            profileCommand.WithDescription("Displays a user's Galaxy Life profile");

            var statsCommand = new SlashCommandBuilder() { IsDMEnabled = false };
            statsCommand.WithName("stats");
            statsCommand.WithDescription("Displays a user's Galaxy Life stats");

            var advancedStatsCommand = new SlashCommandBuilder() { IsDMEnabled = false };
            advancedStatsCommand.WithName("advancedstats");
            advancedStatsCommand.WithDescription("Displays a user's extensive Galaxy Life stats");

            var asCommand = new SlashCommandBuilder() { IsDMEnabled = false };
            asCommand.WithName("as");
            asCommand.WithDescription("Displays a user's extensive Galaxy Life stats");

            await _client.BulkOverwriteGlobalApplicationCommandsAsync(new ApplicationCommandProperties[] { statusCommand.Build(), profileCommand.Build(), statsCommand.Build(), advancedStatsCommand.Build(), asCommand.Build() } );
        }
    }
}

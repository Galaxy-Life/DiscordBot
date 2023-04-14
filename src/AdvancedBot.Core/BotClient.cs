using Discord;
using Discord.WebSocket;
using AdvancedBot.Core.Services.DataStorage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands;
using AdvancedBot.Core.Services;
using Discord.Interactions;
using System.Reflection;
using AdvancedBot.Core.Entities;
using GL.NET;
using System.IO;

namespace AdvancedBot.Core
{
    public class BotClient
    {
        private DiscordSocketClient _client;
        private CustomCommandService _commands;
        private IServiceProvider _services;
        private InteractionService _interactions;
        private AccountService _accounts;
        private AuthorizedGLClient _glClient;

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
                RepositoryUrl = "https://github.com/Galaxy-Life/DiscordBot",
                LogChannelId = 1090274237572468796
            });

            _interactions = new InteractionService(_client.Rest, new InteractionServiceConfig() { UseCompiledLambda = true });

            var creds = Environment.GetEnvironmentVariable("PhoenixApiCred").Split(';');
            _glClient = new AuthorizedGLClient(creds[0], creds[1], creds[2]);
        }

        public async Task InitializeAsync()
        {
            Console.Title = $"Launching Discord Bot...";
            _services = ConfigureServices();
            _accounts = _services.GetRequiredService<AccountService>();

            _client.Ready += OnReadyAsync;
            _interactions.SlashCommandExecuted += OnSlashCommandExecuted;

            _client.Log += LogAsync;
            _commands.Log += LogAsync;

            var token = Environment.GetEnvironmentVariable("Token");

            await Task.Delay(10).ContinueWith(t => _client.LoginAsync(TokenType.Bot, token));
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task OnReadyAsync()
        {
            Console.Title = $"Running Discord Bot: {_client.CurrentUser.Username}";
            await _client.SetGameAsync("Galaxy Life");
            Console.WriteLine($"Guild count: {_client.Guilds.Count}");

            await _interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);
            Console.WriteLine($"Modules count: {_interactions.Modules.Count}");
            Console.WriteLine($"SlashCommands count: {_interactions.SlashCommands.Count}");

            #if DEBUG
                Console.WriteLine("Registered all commands to test server");
                await _interactions.RegisterCommandsToGuildAsync(696343127144923158, false);
            #else
                Console.WriteLine("Registered all commands globally");
                await _interactions.RegisterCommandsGloballyAsync();
            #endif

            _client.InteractionCreated += async (x) =>
            {
                var context = new SocketInteractionContext(_client, x);
                await _interactions.ExecuteCommandAsync(context, _services);
            };

            _glClient.ErrorThrown += OnGLErrorThrown;
        }

        private async void OnGLErrorThrown(object sender, ErrorEventArgs e)
        {
            var exception = e.GetException();
            await LogAsync(new LogMessage(LogSeverity.Critical, "GL.NET", exception.Message, exception));
        }

        private Task LogAsync(LogMessage msg)
        {
            if (msg.Exception != null)
            {
                Console.WriteLine($"{msg.Source}: {msg.Exception.Message}");
            }
            else
            {
                Console.WriteLine($"{msg.Source}: {msg.Message}");
            }

            return Task.CompletedTask;
        }

        private async Task OnSlashCommandExecuted(SlashCommandInfo cmd, IInteractionContext context, IResult result)
        {
            if (!result.IsSuccess)
            {
                if (result.Error == InteractionCommandError.UnmetPrecondition)
                {
                    await context.Interaction.DeferAsync();
                }

                try
                {
                    await context.Interaction.ModifyOriginalResponseAsync(x => x.Content = $"⛔ {result.ErrorReason}");
                }
                catch (Exception)
                {
                    // failed because took to long to respond
                    await context.Channel.SendMessageAsync($"⛔ {result.ErrorReason}");
                }
            }

            var id = context.Interaction.IsDMInteraction ? context.User.Id : context.Guild.Id;
            var acc = _accounts.GetOrCreateAccount(id, !context.Interaction.IsDMInteraction);

            var cmdInfo = acc.CommandStats.Find(x => x.Name == cmd.Name);

            if (cmdInfo == null)
            {
                acc.CommandStats.Add(new CommandStats(cmd.Name));
                cmdInfo = acc.CommandStats.Find(x => x.Name == cmd.Name);
            }

            cmdInfo.TimesRun++;

            if (!result.IsSuccess)
            {
                cmdInfo.TimesFailed++;
            }

            _accounts.SaveAccount(acc);
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton(_interactions)
                .AddSingleton(_glClient)
                .AddSingleton<LiteDBHandler>()
                .AddSingleton<AccountService>()
                .AddSingleton<PaginatorService>()
                .AddSingleton<LogService>()
                .AddSingleton<ChannelCounterService>()
                .AddSingleton<ModerationService>()
                .AddSingleton<GLService>()
                .BuildServiceProvider();
        }
    }
}

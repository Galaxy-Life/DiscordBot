using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Services;
using AdvancedBot.Core.Services.DataStorage;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GL.NET;
using Microsoft.Extensions.DependencyInjection;

namespace AdvancedBot.Core
{
    public class BotClient
    {
        private readonly DiscordSocketClient client;
        private readonly CustomCommandService commands;
        private IServiceProvider services;
        private readonly InteractionService interactions;
        private AccountService accounts;
        private readonly GLClient glClient;

        public BotClient(CustomCommandService commands = null, DiscordSocketClient client = null)
        {
            this.client = client ?? new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 1000
            });

            this.commands = commands ?? new CustomCommandService(new CustomCommandServiceConfig
            {
                CaseSensitiveCommands = false,
                LogLevel = LogSeverity.Info,
                BotInviteIsPrivate = false,
                RepositoryUrl = "https://github.com/Galaxy-Life/DiscordBot",
#if DEBUG
                LogChannelId = 697920194559082547
#else
                LogChannelId = 1090274237572468796
#endif
            });

            interactions = new InteractionService(this.client.Rest, new InteractionServiceConfig() { UseCompiledLambda = true });

            string envVar = Environment.GetEnvironmentVariable("PhoenixApiCred");

            if (envVar == null)
            {
                logAsync(new LogMessage(LogSeverity.Warning, "BotClient", "Initializing GLClient without tokens!"));
                glClient = new GLClient("", "", "");
                return;
            }

            string[] creds = envVar.Split(';');
            glClient = new GLClient(creds[0], creds[1], creds[2]);
        }

        public async Task InitializeAsync()
        {
            Console.Title = $"Launching Discord Bot...";
            services = configureServices();
            accounts = services.GetRequiredService<AccountService>();

            client.Ready += onReadyAsync;
            interactions.SlashCommandExecuted += onSlashCommandExecuted;

            client.Log += logAsync;
            commands.Log += logAsync;

            string token = Environment.GetEnvironmentVariable("Token");

            await Task.Delay(10).ContinueWith(t => client.LoginAsync(TokenType.Bot, token));
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task onReadyAsync()
        {
            Console.Title = $"Running Discord Bot: {client.CurrentUser.Username}";

            Game activity = new(
              "Galaxy Life",
              ActivityType.Watching,
              ActivityProperties.Instance
            );

            await client.SetActivityAsync(activity);
            Console.WriteLine($"Guild count: {client.Guilds.Count}");

            await interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), services);
            Console.WriteLine($"Modules count: {interactions.Modules.Count}");
            Console.WriteLine($"SlashCommands count: {interactions.SlashCommands.Count}");

#if DEBUG
            Console.WriteLine("Registered all commands to test server");
            await interactions.RegisterCommandsToGuildAsync(696343127144923158, false);
#else
            Console.WriteLine("Registered all commands globally");
            await interactions.RegisterCommandsGloballyAsync();
#endif

            client.InteractionCreated += async (x) =>
            {
                var context = new SocketInteractionContext(client, x);
                await interactions.ExecuteCommandAsync(context, services);
            };

            glClient.ErrorThrown += onGLErrorThrown;
        }

        private async void onGLErrorThrown(object sender, ErrorEventArgs e)
        {
            var exception = e.GetException();
            await logAsync(new LogMessage(LogSeverity.Critical, "GL.NET", exception.Message, exception));
        }

        private Task logAsync(LogMessage msg)
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

        private async Task onSlashCommandExecuted(SlashCommandInfo cmd, IInteractionContext context, IResult result)
        {
            if (!result.IsSuccess)
            {
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

            ulong id = context.Interaction.IsDMInteraction ? context.User.Id : context.Guild.Id;
            var acc = accounts.GetOrCreateAccount(id, !context.Interaction.IsDMInteraction);

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

            accounts.SaveAccount(acc);
        }

        private ServiceProvider configureServices()
        {
            return new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .AddSingleton(interactions)
                .AddSingleton(glClient)
                .AddSingleton<LiteDBHandler>()
                .AddSingleton<AccountService>()
                .AddSingleton<PaginatorService>()
                .AddSingleton<LogService>()
                .AddSingleton<ChannelCounterService>()
                .AddSingleton<ModerationService>()
                .AddSingleton<GLService>()
                .AddSingleton<BotStorage>()
                .BuildServiceProvider();
        }
    }
}

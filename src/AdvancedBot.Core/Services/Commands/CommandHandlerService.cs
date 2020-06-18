using Discord;
using Discord.Commands;
using Discord.WebSocket;
using AdvancedBot.Core.Extensions;
using AdvancedBot.Core.Services.DataStorage;
using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands;

namespace AdvancedBot.Core.Services.Commands
{
    public class CommandHandlerService
    {
        private readonly DiscordSocketClient _client;
        private readonly CustomCommandService _commands;
        private readonly IServiceProvider _services;
        private readonly GuildAccountService _accounts;

        public CommandHandlerService(DiscordSocketClient client, CustomCommandService commands, IServiceProvider services, GuildAccountService accounts)
        {
            _commands = commands;
            _client = client;
            _services = services;
            _accounts = accounts;
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.MessageReceived += OnMessageReceived;
            _commands.CommandExecuted += OnCommandExecuted;
        }

        private async Task OnMessageReceived(SocketMessage msg)
        {
            if (!(msg is SocketUserMessage message)) return;
            if (message.Author == _client.CurrentUser) { return; }
            
            if (message.Channel is IPrivateChannel)
            {
                await message.Channel.SendMessageAsync($"I only respond in guilds.");
                return;
            }

            var guildId = (message.Author as SocketGuildUser).Guild.Id;
            var guild = _accounts.GetOrCreateGuildAccount(guildId);

            int argPos = 0;
            if (!message.HasPrefix(_client, out argPos, guild.Prefixes)) { return; }

            var context = new SocketCommandContext(_client, message);
            var result = await _commands.ExecuteAsync(context, argPos, _services);
        }

        private async Task OnCommandExecuted(Optional<CommandInfo> cmd, ICommandContext ctx, IResult result)
        {
            if (result.IsSuccess) 
            {
                await ctx.Message.AddReactionAsync(new Emoji("✅"));
                return;
            }

            if (result.Error == CommandError.UnknownCommand) return;
            if (result.Error == CommandError.BadArgCount)
            {
                await SendWrongParameterCountMessage(ctx, cmd.Value);
                return;
            }
            

            switch (result.ErrorReason)
            {
                case "User has insuffient permission to execute command.": break;

                default: await SendDefaultErrorMessage(ctx, cmd.Value, result.ErrorReason);
                break;
            }
        }

        private async Task SendDefaultErrorMessage(ICommandContext ctx, CommandInfo cmd, string error)
        {
            error = error.StartsWith("Could not find file") ? "Requested file not found." : error;

            var embed = new EmbedBuilder()
            {
                Color = Color.DarkOrange,
                Title = $"{error}",
            }
            .Build();
        
            await ctx.Channel.SendMessageAsync("", false, embed);
        }
    
        private async Task SendWrongParameterCountMessage(ICommandContext ctx, CommandInfo command)
        {
            var usage = _commands.GenerateCommandUsage(command);

            var embed = new EmbedBuilder()
            .WithTitle("Command wrongly executed")
            .AddField($"Command example:", usage)
            .WithFooter("Tip: <> means mandatory, [] optional")
            .Build();

            await ctx.Channel.SendMessageAsync("", false, embed);
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using AdvancedBot.Core.Entities;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace AdvancedBot.Core.Services
{
    public class PaginatorService
    {
        private IEmote _next = new Emoji("▶️");
        private IEmote _previous = new Emoji("◀️");
        private IEmote _first = new Emoji("⏮️");
        private List<PaginatedMessage> _activeMessages;
        private ConcurrentDictionary<ulong, Timer> _activeTimers;
        private DiscordSocketClient _client;

        public PaginatorService(DiscordSocketClient client)
        {
            _activeMessages = new List<PaginatedMessage>();
            _activeTimers = new ConcurrentDictionary<ulong, Timer>();
            
            _client = client;
            _client.ReactionAdded += OnReactionUpdatedAsync;
        }

        private async Task OnReactionUpdatedAsync(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var message = await msg.GetOrDownloadAsync();

            var paginatedMessage = _activeMessages.Find(x => x.DiscordMessageId == message.Id);
            if (paginatedMessage is null) return;

            if (reaction.UserId != paginatedMessage.DiscordUserId) return;
            await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);

            if (reaction.Emote.Name == _first.Name) await GoToFirstPageAsync(message.Id);
            else if (reaction.Emote.Name == _previous.Name) await GoToPreviousPageAsync(message.Id);
            else if (reaction.Emote.Name == _next.Name) await GoToNextPageAsync(message.Id);
            else if (reaction.Emote.Name == new Emoji("⏭️").Name) await GoToLastPageAsync(message.Id);
            else return;

            ResetTimer(message.Id);
        }

        public async Task<IUserMessage> HandleNewPaginatedMessageAsync(SocketCommandContext context, IEnumerable<string> displayTexts, Embed embed)
        {
            var message = await context.Channel.SendMessageAsync("", false, embed);
            var paginatedMessage = new PaginatedMessage()
            {
                DiscordMessageId = message.Id,
                DiscordChannelId = message.Channel.Id,
                DiscordUserId = context.User.Id,
                DisplayMessages = displayTexts.ToArray()
            };
            _activeMessages.Add(paginatedMessage);
            
            if (paginatedMessage.TotalPages == 1) return message;
            
            await AddPaginatorReactionsAsync(message);
            AddNewTimer(message.Id);
            
            return message;
        }

        public void AddNewTimer(ulong messageId)
        {
            var timer = new Timer();
            timer.Interval = 30000;
            timer.Start();
            timer.Elapsed += DisposeActivePaginatorMessage;
            _activeTimers.TryAdd(messageId, timer);
        }

        private void DisposeActivePaginatorMessage(object timerObj, ElapsedEventArgs e)
        {
            var timer = timerObj as Timer;
            
            var messageId = _activeTimers.First(x => x.Value == timer).Key;
            timer.Enabled = false;

            var paginatorMessage = _activeMessages.First(x => x.DiscordMessageId == messageId);
            
            var channel = _client.GetChannel(paginatorMessage.DiscordChannelId) as SocketTextChannel;
            var message = channel.GetMessageAsync(paginatorMessage.DiscordMessageId).GetAwaiter().GetResult() as SocketUserMessage;
            if (message is null) return;

            message.RemoveAllReactionsAsync().GetAwaiter().GetResult();
            
            _activeMessages.Remove(paginatorMessage);
            _activeTimers.TryRemove(messageId, out Timer oldTimer);
            timer.Dispose();
        }

        public void ResetTimer(ulong messageId)
        {
            _activeTimers.TryRemove(messageId, out Timer currentTimer);

            currentTimer.Stop();
            currentTimer.Start();
            
            _activeTimers.TryAdd(messageId, currentTimer);
        }

        private async Task AddPaginatorReactionsAsync(IUserMessage message)
        {
            await message.AddReactionAsync(new Emoji("⏮️"));
            await message.AddReactionAsync(new Emoji("◀️"));
            await message.AddReactionAsync(new Emoji("▶️"));
            await message.AddReactionAsync(new Emoji("⏭️"));
        }

        private async Task HandleUpdateMessagePagesAsync(PaginatedMessage msg)
        {
            var message = await (_client.GetChannel(msg.DiscordChannelId) as SocketTextChannel).GetMessageAsync(msg.DiscordMessageId) as SocketUserMessage;
            var oldEmbed = message.Embeds.First();

            // get correct messages to display
            var displayMessages = msg.DisplayMessages.Skip((msg.CurrentPage - 1) * 10).Take(10);

            // update title to correct page
            var title = oldEmbed.Title.Split(" | ").First();
            var newTitle = title + $" | Page {msg.CurrentPage}";

            var newEmbed = new EmbedBuilder()
            {
                Title = newTitle,
                Description = string.Join("\n", displayMessages),
                Color = oldEmbed.Color,
                Url = oldEmbed.Url
            }
            .WithFooter(oldEmbed.Footer.Value.Text, oldEmbed.Footer.Value.IconUrl)
            .Build();

            await message.ModifyAsync(x => x.Embed = newEmbed);
        }

        private async Task GoToLastPageAsync(ulong id)
        {
            var paginatorMessage = _activeMessages.Find(x => x.DiscordMessageId == id);
            paginatorMessage.CurrentPage = paginatorMessage.TotalPages;
            await HandleUpdateMessagePagesAsync(paginatorMessage);
        }

        private async Task GoToFirstPageAsync(ulong id)
        {
            var paginatorMessage = _activeMessages.Find(x => x.DiscordMessageId == id);
            paginatorMessage.CurrentPage = 1;
            await HandleUpdateMessagePagesAsync(paginatorMessage);
        }

        private async Task GoToNextPageAsync(ulong id)
        {
            var paginatorMessage = _activeMessages.First(x => x.DiscordMessageId == id);
            paginatorMessage.CurrentPage++;
            await HandleUpdateMessagePagesAsync(paginatorMessage);
        }

        private async Task GoToPreviousPageAsync(ulong id)
        {
            var paginatorMessage = _activeMessages.Find(x => x.DiscordMessageId == id);
            paginatorMessage.CurrentPage--;
            await HandleUpdateMessagePagesAsync(paginatorMessage);
        }
    }
}

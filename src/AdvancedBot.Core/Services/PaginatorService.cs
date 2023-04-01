using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using AdvancedBot.Core.Entities;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace AdvancedBot.Core.Services
{
    public class PaginatorService
    {
        private List<PaginatedMessage> _activeMessages;
        private ConcurrentDictionary<ulong, Timer> _activeTimers;
        private DiscordSocketClient _client;

        public PaginatorService(DiscordSocketClient client)
        {
            _activeMessages = new List<PaginatedMessage>();
            _activeTimers = new ConcurrentDictionary<ulong, Timer>();
            
            _client = client;
            _client.InteractionCreated += OnInteraction;
        }
        public async Task HandleNewPaginatedMessageAsync(SocketInteractionContext context, IEnumerable<EmbedField> displayFields, IEnumerable<string> displayTexts, Embed embed)
        {
            var message = await context.Interaction.GetOriginalResponseAsync();
            
            await context.Interaction.ModifyOriginalResponseAsync(x => x.Embed = embed);

            var paginatedMessage = new PaginatedMessage()
            {
                DiscordMessageId = message.Id,
                DiscordChannelId = context.Interaction.Channel.Id,
                DiscordUserId = context.User.Id,
            };

            if (displayFields == null) paginatedMessage.DisplayMessages = displayTexts.ToArray();
            else paginatedMessage.DisplayFields = displayFields.ToArray();

            _activeMessages.Add(paginatedMessage);
            
            if (paginatedMessage.TotalPages > 1)
            {
                await context.Interaction.ModifyOriginalResponseAsync(x => x.Components = CreateMessageComponents());
                AddNewTimer(message.Id);
            }
        }

        public void AddNewTimer(ulong messageId)
        {
            var timer = new Timer();
            timer.Interval = 30000;
            timer.Start();
            timer.Elapsed += DisposeActivePaginatorMessage;
            _activeTimers.TryAdd(messageId, timer);
        }

        private async void DisposeActivePaginatorMessage(object timerObj, ElapsedEventArgs e)
        {
            var timer = timerObj as Timer;
            
            var messageId = _activeTimers.First(x => x.Value == timer).Key;
            timer.Enabled = false;

            var paginatorMessage = _activeMessages.First(x => x.DiscordMessageId == messageId);
            
            var channel = await _client.GetChannelAsync(paginatorMessage.DiscordChannelId) as SocketTextChannel;
            var message = await channel.GetMessageAsync(paginatorMessage.DiscordMessageId) as SocketUserMessage;
            if (message is null) return;

            await message.ModifyAsync(x => x.Components = CreateMessageComponents(true));

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

        private async Task OnInteraction(SocketInteraction interaction)
        {
            if (interaction is SocketMessageComponent component)
            {
                // Not our message to handle
                if (_activeMessages.FirstOrDefault(x => x.DiscordMessageId == component.Message.Id) == null)
                {
                    return;
                }

                await component.DeferAsync();

                switch (component.Data.CustomId)
                {
                    case "first":
                        await GoToFirstPageAsync(component.Message.Id);
                        break;
                    case "previous":
                        await GoToPreviousPageAsync(component.Message.Id);
                        break;
                    case "next":
                        await GoToNextPageAsync(component.Message.Id);
                        break;
                    case "last":
                        await GoToLastPageAsync(component.Message.Id);
                        break;
                }

                ResetTimer(component.Message.Id);
            }
        }

        private MessageComponent CreateMessageComponents(bool disabled = false)
        {
            var builder = new ComponentBuilder()
                .WithButton("First", "first", ButtonStyle.Secondary, new Emoji("⏮️"), disabled: disabled)
                .WithButton("Previous", "previous", ButtonStyle.Secondary, new Emoji("⬅️"), disabled: disabled)
                .WithButton("Next", "next", ButtonStyle.Secondary, new Emoji("➡️"), disabled: disabled)
                .WithButton("Last", "last", ButtonStyle.Secondary, new Emoji("⏭️"), disabled: disabled);

            return builder.Build();
        }

        private async Task HandleUpdateMessagePagesAsync(PaginatedMessage msg)
        {
            var message = await (_client.GetChannel(msg.DiscordChannelId) as SocketTextChannel).GetMessageAsync(msg.DiscordMessageId) as SocketUserMessage;
            var oldEmbed = message.Embeds.First();
            
            var newEmbed = new EmbedBuilder()
            {
                Title = $"{oldEmbed.Title.Split('(').First()}(Page {msg.CurrentPage})",
                Color = oldEmbed.Color,
                Url = oldEmbed.Url
            };

            if (oldEmbed.Footer != null)
            {
                newEmbed.WithFooter(oldEmbed.Footer.Value.Text, oldEmbed.Footer.Value.IconUrl);
            }

            if (msg.DisplayMessages != null)
            {
                // get correct messages to display
                var displayMessages = msg.DisplayMessages.Skip((msg.CurrentPage - 1) * 10).Take(10);
                
                newEmbed.Description = string.Join("\n", displayMessages);
            }
            else
            {
                var displayFields = msg.DisplayFields.Skip((msg.CurrentPage - 1) * 10).Take(10).ToArray();

                for (int i = 0; i < displayFields.Length; i++)
                {
                    newEmbed.AddField(displayFields[i].Name, displayFields[i].Value, displayFields[i].Inline);
                }
            }

            await message.ModifyAsync(x => x.Embed = newEmbed.Build());
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

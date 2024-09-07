using System;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services;
using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules
{
    [Group("channels", "All commands regarding channel counters")]
    [RequireCustomPermission(GuildPermission.ManageChannels)]
    public class ChannelInfoModule : TopModule
    {
        private readonly ChannelCounterService _counter;

        public ChannelInfoModule(ChannelCounterService counter)
        {
            _counter = counter;
        }

    [SlashCommand("setup", "Set up the channel counter")]
    [RequireBotPermission(GuildPermission.ManageChannels)]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
    public async Task SetupChannelCountersAsync()
        {
            var voiceChannel = await Context.Guild.CreateVoiceChannelAsync($"Server Status");
            await voiceChannel.AddPermissionOverwriteAsync(Context.Client.CurrentUser, new OverwritePermissions(connect: PermValue.Allow));
            await voiceChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(connect: PermValue.Deny));

            try
            {
                var counter = new ChannelCounter(voiceChannel.Id, ChannelCounterType.FlashStatus);

                _counter.AddNewChannelCounter(Context.Guild.Id, counter);
                await _counter.UpdateChannelAsync(Accounts.GetOrCreateAccount(Context.Guild.Id), counter);

                await ModifyOriginalResponseAsync(msg => msg.Content = $"Setup the Server Status Voice Channel {voiceChannel.Mention}");
            }
            catch (Exception e)
            {
                await voiceChannel.DeleteAsync();

                var embed = new EmbedBuilder()
                    .WithTitle("Failed to setup!")
                    .WithDescription(e.Message)
                    .Build();

                await ModifyOriginalResponseAsync(msg => msg.Embeds = new Embed[] { embed });
            }
        }
    }
}

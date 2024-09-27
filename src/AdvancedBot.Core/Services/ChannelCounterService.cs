using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using AdvancedBot.Core.Entities;
using AdvancedBot.Core.Entities.Enums;
using AdvancedBot.Core.Services.DataStorage;
using Discord;
using Discord.WebSocket;
using GL.NET;

namespace AdvancedBot.Core.Services
{
    public class ChannelCounterService
    {
        private readonly List<ChannelCounterInfo> activeCounters = new();
        private readonly DiscordSocketClient client;
        private readonly GLClient gl;
        private readonly AccountService guild;
        private readonly Timer timer = new(6 * 60 * 1000);
        private string serverStatus = "Offline";

        public ChannelCounterService(DiscordSocketClient client, GLClient gl, AccountService guild)
        {
            this.client = client;
            this.gl = gl;
            this.guild = guild;

            timer.Start();
            timer.Elapsed += onTimerElapsed;

            onTimerElapsed(null, null);
            InitializeCounters();
        }

        private async void onTimerElapsed(object timerObj, ElapsedEventArgs e)
        {
            timer.Stop();

            try
            {
                await handleActiveChannelCounters();
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
            }

            timer.Start();
        }

        public void InitializeCounters()
        {
            var enumValues = Enum.GetValues(typeof(ChannelCounterType)) as ChannelCounterType[];

            for (int i = 0; i < enumValues.Length; i++)
            {
                activeCounters.Add(new ChannelCounterInfo(enumValues[i]));
            }
        }

        public ChannelCounterInfo[] GetAllChannelCounters()
            => activeCounters.ToArray();

        public void AddNewChannelCounter(ulong guildId, ChannelCounter counter)
        {
            var guild = this.guild.GetOrCreateAccount(guildId, true);
            var fCounter = guild.ChannelCounters.Find(x => x.Type == counter.Type);
            var cCounter = guild.ChannelCounters.Find(x => x.ChannelId == counter.ChannelId);

            if (fCounter != null)
                throw new Exception($"A counter of this type already exists. (channel id: {fCounter.ChannelId})");
            else if (cCounter != null)
                throw new Exception($"This channel already has the '{cCounter.Type}' active.");

            guild.ChannelCounters.Add(counter);
            this.guild.SaveAccount(guild);
        }

        public void RemoveChannelCounterByType(ulong guildId, ChannelCounterType counterType)
        {
            var guild = this.guild.GetOrCreateAccount(guildId);

            var counter = guild.ChannelCounters.Find(x => x.Type == counterType) ?? throw new Exception($"There is no counter active of type '{counterType}'.");
            guild.ChannelCounters.Remove(counter);

            this.guild.SaveAccount(guild);
        }

        public void RemoveChannelCounterByChannel(ulong guildId, ulong channelId)
        {
            var guild = this.guild.GetOrCreateAccount(guildId);

            var counter = guild.ChannelCounters.Find(x => x.ChannelId == channelId) ?? throw new Exception($"This channel has no active counter.");
            guild.ChannelCounters.Remove(counter);

            this.guild.SaveAccount(guild);
        }

        public async Task UpdateChannelAsync(Account account, ChannelCounter counter)
        {
            /* Channel got removed */
            if (await client.GetChannelAsync(counter.ChannelId) is not IVoiceChannel channel)
            {
                account.ChannelCounters.Remove(counter);
                guild.SaveAccount(account);
                return;
            }

            switch (counter.Type)
            {
                case ChannelCounterType.FlashStatus:
                    string newName = $"Server Status: {serverStatus}";
                    if (channel.Name != newName)
                        await channel.ModifyAsync(x => x.Name = newName);
                    break;
                default:
                    break;
            }
        }

        private async Task handleActiveChannelCounters()
        {
            Console.WriteLine($"Started handling all counters");
            var start = DateTime.UtcNow;

            var guilds = guild.GetManyAccounts(x => x.IsGuild);
            await updateServerInfo();

            for (int i = 0; i < guilds.Length; i++)
            {
                if (guilds[i].ChannelCounters.Count == 0)
                    continue;

                for (int j = 0; j < guilds[i].ChannelCounters.Count; j++)
                {
                    await UpdateChannelAsync(guilds[i], guilds[i].ChannelCounters[j]);
                }
            }

            Console.WriteLine($"Finished updating all counters ({(DateTime.UtcNow - start).TotalSeconds}s)");
        }

        private async Task updateServerInfo()
        {
            try
            {
                var status = await gl.Api.GetServerStatus();
                var authStatus = status.Find(x => x.Name == "Auth Server");

                if (authStatus == null || !authStatus.IsOnline)
                {
                    serverStatus = "Auth server down";
                    return;
                }

                int onlineBackends = status.Count(x => x.Name.Contains("Backend") && x.IsOnline);

                if (onlineBackends >= 3)
                {
                    serverStatus = "Online";
                    return;
                }

                serverStatus = "Offline";
            }
            catch (Exception)
            {
                serverStatus = "Offline";
            }
        }
    }
}

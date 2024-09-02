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
        private readonly List<ChannelCounterInfo> _activeCounters = new();
        private readonly DiscordSocketClient _client;
        private readonly GLClient _gl;
        private readonly AccountService _guild;
        private readonly Timer _timer = new(6 * 60 * 1000);
        private string _serverStatus = "Offline";

        public ChannelCounterService(DiscordSocketClient client, GLClient gl, AccountService guild)
        {
            _client = client;
            _gl = gl;
            _guild = guild;

            _timer.Start();
            _timer.Elapsed += OnTimerElapsed;

            OnTimerElapsed(null, null);
            InitializeCounters();
        }

        private async void OnTimerElapsed(object timerObj, ElapsedEventArgs e)
        {
            _timer.Stop();

            try
            {
                await HandleActiveChannelCounters();
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
            }

            _timer.Start();
        }

        public void InitializeCounters()
        {
            var enumValues = Enum.GetValues(typeof(ChannelCounterType)) as ChannelCounterType[];

            for (int i = 0; i < enumValues.Length; i++)
            {
                _activeCounters.Add(new ChannelCounterInfo(enumValues[i]));
            }
        }

        public ChannelCounterInfo[] GetAllChannelCounters()
            => _activeCounters.ToArray();

        public void AddNewChannelCounter(ulong guildId, ChannelCounter counter)
        {
            var guild = _guild.GetOrCreateAccount(guildId, true);
            var fCounter = guild.ChannelCounters.Find(x => x.Type == counter.Type);
            var cCounter = guild.ChannelCounters.Find(x => x.ChannelId == counter.ChannelId);

            if (fCounter != null)
                throw new Exception($"A counter of this type already exists. (channel id: {fCounter.ChannelId})");
            else if (cCounter != null)
                throw new Exception($"This channel already has the '{cCounter.Type}' active.");

            guild.ChannelCounters.Add(counter);
            _guild.SaveAccount(guild);
        }

        public void RemoveChannelCounterByType(ulong guildId, ChannelCounterType counterType)
        {
            var guild = _guild.GetOrCreateAccount(guildId);

            var counter = guild.ChannelCounters.Find(x => x.Type == counterType);

            if (counter is null)
                throw new Exception($"There is no counter active of type '{counterType}'.");
            
            guild.ChannelCounters.Remove(counter);

            _guild.SaveAccount(guild);
        }

        public void RemoveChannelCounterByChannel(ulong guildId, ulong channelId)
        {
            var guild = _guild.GetOrCreateAccount(guildId);

            var counter = guild.ChannelCounters.Find(x => x.ChannelId == channelId);

            if (counter is null)
                throw new Exception($"This channel has no active counter.");
            
            guild.ChannelCounters.Remove(counter);

            _guild.SaveAccount(guild);
        }

        public async Task UpdateChannelAsync(Account account, ChannelCounter counter)
        {
            var channel = await _client.GetChannelAsync(counter.ChannelId) as IVoiceChannel;

            /* Channel got removed */
            if (channel == null)
            {
                account.ChannelCounters.Remove(counter);
                _guild.SaveAccount(account);
                return;
            }

            switch (counter.Type)
            {
                case ChannelCounterType.FlashStatus:
                    string newName = $"Server Status: {_serverStatus}";
                    if (channel.Name != newName)
                        await channel.ModifyAsync(x => x.Name = newName);
                    break;
                default:
                    break;
            }
        }

        private async Task HandleActiveChannelCounters()
        {
            Console.WriteLine($"Started handling all counters");
            var start = DateTime.UtcNow;

            var guilds = _guild.GetManyAccounts(x => x.IsGuild);
            await UpdateServerInfo();

            for (int i = 0; i < guilds.Length; i++)
            {
                if (!guilds[i].ChannelCounters.Any())
                    continue;

                for (int j = 0; j < guilds[i].ChannelCounters.Count; j++)
                {
                    await UpdateChannelAsync(guilds[i], guilds[i].ChannelCounters[j]);
                }
            }
            
            Console.WriteLine($"Finished updating all counters ({(DateTime.UtcNow - start).TotalSeconds}s)");
        }

        private async Task UpdateServerInfo()
        {
            try
            {
                var status = await _gl.Api.GetServerStatus();
                var authStatus = status.Find(x => x.Name == "Auth Server");

                if (authStatus == null || !authStatus.IsOnline)
                {
                    _serverStatus = "Auth server down";
                    return;
                }

                var onlineBackends = status.Count(x => x.Name.Contains("Backend") && x.IsOnline);

                if (onlineBackends >= 3)
                {
                    _serverStatus = "Online";
                    return;
                }

                _serverStatus = "Offline";
            }
            catch (Exception)
            {
                _serverStatus = "Offline";
            }
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using AdvancedBot.Core.Commands.Preconditions;
using Discord;

namespace AdvancedBot.Core.Commands.Modules
{
    [RequireCustomPermission(GuildPermission.ManageChannels)]
    [Group("command")][Alias("c", "cmd")]
    [Summary("Handles all commands regarding command permissions.")]
    public class CommandPermissionsModule : TopModule
    {        
        [Command("enable")]
        [Summary("Enables a command.")]
        public async Task EnableCommandAsync([Remainder]string commandName)
        {
            var command = Commands.GetCommandInfo(commandName);

            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            var formattedName = FormatCommandName(command);

            guild.EnableCommand(formattedName);
           
            Accounts.SaveGuildAccount(guild);
            await ReplyAsync($"Successfully enabled `{formattedName}`.");
        }

        [Command("disable")]
        [Summary("Disables a command.")]
        public async Task DisableCommandAsync([Remainder]string commandName)
        {
            var command = Commands.GetCommandInfo(commandName);

            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            var formattedName = FormatCommandName(command);

            guild.DisableCommand(formattedName);
           
            Accounts.SaveGuildAccount(guild);
            await ReplyAsync($"Successfully disable `{formattedName}`.");
        }

        [Command("modrole")]
        [Summary("Sets the modrole to a certain role.")]
        public async Task SetModRoleAsync(SocketRole role)
        {
            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            guild.SetModRole(role.Id);
            Accounts.SaveGuildAccount(guild);

            await ReplyAsync($"Modrole has successfully been changed to `{role.Name}`\n" +
                            $"**{role.Members.Count()}** users can now access all enabled commands anywhere.");
        }

        [Command("modrole")]
        [Summary("Clears the modrole.")]
        public async Task SetModRoleAsync()
        {
            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
            guild.ModRoleId = 0;
            Accounts.SaveGuildAccount(guild);

            await ReplyAsync($"Successfully cleared the modrole.");
        }

        [Group("roles")]
        [Summary("Handle all permissions related to roles.")]
        public class RolesSubmodule : CommandPermissionsModule
        {
            [Command][Alias("list")][Name("")][Priority(0)]
            [Summary("Displays the status of roles for a certain command.")]
            public async Task DisplayRolesCommandStatusAsync([Remainder]string commandName)
            {
                var command = Commands.GetCommandInfo(commandName);
                var formattedName = FormatCommandName(command);

                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

                var cmd = guild.Commands.Find(x => x.Name == formattedName);
                var roleList = cmd.WhitelistedRoles.Count == 0 
                            ? $"No roles have been put on the list."
                            : $"**Roles:**<#{string.Join("> <#", cmd.WhitelistedRoles)}>";

                await ReplyAsync($"**Info for {cmd.Name} regarding roles.**\n" + 
                                $"Blacklist enabled: `{cmd.RolesListIsBlacklist}`.\n" +
                                $"{roleList}");
            }

            [Command("enable")][Priority(1)]
            [Summary("Enables whitelist and disables blacklist for roles for a certain command.")]
            public async Task EnableWhitelistAsync([Remainder]string commandName)
            {
                var command = Commands.GetCommandInfo(commandName);
                var formattedName = FormatCommandName(command);

                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
                guild.EnableWhitelist(formattedName, false);
                Accounts.SaveGuildAccount(guild);

                await ReplyAsync($"Successfully enabled roles whitelist for `{formattedName}`.");
            }

            [Command("disable")][Priority(1)]
            [Summary("Disables whitelist and enables blacklist for roles for a certain command.")]
            public async Task EnableBlacklistAsync([Remainder]string commandName)
            {
                var command = Commands.GetCommandInfo(commandName);
                var formattedName = FormatCommandName(command);

                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
                guild.DisableWhitelist(formattedName, false);

                Accounts.SaveGuildAccount(guild);
                await ReplyAsync($"Successfully disabled roles whitelist for `{formattedName}`.");
            }

            [Command("add")][Priority(1)]
            [Summary("Adds a role to the white/blacklist.")]
            public async Task AddChannelToListAsync(SocketRole role, [Remainder]string commandName)
            {
                var command = Commands.GetCommandInfo(commandName);
                var formattedName = FormatCommandName(command);

                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
                guild.AddToWhitelist(formattedName, role.Id, false);
                Accounts.SaveGuildAccount(guild);

                await ReplyAsync($"Succesfully added {role.Mention} to the list.");
            }

            [Command("remove")][Priority(1)]
            [Summary("Removes a role from the white/blacklist")]
            public async Task RemoveChannelFromListAsync(SocketRole role, [Remainder]string commandName)
            {
                var command = Commands.GetCommandInfo(commandName);
                var formattedName = FormatCommandName(command);

                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
                guild.RemoveFromWhitelist(formattedName, role.Id, false);
                Accounts.SaveGuildAccount(guild);

                await ReplyAsync($"Succesfully removed {role.Mention} from the list.");
            }
        }
    
        [Group("channels")]
        [Summary("Handle all permissions related to channels.")]
        public class ChannelPermissionsSubmodule : CommandPermissionsModule
        {
            [Command][Alias("list")][Name("")][Priority(0)]
            [Summary("Displays the status of channels for a certain command.")]
            public async Task DisplayChannelsCommandStatusAsync([Remainder]string commandName)
            {
                var command = Commands.GetCommandInfo(commandName);
                var formattedName = FormatCommandName(command);

                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

                var cmd = guild.Commands.Find(x => x.Name == formattedName);
                var channelList = cmd.WhitelistedRoles.Count == 0 
                            ? $"No channels have been put on the list."
                            : $"**Channels:**<#{string.Join("> <#", cmd.WhitelistedChannels)}>";

                await ReplyAsync($"**Info for {cmd.Name} regarding channels.**\n" + 
                                $"Blacklist enabled: `{cmd.ChannelListIsBlacklist}`.\n" +
                                $"{channelList}");
            }

            [Command("enable")][Priority(1)]
            [Summary("Enables whitelist and disables blacklist for channels for a certain command.")]
            public async Task EnableWhitelistAsync([Remainder]string commandName)
            {
                var command = Commands.GetCommandInfo(commandName);
                var formattedName = FormatCommandName(command);

                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
                guild.EnableWhitelist(formattedName, true);
                Accounts.SaveGuildAccount(guild);

                await ReplyAsync($"Successfully enabled channels whitelist for `{formattedName}`.");
            }

            [Command("disable")][Priority(1)]
            [Summary("Disables whitelist and enables blacklist for said command.")]
            public async Task EnableBlacklistAsync([Remainder]string commandName)
            {
                var command = Commands.GetCommandInfo(commandName);
                var formattedName = FormatCommandName(command);

                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
                guild.DisableWhitelist(formattedName, true);

                Accounts.SaveGuildAccount(guild);
                await ReplyAsync($"Successfully disabled channels whitelist for `{formattedName}`.");
            }

            [Command("add")][Priority(1)]
            [Summary("Adds a channel to the white/blacklist.")]
            public async Task AddChannelToListAsync(SocketTextChannel channel, [Remainder]string commandName)
            {
                var command = Commands.GetCommandInfo(commandName);
                var formattedName = FormatCommandName(command);

                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
                guild.AddToWhitelist(formattedName, channel.Id, true);
                Accounts.SaveGuildAccount(guild);

                await ReplyAsync($"Succesfully added {channel.Mention} to the list.");
            }

            [Command("remove")][Priority(1)]
            [Summary("Removes a channel from the white/blacklist")]
            public async Task RemoveChannelFromListAsync(SocketTextChannel channel, [Remainder]string commandName)
            {
                var command = Commands.GetCommandInfo(commandName);
                var formattedName = FormatCommandName(command);

                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);
                guild.RemoveFromWhitelist(formattedName, channel.Id, true);
                Accounts.SaveGuildAccount(guild);

                await ReplyAsync($"Succesfully removed {channel.Mention} from the list.");
            }
        }
    }
}

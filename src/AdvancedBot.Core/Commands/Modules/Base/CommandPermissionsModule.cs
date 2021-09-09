using System.Linq;
using System.Threading.Tasks;
using AdvancedBot.Core.Commands.Preconditions;
using AdvancedBot.Core.Services.Commands;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace AdvancedBot.Core.Commands.Modules.Base
{
    [RequireCustomPermission(GuildPermission.ManageChannels)]
    [Group("command")][Alias("c", "cmd")]
    [Summary("Handles all commands regarding command permissions.")]
    public class CommandPermissionsModule : TopModule
    {
        public CommandPermissionService Permissions { get; set; }

        [Command("enable")]
        [Summary("Enables a command or a category.")]
        public async Task EnableCommandOrModuleAsync([Remainder]string input)
        {
            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

            Permissions.EnableGuildCommandOrModule(guild, input);
  
            Accounts.SaveGuildAccount(guild);
            await ReplyAsync($"Successfully enabled all commands associated with `{input}`");
        }

        [Command("disable")]
        [Summary("Disables a command or module.")]
        public async Task DisableCommandAsync([Remainder]string input)
        {
            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

            Permissions.DisableGuildCommandOrModule(guild, input);
           
            Accounts.SaveGuildAccount(guild);
            await ReplyAsync($"Successfully disabled all commands associated with `{input}`.");
        }

        [Command("delete")]
        [Summary("Toggles whether the original message should be deleted after the command ran.")]
        public async Task ToggleDeleteMessageAsync([Remainder]string input)
        {
            var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

            Permissions.ToggleDeleteMessageForCommandOrModule(guild, input);

            Accounts.SaveGuildAccount(guild);
            await ReplyAsync($"Successfully toggled message-delete on all commands associated with `{input}`.");
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
                            : $"**Roles:** <#{string.Join("> <#", cmd.WhitelistedRoles)}>";

                await ReplyAsync($"**Info for {cmd.Name} regarding roles.**\n" + 
                                $"Blacklist enabled: `{cmd.RolesListIsBlacklist}`.\n" +
                                $"{roleList}");
            }

            [Command("enable")][Alias("whitelist")][Priority(1)]
            [Summary("Enables whitelist and disables blacklist for roles for a certain command.")]
            public async Task EnableWhitelistAsync([Remainder] string input)
            {
                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

                Permissions.EnableWhitelistForCommandOrModule(guild, input, false);

                Accounts.SaveGuildAccount(guild);
                await ReplyAsync($"Successfully enabled roles whitelist for `{input}`.");
            }

            [Command("disable")][Alias("blacklist")][Priority(1)]
            [Summary("Disables whitelist and enables blacklist for roles for a certain command.")]
            public async Task EnableBlacklistAsync([Remainder] string input)
            {
                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

                Permissions.DisableWhitelistForCommandOrModule(guild, input, false);

                Accounts.SaveGuildAccount(guild);
                await ReplyAsync($"Successfully enabled roles blacklist for `{input}`.");
            }

            [Command("add")][Priority(1)]
            [Summary("Adds a role to the white/blacklist.")]
            public async Task AddChannelToListAsync(SocketRole role, [Remainder] string input)
            {
                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

                Permissions.AddIdToWhitelistForCommandOrModule(guild, input, role.Id, false);

                Accounts.SaveGuildAccount(guild);
                await ReplyAsync("", false, new EmbedBuilder()
                {
                    Description = $"Succesfully added {role.Mention} to the list of {input}."
                }.Build());
            }

            [Command("remove")][Priority(1)]
            [Summary("Removes a role from the white/blacklist")]
            public async Task RemoveChannelFromListAsync(SocketRole role, [Remainder] string input)
            {
                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

                Permissions.RemoveIdFromWhitelistForCommandOrModule(guild, input, role.Id, false);

                Accounts.SaveGuildAccount(guild);
                await ReplyAsync("", false, new EmbedBuilder()
                {
                    Description = $"Succesfully removed {role.Mention} from the list of {input}."
                }.Build());
            }
        }
    
        [Group("channels")]
        [Summary("Handle all permissions related to channels.")]
        public class ChannelPermissionsSubmodule : CommandPermissionsModule
        {
            [Command][Alias("list")][Name("")][Priority(0)]
            [Summary("Displays the status of channels for a certain command.")]
            public async Task DisplayChannelsCommandStatusAsync([Remainder] string commandName)
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

            [Command("enable")][Alias("whitelist")][Priority(1)]
            [Summary("Enables whitelist and disables blacklist for channels for a certain command.")]
            public async Task EnableWhitelistAsync([Remainder]string input)
            {
                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

                Permissions.EnableWhitelistForCommandOrModule(guild, input, true);

                Accounts.SaveGuildAccount(guild);
                await ReplyAsync($"Successfully enabled channels whitelist for `{input}`.");
            }

            [Command("disable")][Alias("blacklist")][Priority(1)]
            [Summary("Disables whitelist and enables blacklist for said command.")]
            public async Task EnableBlacklistAsync([Remainder]string input)
            {
                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

                Permissions.DisableWhitelistForCommandOrModule(guild, input, true);

                Accounts.SaveGuildAccount(guild);
                await ReplyAsync($"Successfully enabled channels blacklist for `{input}`.");
            }

            [Command("add")][Priority(1)]
            [Summary("Adds a channel to the white/blacklist.")]
            public async Task AddChannelToListAsync(SocketTextChannel channel, [Remainder] string input)
            {
                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

                Permissions.AddIdToWhitelistForCommandOrModule(guild, input, channel.Id, true);

                Accounts.SaveGuildAccount(guild);
                await ReplyAsync("", false, new EmbedBuilder()
                {
                    Description = $"Succesfully added {channel.Mention} to the list for {input}."
                }.Build());
            }

            [Command("remove")][Priority(1)]
            [Summary("Removes a channel from the white/blacklist")]
            public async Task RemoveChannelFromListAsync(SocketTextChannel channel, [Remainder] string input)
            {
                var guild = Accounts.GetOrCreateGuildAccount(Context.Guild.Id);

                Permissions.AddIdToWhitelistForCommandOrModule(guild, input, channel.Id, true);

                Accounts.SaveGuildAccount(guild);
                await ReplyAsync("", false, new EmbedBuilder()
                {
                    Description = $"Succesfully removed {channel.Mention} from the list for {input}."
                }.Build());
            }
        }
    }
}

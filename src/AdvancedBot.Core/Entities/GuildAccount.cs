using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Commands;

namespace AdvancedBot.Core.Entities
{
    public class GuildAccount
    {
        #region Properties
        public ulong Id { get; set; }
        public List<string> Prefixes { get; set; }
        public ulong ModRoleId { get; set; }
        public List<CommandSettings> Commands { get; set; }

        #endregion

        public GuildAccount()
        {
            Prefixes = new List<string>() { "!" };
        }

        public void RemovePrefix(string prefix)
        {
            if (!Prefixes.Contains(prefix)) throw new Exception("This is not a prefix in this server.");
            else Prefixes.Remove(prefix);
        }

        public void AddPrefix(string prefix)
        {           
            if (Prefixes.Contains(prefix)) throw new Exception("Prefix already exists.");
            else Prefixes.Add(prefix);
        }

        public void AddToWhitelist(string name, ulong id, bool isChannel)
        {
            var command = Commands.Where(x => x.Name == name.ToLower()).FirstOrDefault();

            if (command is null) throw new Exception("Command not found");
            if (command.WhitelistedChannels.Contains(id)) throw new Exception("Channel/role already on the list.");

            if (isChannel) command.WhitelistedChannels.Add(id);
            else command.WhitelistedRoles.Add(id);
        }

        public void AddNewCommand(CommandInfo command)
            => Commands.Add(new CommandSettings()
            {
                Name = $"{command.Module.Name}_{command.Name}".ToLower(),
                IsEnabled = true,
                ChannelListIsBlacklist = true,
                RolesListIsBlacklist = true,
                WhitelistedChannels = new List<ulong>(),
                WhitelistedRoles = new List<ulong>()
            });

        public void RemoveFromWhitelist(string name, ulong id, bool isChannel)
        {
            var command = Commands.Where(x => x.Name == name.ToLower()).FirstOrDefault();

            if (command is null) throw new Exception("Command not found");

            if (!command.WhitelistedChannels.Contains(id) && !command.WhitelistedRoles.Contains(id))
                throw new Exception("Channel or role is not on the list.");

            if (isChannel) command.WhitelistedChannels.Remove(id);
            else command.WhitelistedRoles.Remove(id);
        }
    
        public void EnableWhitelist(string name, bool isChannel)
        {
            var command = Commands.Find(x => x.Name == name);
            Commands.Remove(command);

            if (isChannel) command.ChannelListIsBlacklist = false;
            else command.RolesListIsBlacklist = false;

            Commands.Add(command);
        }

        public void DisableWhitelist(string name, bool isChannel)
        {
            var command = Commands.Find(x => x.Name == name);
            Commands.Remove(command);

            if (isChannel) command.ChannelListIsBlacklist = true;
            else command.RolesListIsBlacklist = true;

            Commands.Add(command);
        }

        public void EnableCommand(string name) 
        {
            var command = Commands.Find(x => x.Name == name);
            command.IsEnabled = true;
        }

        public void DisableCommand(string name)
        {
            var command = Commands.Find(x => x.Name == name);
            command.IsEnabled = false;
        }

        public void SetModRole(ulong id)
            => ModRoleId = id;
    }
}

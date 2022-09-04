using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace AdvancedBot.Core.Commands.TypeReaders
{
    public class IUserTypeReader : TypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            /* No need to check if context is in guild, because it always is (see CommandHandler) */
            var restClient = (context.Client as DiscordSocketClient).Rest;

            /* By mention or by id */
            if (MentionUtils.TryParseUser(input, out ulong userId) || ulong.TryParse(input, out userId))
            {
                IUser user = await context.Guild.GetUserAsync(userId);
                user = user ?? await restClient.GetUserAsync(userId);

                return TypeReaderResult.FromSuccess(user);
            }
            
            /* By Username */
            var channelUsers = context.Channel.GetUsersAsync(CacheMode.CacheOnly).Flatten();
            var channelUser = await channelUsers.FirstOrDefaultAsync(x => string.Equals(input, x.Username, StringComparison.OrdinalIgnoreCase));

            if (channelUser != null)
            {
                return TypeReaderResult.FromSuccess(channelUser);
            }

            var guildUsers = await context.Guild.GetUsersAsync(CacheMode.CacheOnly).ConfigureAwait(false);
            var guildUser = guildUsers.FirstOrDefault(x => string.Equals(input, x.Nickname, StringComparison.OrdinalIgnoreCase));

            if (guildUser != null)
            {
                return TypeReaderResult.FromSuccess(guildUser.Id);
            }

            /* By Nickname */
            var cUser = await channelUsers.FirstOrDefaultAsync(x => string.Equals(input, (x as IGuildUser)?.Nickname, StringComparison.OrdinalIgnoreCase));
            
            if (cUser != null)
            {
                return TypeReaderResult.FromSuccess(cUser);
            }

            var gUser = guildUsers.FirstOrDefault(x => string.Equals(input, x.Nickname, StringComparison.OrdinalIgnoreCase));

            if (gUser != null)
            {
                return TypeReaderResult.FromSuccess(gUser);
            }

            return TypeReaderResult.FromError(CommandError.ObjectNotFound, $"User not found.");
        }
    }
}

using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;

namespace AdvancedBot.Core.Extensions
{
    public static class PrefixCheckerExt
    {
        public static bool HasPrefix(this SocketUserMessage message, DiscordSocketClient client, out int argPos,
            List<string> prefixes)
        {
            int prefixStart = 0;
            for (int i = 0; i < prefixes.Count; i++)
            {
                argPos = prefixes[i].Length;
                if (message.HasStringPrefix(prefixes[i], ref prefixStart)) { return true; }
            }

            prefixStart = 0;
            argPos = client.CurrentUser.Mention.Length + 1;
            return message.HasMentionPrefix(client.CurrentUser, ref prefixStart);
        }

        public static bool HasPrefix(this SocketUserMessage message, DiscordSocketClient client, out int argPos,
            string prefix)
        {
            int prefixStart = 0;

            argPos = prefix.Length;
            if (message.HasStringPrefix(prefix, ref prefixStart)) { return true; }

            prefixStart = 0;
            argPos = client.CurrentUser.Mention.Length + 1;
            return message.HasMentionPrefix(client.CurrentUser, ref prefixStart);
        }
    }
}

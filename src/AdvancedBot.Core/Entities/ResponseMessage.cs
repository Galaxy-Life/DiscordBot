using Discord;

namespace AdvancedBot.Core.Entities
{
    public class ResponseMessage
    {
        public ResponseMessage(string content = "", Embed[] embeds = null, bool ephemeral = false)
        {
            Content = content;
            Embeds = embeds;
            Ephemeral = ephemeral;
        }

        public string Content { get; set; }
        public Embed[] Embeds { get; set; }
        public bool Ephemeral { get; set; }
    }
}

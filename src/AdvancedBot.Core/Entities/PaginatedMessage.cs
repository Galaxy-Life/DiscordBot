using System;
using Discord;

namespace AdvancedBot.Core.Entities
{
    public class PaginatedMessage
    {
        private int currentPage = 1;

        public ulong DiscordMessageId { get; set; }
        public ulong DiscordChannelId { get; set; }
        public ulong DiscordUserId { get; set; }
        public string[] DisplayMessages { get; set; }
        public EmbedField[] DisplayFields { get; set; }
        public int CurrentPage
        {
            get => currentPage;
            set
            {
                if (value > TotalPages) currentPage = TotalPages;
                else if (value < 1) currentPage = 1;
                else currentPage = value;
            }
        }
        public int TotalPages
        {
            get
            {
                int totalPages = DisplayMessages == null ? DisplayFields.Length : DisplayMessages.Length;
                double decimalValue = (double)totalPages / 10;
                double roundedUpValue = Math.Ceiling(decimalValue);
                return int.Parse(roundedUpValue.ToString());
            }
        }
    }
}

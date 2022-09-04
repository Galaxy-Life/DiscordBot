using System;
using Discord;

namespace AdvancedBot.Core.Entities
{
    public class PaginatedMessage
    {
        private int _currentPage = 1;

        public ulong DiscordMessageId { get; set; }
        public ulong DiscordChannelId { get; set; }
        public ulong DiscordUserId { get; set; }
        public string[] DisplayMessages { get; set; }
        public EmbedField[] DisplayFields { get; set; }
        public int CurrentPage 
        {
            get => _currentPage;
            set
            {
                if (value > TotalPages) _currentPage = TotalPages;
                else if (value < 1) _currentPage = 1;
                else _currentPage = value;
            }
        }
        public int TotalPages
        {
            get
            {
                var totalPages = DisplayMessages == null ? DisplayFields.Length : DisplayMessages.Length;
                var decimalValue = (double) totalPages / 10;
                var roundedUpValue = Math.Ceiling(decimalValue);
                return int.Parse(roundedUpValue.ToString());
            }
        }
    }
}

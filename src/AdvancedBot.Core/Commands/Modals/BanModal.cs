using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules
{
    public class BanModal : IModal
    {
        public string Title => $"Banning User";

        [InputLabel("Ban Reason:")]
        [ModalTextInput("ban_reason", TextInputStyle.Paragraph, "L bozo")]
        public string BanReason { get; set; }

        [InputLabel("Duration in days: ")]
        [ModalTextInput("duration", TextInputStyle.Short, "14", maxLength: 5)]
        public string Duration { get; set; }
    }
}

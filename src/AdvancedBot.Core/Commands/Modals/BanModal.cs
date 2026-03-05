using AdvancedBot.Core.Entities.Enums;
using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modals;

public class BanModal : IModal
{
    public string Title => $"Banning User";

    [InputLabel("Ban Reason:")]
    [ModalTextInput("ban_reason", TextInputStyle.Paragraph, "L bozo")]
    public string BanReason { get; set; }

    [InputLabel("Ban Type:")]
    [ModalSelectMenu("ban_type")]
    public BanReasonType BanType { get; set; }

    [InputLabel("Duration in days: ")]
    [ModalTextInput("duration", TextInputStyle.Short, "14", maxLength: 5)]
    public string Duration { get; set; }
}

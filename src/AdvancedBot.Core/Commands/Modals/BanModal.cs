using AdvancedBot.Core.Entities.Enums;
using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modals;

public class BanModal : IModal
{
    public string Title => $"Ban user";

    [InputLabel("Reason")]
    [ModalSelectMenu("reason")]
    public BanReasonType BanType { get; set; }

    [RequiredInput(false)]
    [InputLabel("Duration", Description = "Ban duration in days, omit for permanent")]
    [ModalTextInput("duration", TextInputStyle.Short, "14", maxLength: 5)]
    public string? Duration { get; set; }

    [RequiredInput(false)]
    [InputLabel("Additional notes", Description = "Provide additional context about the ban (not shown to the user)")]
    [ModalTextInput("moderator_notes", TextInputStyle.Paragraph, placeholder: "User was caught exceeding star visit rate limits")]
    public string? ModeratorNotes { get; set; }
}

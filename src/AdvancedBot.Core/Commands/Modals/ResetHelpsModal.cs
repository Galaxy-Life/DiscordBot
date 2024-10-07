using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules;

public class ResetHelpsModal : IModal
{
    public string Title => $"Resetting visit helps";

    [InputLabel("User Id:")]
    [ModalTextInput("user_id", TextInputStyle.Paragraph, "12345")]
    public string UserId { get; set; }
}

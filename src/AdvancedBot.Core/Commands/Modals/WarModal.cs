using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules;

public class WarModal : IModal
{
    public string Title => $"Modifying War";

    [InputLabel("1st Alliance:")]
    [ModalTextInput("alliance_one", TextInputStyle.Paragraph, "Pussy Destroyers")]
    public string AllianceA { get; set; }

    [InputLabel("2nd Alliance:")]
    [ModalTextInput("alliance_two", TextInputStyle.Paragraph, "Pussy Destroyers Destroyers")]
    public string AllianceB { get; set; }
}

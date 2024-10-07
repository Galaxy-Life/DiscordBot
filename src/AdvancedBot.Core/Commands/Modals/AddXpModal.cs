using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules;

public class AddXpModal : IModal
{
    public string Title => $"Add xp to user";

    [InputLabel("Xp Amount:")]
    [ModalTextInput("amount", TextInputStyle.Short, "25")]
    public string Amount { get; set; }

    public int ActualAmount => int.Parse(Amount);
}

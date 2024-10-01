using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules;

public class AddChipsModal : IModal
{
    public string Title => $"Add chips to user";

    [InputLabel("Chips Amount:")]
    [ModalTextInput("amount", TextInputStyle.Short, "25")]
    public string Amount { get; set; }

    public int ActualAmount => int.Parse(Amount);
}

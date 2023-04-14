using Discord;
using Discord.Interactions;

namespace AdvancedBot.Core.Commands.Modules
{
    public class AddItemModal : IModal
    {
        public string Title => $"Add item to user";

        [InputLabel("Item Sku:")]
        [ModalTextInput("sku", TextInputStyle.Short, "7000")]
        public string Sku { get; set; }

        [InputLabel("Item Amount:")]
        [ModalTextInput("amount", TextInputStyle.Short, "25")]
        public string Amount { get; set; }

        public int ActualAmount => int.Parse(Amount);
    }
}

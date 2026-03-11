using Discord.Interactions;

namespace AdvancedBot.Core.Entities.Enums;

public enum BanReasonType
{
    [ChoiceDisplay("Cheating")]
    Cheating = 0,

    [ChoiceDisplay("Exploiting (unintended mechanics)")]
    Exploiting = 1,

    [ChoiceDisplay("Game File Modification")]
    GameFilesModification = 2,

    [ChoiceDisplay("Alt Abuse")]
    AltAbuse = 3,

    [ChoiceDisplay("Offensive Layout")]
    OffensiveLayout = 4,

    [ChoiceDisplay("Offensive Username")]
    OffensiveName = 20,

    [ChoiceDisplay("Offensive Avatar")]
    OffensiveAvatar = 21,

    [ChoiceDisplay("Chargeback / Payment Fraud")]
    Chargeback = 40,

    [ChoiceDisplay("Account Sharing")]
    AccountSharing = 60,

    [ChoiceDisplay("Compromised Account")]
    Compromised = 61,

    [ChoiceDisplay("Other")]
    [SelectMenuOption(IsDefault = true)]
    Other = 100
}

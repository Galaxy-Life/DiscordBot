namespace AdvancedBot.Core.Entities.Enums;

public enum BanReasonType
{
    // game violations
    Cheating = 0,
    Exploiting = 1,
    GameFilesModification = 2,
    AltAbuse = 3,
    OffensiveLayout = 4,

    // community violations
    OffensiveName = 20,
    OffensiveAvatar = 21,

    // financial voliations
    Chargeback = 40,

    // security violations
    AccountSharing = 60,
    Compromised = 61,

    // edge cases
    Other = 100
}

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace AdvancedBot.Core.Commands.Preconditions;

public class RequireCustomPermission : PreconditionAttribute
{
    private readonly GuildPermission permission;

    public RequireCustomPermission(GuildPermission permission = default)
    {
        this.permission = permission;
    }

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        if (context.User.Id == (await context.Client.GetApplicationInfoAsync()).Owner.Id)
        {
            return PreconditionResult.FromSuccess();
        }

        var guildUser = context.User as SocketGuildUser;

        if (guildUser.GuildPermissions.Has(permission) || guildUser.GuildPermissions.Has(GuildPermission.Administrator))
        {
            return PreconditionResult.FromSuccess();
        }

        return PreconditionResult.FromError("You do not have enough permissions");
    }
}

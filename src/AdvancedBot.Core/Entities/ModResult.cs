using AdvancedBot.Core.Entities.Enums;
using GL.NET.Entities;

namespace AdvancedBot.Core.Entities;

public class ModResult
{
    public ModResult(ModResultType type, ResponseMessage message = null, PhoenixUser phoenixUser = null, User user = null)
    {
        Type = type;
        Message = message;
        PhoenixUser = phoenixUser;
        User = user;
    }

    public PhoenixUser PhoenixUser { get; set; }
    public User User { get; set; }
    public Alliance Alliance { get; set; }
    public int IntValue { get; set; }
    public string StringValue { get; set; }
    public ModResultType Type { get; set; }
    public ResponseMessage Message { get; set; }
}

public class ModResult<T> : ModResult
{
    public ModResult(T output, ModResultType type, ResponseMessage message = null, PhoenixUser phoenixUser = null, User user = null)
        : base(type, message, phoenixUser, user)
    {
        Output = output;
    }

    public T Output { get; set; }
}

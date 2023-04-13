using AdvancedBot.Core.Entities.Enums;
using GL.NET.Entities;

namespace AdvancedBot.Core.Entities
{
    public class ModResult
    {
        public ModResult(ModResultType type, PhoenixUser phoenixUser = null, User user = null, ResponseMessage message = null)
        {
            Type = type;
            PhoenixUser = phoenixUser;
            User = user;
            Message = message;
        }

        public PhoenixUser PhoenixUser { get; set; }
        public User User { get; set; }
        public Alliance Alliance { get; set; }
        public int IntValue { get; set; }
        public string StringValue { get; set; }
        public ModResultType Type { get; set; }
        public ResponseMessage Message { get; set; }
    }
}

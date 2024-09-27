namespace AdvancedBot.Core.Entities
{
    public class CommandStats
    {
        public CommandStats(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public ulong TimesRun { get; set; }
        public ulong TimesFailed { get; set; }
    }
}

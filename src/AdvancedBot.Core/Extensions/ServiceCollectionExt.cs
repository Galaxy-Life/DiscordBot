using Microsoft.Extensions.DependencyInjection;

namespace AdvancedBot.Core.Extensions
{
    public static class ServiceCollectionExt
    {
        public static IServiceCollection AddBotTypes(this IServiceCollection collection)
        {
            return collection;
        }
    }
}

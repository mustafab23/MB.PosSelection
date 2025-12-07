using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Application.Rules;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MB.PosSelection.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            services.AddTransient<IPosSelectionRule, LowestCostRule>();       // 1. Kural
            services.AddTransient<IPosSelectionRule, HighestPriorityRule>();  // 2. Kural
            services.AddTransient<IPosSelectionRule, LowestCommissionRule>(); // 3. Kural
            services.AddTransient<IPosSelectionRule, PosNameRule>();          // 4. Kural

            return services;
        }
    }
}

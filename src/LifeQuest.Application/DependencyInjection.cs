using System.Globalization;
using FluentValidation;
using Gvn.GvnFramework.Application.Behaviors;
using Gvn.GvnFramework.Application.DependencyInjection;
using LifeQuest.Application.Behaviors;
using LifeQuest.Application.Diagnostics;
using LifeQuest.Application.Identity;
using LifeQuest.Application.Narration;
using LifeQuest.Application.Notifications;
using LifeQuest.Application.Profiles;
using LifeQuest.Application.Quests;
using LifeQuest.Domain.Recommendations;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LifeQuest.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddLifeQuestApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Framework: MediatR handler'ları, FluentValidation validator'ları ve Logging/Validation/Performance pipeline'ı.
        services.AddApplicationServices(typeof(DependencyInjection).Assembly);
        services.ReplaceFrameworkValidationBehavior();

        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("tr");

        services.Configure<QuestOptions>(configuration.GetSection(QuestOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));
        services.Configure<RecommendationWeights>(configuration.GetSection(RecommendationWeights.SectionName));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<LifeQuestMetrics>();
        services.AddScoped<AuthTokenIssuer>();
        services.AddScoped<ProfileService>();
        services.AddScoped<QuestOfferService>();
        services.AddScoped<WeeklySummaryService>();
        services.AddScoped<QuestNarrationService>();
        services.AddScoped<Admin.AdminAuditWriter>();

        // AI Quest Master portu: gerçek bir LLM adaptörü Infrastructure'da kaydedilirse onu kullanır.
        services.TryAddSingleton<IQuestNarrator, TemplateQuestNarrator>();
        services.Configure<NarrationOptions>(configuration.GetSection(NarrationOptions.SectionName));

        return services;
    }

    /// <summary>Framework ValidationBehavior'ı, pipeline sırasını koruyarak düzeltilmiş sürümle değiştirir.</summary>
    private static void ReplaceFrameworkValidationBehavior(this IServiceCollection services)
    {
        var index = services
            .Select((descriptor, i) => (descriptor, i))
            .Single(x => x.descriptor.ServiceType == typeof(IPipelineBehavior<,>) &&
                         x.descriptor.ImplementationType == typeof(ValidationBehavior<,>))
            .i;

        services[index] = ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ResultValidationBehavior<,>));
    }
}

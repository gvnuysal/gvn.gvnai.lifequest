using System.Globalization;
using FluentValidation;
using Gvn.GvnFramework.Application.Configuration;
using Gvn.GvnFramework.Application.DependencyInjection;
using LifeQuest.Application.Diagnostics;
using LifeQuest.Application.Identity;
using LifeQuest.Application.Narration;
using LifeQuest.Application.Notifications;
using LifeQuest.Application.Profiles;
using LifeQuest.Application.Quests;
using LifeQuest.Domain.Recommendations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LifeQuest.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddLifeQuestApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Framework: MediatR handler'ları, FluentValidation validator'ları ve Logging/Validation/Performance pipeline'ı.
        // 1.1.0'dan itibaren ValidationBehavior Result<T> döndürür ve istek/yanıt gövdelerini varsayılan olarak loglamaz.
        services.AddApplicationServices(typeof(DependencyInjection).Assembly);
        services.Configure<PipelineLoggingOptions>(configuration.GetSection(PipelineLoggingOptions.SectionName));
        services.Configure<PerformanceOptions>(configuration.GetSection(PerformanceOptions.SectionName));

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
        services.AddScoped<PushNotifier>();
        services.AddScoped<DailyReminderService>();
        services.AddScoped<QuestNarrationService>();
        services.AddScoped<RealWorld.RealWorldContextService>();
        services.AddScoped<Admin.AdminAuditWriter>();

        // AI Quest Master portu: gerçek bir LLM adaptörü Infrastructure'da kaydedilirse onu kullanır.
        services.TryAddSingleton<IQuestNarrator, TemplateQuestNarrator>();
        services.Configure<NarrationOptions>(configuration.GetSection(NarrationOptions.SectionName));

        return services;
    }
}

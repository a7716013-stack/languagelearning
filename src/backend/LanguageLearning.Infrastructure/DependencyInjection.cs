using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Admin;
using LanguageLearning.Application.Auth;
using LanguageLearning.Application.Users;
using LanguageLearning.Infrastructure.Auditing;
using LanguageLearning.Infrastructure.Email;
using LanguageLearning.Infrastructure.Identity;
using LanguageLearning.Infrastructure.Options;
using LanguageLearning.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LanguageLearning.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AppOptions>()
            .Bind(configuration.GetSection(AppOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<SeedOptions>().Bind(configuration.GetSection(SeedOptions.SectionName));
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Default");
        services.AddDbContext<AppDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "尚未設定資料庫連線字串 ConnectionStrings:Default。請參考 README，以 user-secrets 或環境變數設定。");
            }

            options.UseSqlServer(connectionString, sql =>
            {
                // 對應 SQL Server 2022；避免產生較新版本才支援的 SQL。
                sql.UseCompatibilityLevel(160);
                sql.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        var auth = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                // 使用者名稱即 Email，格式已由驗證器檢查。
                options.User.AllowedUserNameCharacters = string.Empty;

                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = auth.MaxFailedAccessAttempts;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(auth.LockoutMinutes);

                options.SignIn.RequireConfirmedEmail = auth.RequireConfirmedEmail;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<ZhTwIdentityErrorDescriber>();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromHours(auth.TokenLifespanHours));
        services.Configure<SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = TimeSpan.FromMinutes(auth.SecurityStampValidationMinutes));

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IUserTimeZoneResolver, UserTimeZoneResolver>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<DatabaseInitializer>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        return services;
    }
}

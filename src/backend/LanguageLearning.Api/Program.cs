using System.Diagnostics;
using System.Globalization;
using LanguageLearning.Api.Common;
using LanguageLearning.Api.Endpoints;
using LanguageLearning.Api.Security;
using LanguageLearning.Application;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Infrastructure;
using LanguageLearning.Infrastructure.Options;
using LanguageLearning.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// ---- 日誌：Serilog（結構化）。不記錄密碼、Token、Cookie 與完整 Email。----
builder.Logging.ClearProviders();
builder.Services.AddSerilog(
    (services, logger) =>
    {
        logger
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "LanguageLearning.Api");

        if (builder.Environment.IsProduction())
        {
            logger.WriteTo.Console(new RenderedCompactJsonFormatter());
        }
        else
        {
            logger.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
        }
    },
    // 不使用全域 Log.Logger：同一行程內的多個主機（整合測試）才不會互相覆寫日誌管線。
    preserveStaticLogger: true,
    writeToProviders: true);

// ---- 服務 ----
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd(
            "traceId",
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);

        if (context.ProblemDetails.Status is { } status && string.IsNullOrEmpty(context.ProblemDetails.Detail))
        {
            context.ProblemDetails.Title = Problems.TitleFor(status);
        }
    };
});
builder.Services.AddExceptionHandler<ConcurrencyExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAppAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddAppRateLimiting(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: [SystemEndpoints.ReadyTag]);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info ??= new();
        document.Info.Title = "LanguageLearning API";
        document.Info.Version = AppVersion.Version;
        document.Info.Description = "AI 英文檢定學習平台 API。驗證採 Cookie，所有變更狀態的請求需帶 X-XSRF-TOKEN。";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// ---- 資料庫初始化（Development / Testing 可自動套用 Migration；建立設定中的管理者帳號）----
await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync(CancellationToken.None);
}

// ---- 中介軟體 ----
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Configuration.GetSection(AppOptions.SectionName).Get<AppOptions>()?.UseHttpsRedirection ?? true)
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<CsrfProtectionMiddleware>();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapSystemEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapExamEndpoints();
app.MapAdminEndpoints();

await app.RunAsync();

/// <summary>供整合測試（WebApplicationFactory）使用。</summary>
public partial class Program
{
}

namespace LanguageLearning.IntegrationTests.Infrastructure;

/// <summary>每個測試類別一個 API 主機與獨立資料庫（啟動時套用真實 Migration）。</summary>
public class ApiFixture : IAsyncLifetime
{
    public ApiFactory Factory { get; private set; } = null!;

    protected virtual IReadOnlyDictionary<string, string?> Overrides { get; } = new Dictionary<string, string?>();

    public ValueTask InitializeAsync()
    {
        var database = $"ll_test_{Guid.NewGuid():N}";
        Factory = new ApiFactory(SqlServerFixture.Instance.CreateConnectionString(database), Overrides);

        // 觸發主機啟動（套用 Migration 與 Seed）。
        _ = Factory.Server;
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

public sealed class RateLimitedApiFixture : ApiFixture
{
    protected override IReadOnlyDictionary<string, string?> Overrides { get; } = new Dictionary<string, string?>
    {
        ["RateLimiting:Auth:PermitLimit"] = "3",
        ["RateLimiting:Auth:WindowSeconds"] = "60",
    };
}

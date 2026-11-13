using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(LanguageLearning.IntegrationTests.Infrastructure.SqlServerFixture))]

namespace LanguageLearning.IntegrationTests.Infrastructure;

/// <summary>
/// 整合測試使用真實 SQL Server。預設以 Testcontainers 啟動容器（需要 Docker）；
/// 若設定環境變數 LL_TEST_SQLSERVER（連線字串），則改用既有的 SQL Server。
/// 每個測試類別會建立自己的資料庫並套用真實 Migration。
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string ConnectionStringVariable = "LL_TEST_SQLSERVER";
    public const string Image = "mcr.microsoft.com/mssql/server:2022-latest";

    private MsSqlContainer? _container;
    private string _baseConnectionString = string.Empty;

    public static SqlServerFixture Instance { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (!string.IsNullOrWhiteSpace(external))
        {
            _baseConnectionString = external;
        }
        else
        {
            _container = new MsSqlBuilder(Image).Build();
            await _container.StartAsync(TestContext.Current.CancellationToken);
            _baseConnectionString = _container.GetConnectionString();
        }

        Instance = this;
    }

    public string CreateConnectionString(string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(_baseConnectionString)
        {
            InitialCatalog = databaseName,
            TrustServerCertificate = true,
        };
        return builder.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

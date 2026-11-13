# 0004：xUnit v3 + Microsoft Testing Platform，整合測試使用真實 SQL Server

- 狀態：已採用（Phase 1）

## 決策
- 後端測試使用 xUnit v3（`xunit.v3.mtp-v2`），`global.json` 設定 `dotnet test` 使用 Microsoft Testing Platform。保留 `xunit.runner.visualstudio` 與 `Microsoft.NET.Test.Sdk` 以支援 Visual Studio Test Explorer。
- 整合測試以 Testcontainers 啟動 SQL Server，每個測試類別建立獨立資料庫並套用真實 Migration；不以 SQLite 或 InMemory 取代。
- 若無法使用 Docker，可設定環境變數 `LL_TEST_SQLSERVER` 指向既有 SQL Server。

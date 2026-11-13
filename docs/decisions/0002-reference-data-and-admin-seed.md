# 0002：參考資料用 HasData，管理者帳號用啟動時 Seeder

- 狀態：已採用（Phase 1）

## 決策
- 角色、考試類型、考制版本使用 EF Core `HasData`，固定 Id 與 ConcurrencyStamp，隨 Migration 建立，可重複執行。
- 管理者帳號需要密碼雜湊與 Secret，改由 `DatabaseInitializer` 在啟動時建立：只有設定 `Seed:Admin:Email` 與 `Seed:Admin:Password`（user-secrets / 環境變數）時才執行，已存在則略過。
- Development / Testing 可設定 `Database:ApplyMigrationsOnStartup` 自動套用 Migration；正式環境以 `dotnet ef migrations bundle` 或 SQL Script 部署。

## 影響
- 變更管理者密碼不會覆寫既有帳號（需透過忘記密碼流程）。

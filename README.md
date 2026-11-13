# LanguageLearning

AI 英文檢定學習平台。第一版先支援 **TOEIC Listening & Reading**，架構可擴充至 IELTS、TOEFL。
目前版本：**1.0.0（Phase 1：基礎建設）**。

> TOEIC 為 ETS 的註冊商標。本平台與 ETS 無隸屬或合作關係，所有練習內容均為自行編寫。

## 技術

| 層 | 技術 |
|---|---|
| 後端 | ASP.NET Core 10（Minimal API）、EF Core 10、SQL Server、ASP.NET Core Identity（Cookie）、FluentValidation、Serilog |
| 前端 | React 19、TypeScript、Vite、Tailwind CSS 4、shadcn/ui、React Router、TanStack Query、React Hook Form + Zod、PWA |
| 測試 | xUnit v3、Testcontainers（SQL Server）、Vitest、Testing Library、Playwright + axe |
| DevOps | Docker Compose（SQL Server、Mailpit）、GitHub Actions |

架構說明見 [docs/architecture.md](docs/architecture.md)，進度與測試結果見 [docs/progress.md](docs/progress.md)，完整需求見 [docs/requirements.md](docs/requirements.md)。

## 環境需求

- .NET SDK 10.x
- Node.js 22.22 以上（建議 24 LTS，見 `.nvmrc`）
- Docker Desktop / Docker Engine（執行中）

> Apple Silicon Mac：SQL Server 映像只提供 x86_64，請在 Docker Desktop 開啟 Rosetta（x86_64/amd64 模擬），並配置至少 2 GB 記憶體。

## 第一次啟動

```bash
# 1. 啟動 SQL Server 與 Mailpit
cp .env.example .env          # 編輯 .env，填入 MSSQL_SA_PASSWORD（需由你自行設定）
docker compose up -d --wait

# 2. 設定後端連線字串與管理者帳號（存在 user-secrets，不會進 Git）
dotnet user-secrets --project src/backend/LanguageLearning.Api set "ConnectionStrings:Default" \
  "Server=localhost,1433;Database=LanguageLearning;User Id=sa;Password=<與 .env 相同>;TrustServerCertificate=True;Encrypt=True"
dotnet user-secrets --project src/backend/LanguageLearning.Api set "Seed:Admin:Email" "<管理者 Email>"
dotnet user-secrets --project src/backend/LanguageLearning.Api set "Seed:Admin:Password" "<至少 10 碼，含大小寫與數字>"

# 3. 建立資料庫（Development 啟動時也會自動套用 Migration）
dotnet tool restore
dotnet ef database update -p src/backend/LanguageLearning.Infrastructure -s src/backend/LanguageLearning.Api

# 4. 啟動後端（http://localhost:5080，OpenAPI：/openapi/v1.json）
dotnet run --project src/backend/LanguageLearning.Api

# 5. 另開終端機啟動前端（http://localhost:5173，/api 由 Vite proxy 轉送到後端）
cd src/frontend
npm ci
npm run dev
```

- 驗證信與重設密碼信：開啟 Mailpit http://localhost:8025
- 健康檢查：http://localhost:5080/health/live、http://localhost:5080/health/ready
- 版本：http://localhost:5080/api/version

### 重設本機資料庫

```bash
dotnet ef database drop -f -p src/backend/LanguageLearning.Infrastructure -s src/backend/LanguageLearning.Api
dotnet ef database update -p src/backend/LanguageLearning.Infrastructure -s src/backend/LanguageLearning.Api
```

（若要連同 SQL Server 資料一併清除：`docker compose down -v`，會刪除 Docker volume。）

## 測試

```bash
# 後端：單元測試
dotnet test --project tests/backend/LanguageLearning.UnitTests/LanguageLearning.UnitTests.csproj

# 後端：整合測試（以 Testcontainers 啟動 SQL Server，需要 Docker）
dotnet test --project tests/backend/LanguageLearning.IntegrationTests/LanguageLearning.IntegrationTests.csproj
# 若不使用 Testcontainers，可改用既有 SQL Server：export LL_TEST_SQLSERVER="<連線字串>"
# 注意：此模式不會刪除測試資料庫（ll_test_*），請使用專用執行個體，例如 Windows 上的 LocalDB：
#   sqllocaldb create LanguageLearningTest -s
#   $env:LL_TEST_SQLSERVER='Server=(localdb)\LanguageLearningTest;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=60'

# 前端
cd src/frontend && npm run lint && npm run typecheck && npm test && npm run build

# E2E（需要 docker compose 的 SQL Server 與 Mailpit；Playwright 會自動啟動 API 與前端 preview）
cd tests/e2e
npm ci && npx playwright install chromium
E2E_CONNECTION_STRING="<連線字串>" E2E_ADMIN_PASSWORD="<管理者密碼>" npx playwright test
```

## 專案結構

```text
src/backend/   LanguageLearning.Api / Application / Domain / Infrastructure
src/frontend/  React + Vite PWA（單元測試與原始碼放在一起：*.test.ts(x)）
tests/backend/ LanguageLearning.UnitTests、LanguageLearning.IntegrationTests
tests/e2e/     Playwright
docs/          需求、架構、進度、架構決策（ADR）
```

## GitHub Actions

- **CI**（`.github/workflows/ci.yml`）：後端 build（警告視為錯誤）、EF Model 與 Migration 一致性、單元 / 整合測試、前端 lint / typecheck / test / build、E2E（Playwright + axe）、套件弱點檢查。
- **Bootstrap generated files**（`.github/workflows/bootstrap.yml`，手動執行）：產生 `package-lock.json` 與初始 EF Core Migration 並提交回 Repository。

## 版本

後端版本定義於 `Directory.Build.props`，前端定義於 `src/frontend/package.json`，兩者必須一致。

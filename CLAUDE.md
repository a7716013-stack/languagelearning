# languagelearning

AI 英文檢定學習平台（先做 TOEIC L&R）。ASP.NET Core 10 + EF Core 10 + SQL Server + React/TypeScript/Vite。
完整需求：docs/requirements.md（需求唯一來源）。目前進度：docs/progress.md。架構：docs/architecture.md。

## 工作規則
- 一次只做一個 Phase；先在 Plan Mode 提出計畫，經確認後才實作。
- 每個里程碑完成後執行 build 與測試，通過後才 commit（Conventional Commits）。
- 不得為了通過測試而刪除、略過測試或放寬斷言。
- 只回報實際執行過的結果；無法執行的項目標示「未執行」並說明原因。
- 環境受阻（SDK 缺少、網路被擋）時停下來回報，不使用非官方來源繞過。
- 不建立後續 Phase 的空介面或假頁面。
- Secret 只放 user-secrets / .env，不提交；不填假密鑰。
- 未經同意不 push、不刪除 Docker volume、不 drop 資料庫。

## 架構規則
- 分層：Api → Application → Domain；Infrastructure 實作 Application 介面。Domain 不相依任何層與框架（有架構測試）。
- 模組是各層內的資料夾（Users、Exams、Goals、Admin…），不另建專案。
- 前後端同源；前端只呼叫相對路徑 /api；Cookie 驗證 + X-XSRF-TOKEN（CsrfProtectionMiddleware）。
- 錯誤回應一律 ProblemDetails，含穩定的 `code`；使用者訊息用繁體中文。
- 資料庫時間一律 UTC（欄位以 Utc 結尾，UtcDateTimeConverter），時間來源用 TimeProvider。
- 參考資料（角色、考試類型）用 HasData；管理者帳號由 DatabaseInitializer 依 Seed:Admin 設定建立。
- 日誌禁止記錄密碼、Token、Cookie、Email 驗證或重設連結、完整 Email（有整合測試）。
- 前端單元測試與原始碼放一起（*.test.ts(x)）；E2E 在 tests/e2e。

## 常用指令
- 依賴服務：`cp .env.example .env`（填密碼）→ `docker compose up -d --wait`
- 後端設定：`dotnet user-secrets --project src/backend/LanguageLearning.Api set "ConnectionStrings:Default" "<連線字串>"`
- 資料庫：`dotnet tool restore` → `dotnet ef database update -p src/backend/LanguageLearning.Infrastructure -s src/backend/LanguageLearning.Api`
- 新增 Migration：`dotnet ef migrations add <Name> -p src/backend/LanguageLearning.Infrastructure -s src/backend/LanguageLearning.Api -o Persistence/Migrations`
- 後端：`dotnet build LanguageLearning.slnx`、`dotnet test --project tests/backend/LanguageLearning.UnitTests/LanguageLearning.UnitTests.csproj`、整合測試同理（需要 Docker）
- 啟動 API：`dotnet run --project src/backend/LanguageLearning.Api`（http://localhost:5080）
- 前端（src/frontend）：`npm ci`、`npm run dev`（http://localhost:5173）、`npm run lint`、`npm run typecheck`、`npm test`、`npm run build`
- E2E（tests/e2e）：`npm ci`、`npx playwright install chromium`、`npx playwright test`

## 版本
目前開發版本 2.1.0（Phase 2）。後端版本只改 Directory.Build.props；前端只改 src/frontend/package.json；兩者必須一致（單元測試與 CI 會檢查）。

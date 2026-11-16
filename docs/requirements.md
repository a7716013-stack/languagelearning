# LanguageLearning — Claude 實作指令文件

| 項目 | 內容 |
|---|---|
| 專案代號（Repository / 資料夾） | `languagelearning` |
| C# 命名空間 / 專案前綴 | `LanguageLearning` |
| 產品顯示名稱 | LanguageLearning（副標：AI 英文檢定學習平台） |
| 起始版本 | **1.0.0** |
| 執行者 | Claude Code（建議在開發者本機執行） |
| 文件來源 | 改寫自「AI English Academy — Codex Master Development Prompt」 |

---

## 0. 如何使用本文件

### 0.1 執行環境需求

Claude Code 需要在可以實際編譯、執行資料庫與瀏覽器測試的環境中工作。建議直接在開發者本機使用 Claude Code（CLI 或桌面版）。

| 工具 | 版本要求 | 用途 |
|---|---|---|
| .NET SDK | 10.x（LTS） | 後端建置、EF Core Migration |
| Node.js | 22 LTS 以上 | 前端建置、Vitest、Playwright |
| Docker Desktop / Docker Engine | 正在執行中 | SQL Server、Mailpit、整合測試（Testcontainers） |
| Git | 任一近期版本 | 版本控制 |
| Claude Code | 最新版 | 實作者 |

網路需能連線：`api.nuget.org`、`registry.npmjs.org`、`mcr.microsoft.com`、Docker Hub、Playwright 瀏覽器下載來源、`github.com`。

> Apple Silicon（M 系列）Mac：SQL Server 官方映像只提供 x86_64，請在 Docker Desktop 開啟「Use Rosetta for x86_64/amd64 emulation」，並配置至少 2 GB 記憶體給 Docker。

### 0.2 使用步驟

1. 建立空資料夾 `languagelearning` 並執行 `git init`。
2. 將**本文件整份**存成 `docs/requirements.md`（需求的唯一來源）。
3. 將**附錄 A** 的內容存成專案根目錄的 `CLAUDE.md`。
4. 在 `languagelearning` 資料夾啟動 Claude Code，先切換到 Plan Mode。
5. 貼上**附錄 B** 的 Phase 1 啟動指令。
6. 審核 Claude 提出的計畫，同意後讓它開始實作。
7. 完成後依**附錄 C** 自行驗收。
8. 後續每個 Phase 開新對話（或 `/clear`），貼上**附錄 D** 的範本；Claude 會從 `CLAUDE.md` 與 `docs/progress.md` 接續進度。

---

## 1. 角色與專案目標

你是一名資深 ASP.NET Core、React、SQL Server、AI 系統架構師與全端工程師，以正式產品品質為目標。

請建立全新且獨立的 AI 英文檢定學習平台 **languagelearning**：

- 與既有 Worklog 專案完全獨立，不依賴其程式碼、資料庫或登入系統。
- 正式可持續開發的響應式 Web / PWA，支援電腦、平板和手機。
- 介面語言為繁體中文（zh-TW），英文題目及教學內容保留英文。
- 主要對象為英文檢定考生；先實作 TOEIC Listening & Reading，架構必須能擴充至 IELTS、TOEFL。

---

## 2. Claude 工作守則

這一節規範「怎麼做」，優先於其他章節的細節描述。

### 2.1 先探索、後規劃、再實作
1. 開始前先檢查：Repository 狀態、`dotnet --info`、`node -v`、`docker info`、`git status`。缺少工具時立即回報，不要繼續。
2. 每個 Phase 先在 Plan Mode 產出實作計畫，內容須包含：
   - 要建立 / 修改的專案與主要檔案
   - Entities 與 Migration 內容
   - API 端點清單
   - 前端頁面與元件
   - 測試清單（單元、整合、E2E）
   - 風險、假設與需要開發者決定的事項
3. 等開發者確認計畫後才開始寫程式。

### 2.2 任務追蹤
- 計畫確認後建立任務清單，依里程碑拆分；同一時間只有一個任務在進行中。
- 每完成一個任務立即更新狀態，不要累積到最後一次更新。

### 2.3 驗證迴圈
- 每完成一個里程碑就執行對應的 Build、Lint、Type Check 與測試。
- 失敗時找出根本原因並修正。**禁止**為了讓測試通過而：刪除或略過測試、放寬斷言、加上 `[Skip]` / `.skip`、關閉編譯警告、使用 `--no-verify`。
- 後端建置以 `TreatWarningsAsErrors` 為目標；若有無法消除的警告，須記錄原因。

### 2.4 誠實回報
- 只回報實際執行過的指令結果，並附上指令與結果摘要（例如：`dotnet test` → 通過 48、失敗 0、略過 0）。
- 無法執行的項目（例如 Docker 未啟動導致整合測試無法執行）須明確標示「未執行」與原因，不得標為完成。
- 不得虛構已成功執行的測試、截圖或輸出。

### 2.5 環境受阻時
- 套件來源被擋、SDK 缺少、網路逾時：停下來回報，說明被擋的來源與需要開發者處理的事項。
- 不得改用非官方鏡像、來路不明的套件或繞過網路限制。
- 不得在未告知的情況下降低規格（例如把 SQL Server 換成 SQLite 來「讓測試能跑」）。

### 2.6 範圍控制
- **只實作目前 Phase。** 不建立後續 Phase 的空介面、空資料夾、假頁面或 TODO 骨架。
- 需求不清楚或與實作衝突時，選擇最合理的做法，並記錄在 `docs/decisions/NNNN-標題.md`（ADR 格式），在階段報告中列出。
- 不刪除或改寫開發者已存在的檔案，除非是本 Phase 計畫的一部分。

### 2.7 Git
- 每個里程碑完成且驗證通過後 commit，使用 Conventional Commits（`feat:`、`fix:`、`test:`、`docs:`、`chore:`）。
- Commit 前檢查 `git status` 與 diff，確認沒有 Secret、`.env`、`bin/`、`obj/`、`node_modules/`、測試產物。
- 未經開發者要求不 push、不建立遠端 repo、不建立 tag。

### 2.8 安全操作
以下操作須先取得開發者同意：刪除專案外檔案、`git reset --hard`、`git push --force`、刪除 Docker volume、Drop 資料庫、全域安裝工具、修改系統設定。

### 2.9 Secret 管理
- 本機使用 `dotnet user-secrets` 與 `.env`（不提交）；提交 `.env.example`，只放佔位字串並註明「需由開發者提供」。
- 不填入假密鑰、範例金鑰或看似真實的密碼。
- 測試用的資料庫密碼由 Testcontainers 動態產生，或於 CI 以環境變數提供。

### 2.10 文件維護
每個 Phase 結束時更新：
- `README.md`：專案簡介、需求環境、啟動方式、測試方式
- `docs/architecture.md`：架構、分層規則、橫切關注點、資料模型
- `docs/progress.md`：各 Phase 完成項目、測試結果、未解決事項、下一步（作為下一次對話的交接文件）
- `docs/decisions/`：架構決策紀錄
- `CLAUDE.md`：指令或慣例變更時同步更新，保持精簡

### 2.11 交付前自我審查
最終報告前，逐項對照第 12 節〈安全性與品質檢查表〉與該 Phase 驗收條件。涉及驗證、授權、計分的變更，建議另開一個 subagent，在不參考實作過程的情況下獨立審查程式碼，並把審查結果列入報告。

---

## 3. 技術規格

### 3.1 版本政策
- 一律使用**安裝當下最新的穩定版本**，不使用 Preview / RC / 已淘汰 API。
- 套件之間須確認相容（例如 `vite-plugin-pwa` 支援的 Vite 版本、`@hookform/resolvers` 支援的 Zod 版本、shadcn/ui 支援的 Tailwind 版本）。
- 後端使用 Central Package Management（`Directory.Packages.props`）；以 `global.json` 鎖定 .NET SDK 主版本。
- 前端提交 `package-lock.json`；CI 使用 `npm ci`。

### 3.2 產品版本號
- 採 Semantic Versioning。Phase 1 交付版本為 **1.0.0**；Phase 2 交付版本為 **2.1.0**（開發者指定，見 `docs/decisions/0005-versioning-phase2.md`）。之後各 Phase 的版本號於該 Phase 開始時由開發者決定；修正提升 patch。
- 後端版本只定義在 `Directory.Build.props`（例如 `<Version>2.1.0</Version>`）。
- 前端版本只定義在 `src/frontend/package.json`（例如 `"version": "2.1.0"`），透過 Vite `define` 注入介面。
- 版本須出現在：OpenAPI 文件 `info.version`、`GET /api/version`、前端「設定」頁與頁尾。
- CI 加入檢查：前後端版本號必須一致。

### 3.3 Backend
- C#、ASP.NET Core 10 Web API（Minimal API，依功能分組的 Endpoint 類別）
- Entity Framework Core 10 + SQL Server
- ASP.NET Core Identity（Cookie-based Authentication）
- 內建 OpenAPI（`AddOpenApi` / `MapOpenApi`），僅於 Development 開放
- Dependency Injection、Options Pattern（啟動時驗證設定 `ValidateOnStart`）
- FluentValidation
- Serilog 結構化日誌
- ASP.NET Core 內建 Rate Limiting
- Health Checks（含 EF Core DbContext 檢查）
- `TimeProvider` 取得時間（可測試）

### 3.4 Frontend
- React + TypeScript（strict）+ Vite
- Tailwind CSS + shadcn/ui（以 CLI 加入元件；若 CLI 無法連線，回報後依官方原始碼手動建立）
- React Router
- TanStack Query
- React Hook Form + Zod
- Recharts（第一次需要圖表的 Phase 才加入，不預先安裝未使用套件）
- `vite-plugin-pwa`（Manifest + Service Worker）
- ESLint + Prettier
- 響應式設計與無障礙支援

### 3.5 Testing / DevOps
- 後端：xUnit、`WebApplicationFactory` 整合測試、Testcontainers（SQL Server）
- 前端：Vitest + Testing Library
- E2E：Playwright + `@axe-core/playwright`（無障礙檢查）
- Docker Compose：SQL Server、Mailpit
- GitHub Actions CI
- `.env.example`、`README.md`

---

## 4. 專案結構

```text
languagelearning/
├── .github/workflows/ci.yml
├── .config/dotnet-tools.json          # dotnet-ef 本地工具
├── src/
│   ├── backend/
│   │   ├── LanguageLearning.Api/
│   │   ├── LanguageLearning.Application/
│   │   ├── LanguageLearning.Domain/
│   │   └── LanguageLearning.Infrastructure/
│   └── frontend/
│       ├── public/                    # 圖示、manifest 資源
│       └── src/
│           ├── app/                   # Router、Providers、Layouts
│           ├── components/            # ui/（shadcn）、layout/、共用元件
│           ├── features/              # 依功能分資料夾，見下方說明
│           ├── hooks/
│           ├── services/              # API client
│           ├── types/
│           └── styles/
├── tests/
│   ├── backend/
│   │   ├── LanguageLearning.UnitTests/
│   │   └── LanguageLearning.IntegrationTests/
│   └── e2e/                           # Playwright（獨立 package.json）
├── docs/
│   ├── requirements.md                # 本文件
│   ├── architecture.md
│   ├── progress.md
│   └── decisions/
├── LanguageLearning.sln(x)            # 依 .NET 10 SDK 預設格式
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── .editorconfig
├── docker-compose.yml
├── .env.example
├── .gitignore
├── CLAUDE.md
└── README.md
```

**前端單元測試位置：** Vitest 測試與原始碼放在一起（`src/frontend/src/**/*.test.ts(x)`）。放在 `tests/frontend/` 會因為 `node_modules` 位於 `src/frontend/` 而無法解析套件，因此不另建 `tests/frontend/`。

**前端 features 資料夾：** 只建立當前 Phase 用到的功能資料夾。全部規劃如下：`auth`、`onboarding`、`dashboard`、`settings`、`exams`、`practice`、`vocabulary`、`study-plan`、`analytics`、`ai-tutor`、`admin`。

### 4.1 Modular Monolith 規則
- 採四層架構，**模組是各層內的資料夾**（例如 `Application/Users/`、`Application/Exams/`、`Domain/Exams/`），不另外建立每個模組的專案。
- 相依方向：
  - `Domain`：不相依任何其他專案，也不相依 EF Core、ASP.NET Core。
  - `Application`：只相依 `Domain`；定義介面（如 `IAppDbContext`、`IEmailSender`、`ICurrentUser`）與 Use Case、DTO、Validator。
  - `Infrastructure`：相依 `Application`、`Domain`；實作 EF Core、Identity、Email、外部服務。
  - `Api`：相依 `Application`；只為了 DI 組裝而參考 `Infrastructure`。
- 主要商業邏輯放在 `Domain` / `Application`；Endpoint 只負責 HTTP 轉換。
- `ApplicationUser`（繼承 Identity）放在 `Infrastructure/Identity`；`Domain` 中的實體以 `Guid UserId` 參照使用者，不直接相依 Identity 型別。
- 加入架構測試（例如以反射檢查組件參考），確保 `Domain` 不相依其他層。

---

## 5. 架構與橫切關注點

### 5.1 同源部署
- 前後端**同源**，不使用跨域 CORS 加憑證的做法。
- 開發：Vite dev server 以 proxy 將 `/api`、`/health` 轉發到 ASP.NET Core。
- 正式：由反向代理（或 API 提供前端靜態檔）讓前端與 API 共用同一個網域。
- 前端所有 API 呼叫使用相對路徑 `/api/...`，`credentials: 'same-origin'`。

### 5.2 驗證（Authentication）
- 使用 ASP.NET Core Identity，自訂 `/api/auth/*` 端點（不使用 `MapIdentityApi`，因其路由、角色與錯誤格式無法完整符合需求）。
- 使用者主鍵為 `Guid`；角色：`Admin`、`Student`（註冊時預設 Student）。
- Auth Cookie 設定：
  - `HttpOnly`、`SameSite=Strict`、`Path=/`
  - `SecurePolicy`：Production 為 `Always`，Development / Testing 為 `SameAsRequest`
  - Production 使用 `__Host-` 前綴的 Cookie 名稱
  - 合理的到期時間與滑動延長；「記住我」才建立持久 Cookie
- API 未登入回傳 `401`、權限不足回傳 `403`（ProblemDetails），**不得**回傳 302 轉址。
- 密碼原則：至少 10 碼，包含大小寫字母與數字。
- 帳號鎖定：連續失敗 5 次鎖定 15 分鐘（可設定）。
- `RequireConfirmedEmail` 預設為 `true`（可設定）。
- 避免帳號列舉：註冊、重寄驗證信、忘記密碼，不論 Email 是否存在都回傳一致的回應；已註冊的 Email 再次註冊時，改寄「此帳號已存在」通知信。
- 變更密碼、重設密碼後更新 Security Stamp，使其他裝置的工作階段失效。
- 前端**不得**使用 localStorage / sessionStorage 儲存任何登入憑證或 Token。

### 5.3 CSRF 防護
- 使用 ASP.NET Core Antiforgery，Header 名稱 `X-XSRF-TOKEN`。
- `GET /api/auth/csrf` 回傳 `{ token }` 並設定 Antiforgery Cookie。
- 以 Middleware 或 Endpoint Filter 驗證**所有** `/api` 的 POST / PUT / PATCH / DELETE，包含 login 與 register。
- 前端將 Token 存在記憶體；登入、登出後重新取得 Token（Token 綁定使用者身分）；收到 CSRF 驗證失敗時重新取得 Token 並重試一次。

### 5.4 錯誤回應
- 統一使用 ProblemDetails（RFC 9457），包含 `traceId`。
- 驗證錯誤回傳 `400`，`errors` 為「欄位 → 訊息陣列」。
- 使用者看得到的訊息使用繁體中文；Identity 錯誤以自訂 `IdentityErrorDescriber` 翻譯。
- Production 不回傳 Stack Trace 或內部例外訊息。
- 定義穩定的錯誤代碼（例如 `email_not_confirmed`、`account_locked`），前端依代碼顯示訊息。

### 5.5 驗證規則
- 後端以 FluentValidation 為準；前端 Zod Schema 對應相同規則，僅作為使用體驗輔助。
- 驗證以 Endpoint Filter 統一執行。

### 5.6 日誌
- Serilog：Console（開發）+ 結構化輸出；加入 Request Logging 與 `traceId` 關聯。
- **禁止**記錄：密碼、Token、Cookie、Email 驗證碼、重設密碼連結、完整 Email（以 UserId 取代）、AI API Key。
- 撰寫測試，確認登入與重設密碼流程的日誌不包含上述資料。

### 5.7 時間
- 資料庫一律存 UTC；欄位命名以 `Utc` 結尾（例如 `CreatedAtUtc`）。
- 讀取時確保 `DateTimeKind.Utc`（EF Core Value Converter）。
- 純日期（如考試日期）使用 `DateOnly`。
- 使用者時區欄位 `TimeZoneId`（IANA，預設 `Asia/Taipei`），顯示時於前端轉換。
- 程式碼一律透過 `TimeProvider` 取得目前時間。

### 5.8 Rate Limiting
- 政策：`auth`（登入、註冊、忘記密碼、重寄驗證信，依 IP 分區）、`api`（一般 API，依使用者或 IP 分區）；Phase 2 起加入 `exam`，Phase 4 加入 `ai`。
- 限制數值由設定檔提供；超過時回傳 `429` 與 `Retry-After`。
- 整合測試使用較低的限制值驗證 `429`。

### 5.9 Email
- `Application` 定義 `IEmailSender`；`Infrastructure` 實作 SMTP 版本。
- 開發環境使用 Docker Compose 中的 **Mailpit**（SMTP `1025`、網頁 `8025`）。
- 整合測試使用記憶體收集版本；E2E 透過 Mailpit API（`/api/v1/messages`）讀取驗證信。
- 信件內容為繁體中文；連結以設定值 `App:PublicBaseUrl` 組成，指向前端頁面。
- 寄信失敗不得讓註冊流程回傳 500；記錄錯誤（不含 Token）並允許使用者重寄。

### 5.10 設定與 Secret
主要設定區段（以 Options 類別綁定並驗證）：
- `ConnectionStrings:Default`
- `App:PublicBaseUrl`
- `Auth:*`（RequireConfirmedEmail、鎖定次數、Cookie 期限）
- `RateLimiting:*`
- `Email:Smtp:*`（Host、Port、From、帳密）
- `Seed:Admin:Email`、`Seed:Admin:Password`（只從 user-secrets / 環境變數提供）
- `Database:ApplyMigrationsOnStartup`（只允許 Development 為 true）

### 5.11 資料庫 Migration 與 Seed
- 使用 EF Core Migrations；以 `.config/dotnet-tools.json` 鎖定 `dotnet-ef` 版本。
- 參考資料（角色、ExamType、ExamVersion）使用 `HasData`，隨 Migration 建立，固定 Guid 與 ConcurrencyStamp，確保可重複執行且不重複。
- Admin 帳號由啟動時的 Seeder 建立：只有在設定 `Seed:Admin:*` 時才執行；已存在則跳過；密碼不寫入程式碼或日誌。
- Development 可設定啟動時自動套用 Migration；Production 使用 `dotnet ef migrations bundle` 或產生 SQL Script，由部署流程執行。
- 提供文件說明如何從零建立資料庫、如何重設本機資料庫。

### 5.12 並行控制與稽核
- 重要可編輯資料加入 `rowversion` 並行控制；衝突回傳 `409`。
- `AuditLog` 記錄：登入成功 / 失敗、帳號鎖定、註冊、Email 驗證、重設密碼、變更密碼、角色變更、所有管理者操作。
- 稽核內容不得包含密碼、Token；只記錄必要欄位。

### 5.13 健康檢查與版本
- `GET /health/live`：程序存活
- `GET /health/ready`：含資料庫連線檢查
- `GET /api/version`：`{ version, environment, commit? }`

---

## 6. 功能需求總覽

每項標示預計實作的 Phase。

### A. 使用者系統（Phase 1）
- Email 註冊、登入、登出
- 忘記密碼與重設密碼、變更密碼
- Email 驗證、重寄驗證信
- Admin / Student 角色
- CSRF 保護、Cookie 安全設定、登入速率限制、帳號鎖定
- 使用者資料（顯示名稱、時區）與學習偏好（介面主題）
- 不使用 localStorage 儲存登入憑證

### B. 初次使用流程
- （Phase 1）選擇 TOEIC、IELTS、TOEFL；TOEIC 可設定，其餘顯示「尚未開放」
- （Phase 1）設定目標分數（依考試分數範圍與級距驗證）
- （Phase 1）設定預計考試日期（不得早於今天，依使用者時區判斷）
- （Phase 1）設定每日可學習時間（10–600 分鐘）
- （Phase 3）完成基礎程度診斷；Phase 1 的流程結尾以提示說明「程度診斷將於後續版本開放」，不建立假的診斷頁

### C. TOEIC 題庫（Phase 2）
- 支援 Part 1～7 題型結構
- 題組、文章、音檔、圖片
- 單選題與共用題組
- 難易度、技能、考點分類
- 正確答案、選項解析
- 題目狀態：草稿 → 審核 → 發布 → 停用
- 題目版本及題組完整性驗證
- 管理者批次匯入合法題目
- 媒體檔透過 `IMediaStorage` 抽象層存取；開發使用本機檔案系統，正式可替換為雲端儲存；音檔與圖片需權限檢查，不可被任意列舉

### D. 練習引擎（Phase 2）
- 依 Part、難度、技能、題數建立練習
- 隨機出題
- 自動保存答案
- 題目導航
- 題組共用閱讀內容及音檔
- 交卷後批改與答題解析
- 作答期間 API 不得回傳正確答案或解析

### E. 模擬考（Phase 2）
- 自訂試卷及固定題目順序
- 依考制設定各考試區段時間
- 以伺服器時間為準
- 重整頁面後可恢復答題
- 交卷只能成功一次（以並行控制與狀態機保證，重複請求回傳相同結果）
- 逾時自動結束
- 保存試卷與評分版本快照
- 成績報告及答題明細

**計分注意事項：**
- ETS 未公開官方的原始分數換算表（實作前請再次查證）。本平台的換算一律以可設定的 `ScoringRule` 實作，介面上標示為「**預估分數**」，並註明非官方成績。
- 非完整試卷不得顯示換算分數，只顯示正確率與答對題數。
- TOEIC 為 ETS 的註冊商標；產品文案需註明本平台與 ETS 無隸屬或合作關係，不使用官方 Logo。

### F. 錯題本（Phase 3）
- 自動記錄錯題
- 依題型、技能、考點篩選
- 錯題重新練習
- 追蹤掌握程度
- 保留歷史答題紀錄

### G. 單字系統（Phase 3）
- 單字、音標、中文翻譯
- 例句、詞性、相關片語
- 音訊播放
- 收藏單字
- SRS 間隔重複複習（演算法需有單元測試）
- 每日到期單字清單（依使用者時區計算「今天」）

### H. 學習首頁
- （Phase 1）目標考試、目標分數、距離考試天數、每日目標時間
- （Phase 2）最近成績、練習正確率
- （Phase 3）每日完成任務、連續學習天數、弱點分類、學習時間圖表、下一項推薦練習
- 尚未有資料的區塊顯示 Empty State，不放假數據

### I. 管理後台
- （Phase 1）後台入口與權限控管、使用者列表（分頁、搜尋）、Audit Log 檢視（分頁、篩選）、基本統計摘要
- （Phase 2）題庫 CRUD、分類及考試版本管理、試卷建立、題庫匯入及驗證
- （Phase 3）學習數據摘要、角色管理
- （Phase 4）AI 草稿審核
- 所有管理操作寫入 Audit Log

---

## 7. 資料庫

### 7.1 Entities 與實作階段

| Phase | Entities |
|---|---|
| 1 | `ApplicationUser`（Identity，含 DisplayName、TimeZoneId、UiTheme、CreatedAtUtc、LastLoginAtUtc、OnboardingCompletedAtUtc）、`ApplicationRole`、`ExamType`、`ExamVersion`、`UserExamGoal`、`AuditLog` |
| 2 | `QuestionType`、`Skill`、`Topic`、`QuestionGroup`、`Question`、`QuestionOption`、`QuestionTopic`、`MediaAsset`、`ExamPaper`、`ExamPaperItem`、`ExamAttempt`、`AttemptAnswer`、`ScoringRule` |
| 3 | `VocabularyItem`、`UserVocabularyProgress`、`MistakeRecord`、`StudyPlan`、`StudyTask`（程度診斷所需資料於 Phase 3 計畫中設計） |
| 4 | `AiConversation`、`AiMessage`、`AiFeedback`、`AiUsageRecord` |

Phase 1 主要欄位建議：
- `ExamType`：`Code`（TOEIC / IELTS / TOEFL，唯一）、`Name`、`IsAvailable`、`MinScore`、`MaxScore`、`ScoreStep`、`SortOrder`
- `ExamVersion`：`ExamTypeId`、`Code`（唯一）、`Name`、`Description`、`IsCurrent`、`EffectiveFrom`
- `UserExamGoal`：`UserId`、`ExamTypeId`、`TargetScore`（decimal）、`ExamDate`（DateOnly）、`DailyStudyMinutes`、`CreatedAtUtc`、`UpdatedAtUtc`、`RowVersion`；唯一索引 `(UserId, ExamTypeId)`
- `AuditLog`：`Id`（long）、`OccurredAtUtc`、`ActorUserId`（nullable）、`Action`、`EntityType`、`EntityId`、`Succeeded`、`IpAddress`、`Details`（JSON，已過濾敏感資料）；索引 `OccurredAtUtc`、`ActorUserId`、`Action`

Seed 說明：TOEIC 分數範圍 10–990、級距 5，`IsAvailable = true`。IELTS 與 TOEFL 為 `IsAvailable = false`；其分數制度須在開放前（Phase 5）依官方最新公告確認，Phase 1 不讓使用者為未開放的考試設定目標。

### 7.2 資料庫要求
1. 使用 EF Core Fluent API（每個 Entity 一個 `IEntityTypeConfiguration`）。
2. 定義 PK、FK、刪除行為、唯一索引與必要查詢索引。
3. 字串欄位設定長度上限；金額或分數使用明確精度的 decimal。
4. 正確處理 UTC 日期（見 5.7）。
5. 重要答題資料與可編輯資料加入並行控制。
6. 使用 Migration 建立資料庫，Migration 須可從零重現。
7. Seed 可重複執行且不產生重複資料。
8. 只提供自行編寫的合法示範題目與單字，不放入未授權的官方考題。
9. 重要歷史紀錄（作答、成績）不可因題庫更新而被破壞：使用快照與軟刪除，避免連鎖刪除。

---

## 8. API 設計

### 8.1 路由總覽

```text
/api/auth/*              Phase 1
/api/users/me            Phase 1
/api/exam-types          Phase 1
/api/exam-versions       Phase 1
/api/admin/*             Phase 1 起逐步擴充
/api/questions/*         Phase 2
/api/practice-sessions/* Phase 2
/api/exam-papers/*       Phase 2
/api/exam-attempts/*     Phase 2
/api/results/*           Phase 2
/api/placement-tests/*   Phase 3
/api/vocabulary/*        Phase 3
/api/mistakes/*          Phase 3
/api/study-plans/*       Phase 3
/api/analytics/*         Phase 3
/api/ai/*                Phase 4
```

### 8.2 Phase 1 端點

| 方法 | 路徑 | 授權 | 說明 |
|---|---|---|---|
| GET | `/api/auth/csrf` | 匿名 | 取得 CSRF Token |
| POST | `/api/auth/register` | 匿名 | 註冊；一律回傳 202 並寄信 |
| POST | `/api/auth/confirm-email` | 匿名 | 驗證 Email（userId + token） |
| POST | `/api/auth/resend-confirmation` | 匿名 | 重寄驗證信；一律 202 |
| POST | `/api/auth/login` | 匿名 | 登入；錯誤訊息不透露帳號是否存在 |
| POST | `/api/auth/logout` | 登入 | 登出 |
| POST | `/api/auth/forgot-password` | 匿名 | 寄送重設密碼信；一律 202 |
| POST | `/api/auth/reset-password` | 匿名 | 重設密碼 |
| POST | `/api/auth/change-password` | 登入 | 變更密碼 |
| GET | `/api/users/me` | 登入 | 個人資料、角色、Onboarding 狀態 |
| PUT | `/api/users/me` | 登入 | 更新顯示名稱、時區、介面主題 |
| GET | `/api/users/me/exam-goals` | 登入 | 取得學習目標 |
| PUT | `/api/users/me/exam-goals/{examTypeCode}` | 登入 | 建立或更新目標（含並行控制） |
| POST | `/api/users/me/onboarding/complete` | 登入 | 完成初次設定 |
| GET | `/api/exam-types` | 匿名 | 考試類型、開放狀態、分數範圍 |
| GET | `/api/exam-versions` | 匿名 | 依 `examType` 篩選考制版本 |
| GET | `/api/admin/summary` | Admin | 使用者數、今日註冊數等摘要 |
| GET | `/api/admin/users` | Admin | 使用者列表（分頁、搜尋） |
| GET | `/api/admin/audit-logs` | Admin | 稽核紀錄（分頁、依動作與日期篩選） |
| GET | `/api/version` | 匿名 | 版本資訊 |
| GET | `/health/live`、`/health/ready` | 匿名 | 健康檢查 |

### 8.3 API 共通要求
- 所有端點使用 DTO，不直接回傳 Entity。
- 分頁格式統一：`?page=1&pageSize=20`（pageSize 上限 100），回傳 `{ items, page, pageSize, totalCount, totalPages }`。
- 所有涉及個人資料的端點只透過 `/me` 或檢查資源擁有者存取；Phase 2 起，正式考試開始、儲存答案、交卷及查詢成績必須檢查資源擁有者，越權存取回傳 `404`（不洩漏資源是否存在）。
- 每個端點都有授權、驗證、錯誤回應的整合測試。
- OpenAPI 文件需包含回應型別與錯誤回應說明。

---

## 9. AI 架構（Phase 4）

先建立可替換的介面，避免主要商業邏輯直接依賴特定 AI 廠商。

```csharp
public interface IAiTutorService
{
    Task<AiExplanationResult> ExplainQuestionAsync(
        ExplainQuestionRequest request,
        CancellationToken cancellationToken);

    Task<StudyRecommendationResult> GenerateStudyPlanAsync(
        StudyRecommendationRequest request,
        CancellationToken cancellationToken);
}
```

後續擴充：SpeechToText、TextToSpeech、WritingFeedback、SpeakingFeedback、ConversationService。

規則：
- AI API Key 只放在後端安全設定，禁止暴露於前端。
- AI 不參與客觀題最終答案判定。
- AI 題目解說只能使用已驗證的正確答案作為輸入。
- AI 產生的分析、作文評語和口說評估須標註為「練習參考」。
- 加入：模型設定、Token / 費用紀錄、快取、逾時處理、速率限制、隱私資料過濾、不可信輸入防護（Prompt Injection 防護）。
- 無 API Key 或 AI 服務錯誤時，核心學習功能與測驗不受影響。

---

## 10. UI / UX 要求

### 10.1 風格
- 繁體中文介面，`<html lang="zh-Hant-TW">`
- 清爽、現代化的專業教育平台
- 白色背景，主色靛藍（indigo），輔色柔和紫（violet）
- Light / Dark Mode（跟隨系統，可手動切換；偏好可同步到使用者設定）
- 以 CSS 變數定義設計 Token，統一元件樣式、間距與字體
- 字體使用系統字體堆疊：`"Noto Sans TC", "PingFang TC", "Microsoft JhengHei", system-ui, sans-serif`
- 手機優先響應式排版
- 桌面：左側導覽；手機：底部導航
- 正式模擬考（Phase 2）採沉浸式作答介面，隱藏一般導覽

### 10.2 每個主要頁面必須包含
- Loading State（Skeleton）
- Empty State
- Error State（含重試）
- Success Feedback（Toast）
- 表單驗證與欄位錯誤訊息
- 鍵盤操作支援

### 10.3 無障礙
- 目標 WCAG 2.2 AA
- 「跳到主要內容」連結、可見的焦點樣式
- 表單錯誤以 `aria-describedby` 關聯欄位
- 色彩對比符合標準（Light 與 Dark 皆須檢查）
- E2E 以 axe 檢查主要頁面，不得有 serious / critical 問題

### 10.4 Phase 1 頁面
| 路徑 | 說明 |
|---|---|
| `/` | 未登入顯示產品介紹與登入 / 註冊入口；已登入導向 `/dashboard` |
| `/login`、`/register` | 登入、註冊 |
| `/register/check-email` | 提示查看驗證信，可重寄 |
| `/confirm-email` | 讀取連結參數並驗證 |
| `/forgot-password`、`/reset-password` | 忘記與重設密碼 |
| `/onboarding` | 多步驟：選考試 → 目標分數 → 考試日期 → 每日時間 → 完成 |
| `/dashboard` | 目標摘要、距離考試天數；後續功能區塊以 Empty State 說明 |
| `/settings` | 個人資料、時區、主題、變更密碼、版本資訊 |
| `/admin` | 摘要、使用者列表、Audit Log（僅 Admin） |
| 403 / 404 頁 | 權限不足、找不到頁面 |

路由守衛：未登入導向 `/login`（保留原目標路徑）；已登入但未完成 Onboarding 時導向 `/onboarding`；非 Admin 進入 `/admin` 顯示 403。

---

## 11. PWA

- 使用 `vite-plugin-pwa` 產生 Manifest 與 Service Worker
- 設計應用圖示：192、512、maskable、Apple Touch Icon、favicon
- 支援安裝至主畫面
- 靜態資源預先快取；SPA 導覽回退到 `index.html`，但排除 `/api` 與 `/health`
- `/api/*` 一律 NetworkOnly，**不快取個人資料或考試答案的 API 回應**
- 網路狀態提示（離線橫幅）
- 新版本更新提示（`registerType: 'prompt'`，由使用者確認後重新載入）
- 重要作答資料由伺服器持久化；若日後實作離線暫存，必須設計安全的同步與衝突處理機制，並先提出 ADR

---

## 12. 安全性與品質檢查表

- [ ] 正式環境強制 HTTPS 與 HSTS
- [ ] 安全標頭：`Content-Security-Policy`、`X-Content-Type-Options`、`Referrer-Policy`、`Permissions-Policy`、`frame-ancestors 'none'`
- [ ] CSRF：所有變更狀態的 API 驗證 Antiforgery Token
- [ ] XSS：不使用 `dangerouslySetInnerHTML` 渲染使用者內容；必要時先消毒
- [ ] SQL Injection：只使用 EF Core 參數化查詢，不使用字串拼接 SQL
- [ ] 授權：每個端點都有授權測試；防止越權讀取他人資料與成績
- [ ] Rate Limiting：登入、註冊、忘記密碼（Phase 1）；測驗（Phase 2）；AI（Phase 4）
- [ ] 帳號鎖定與避免帳號列舉
- [ ] 管理者操作寫入 Audit Log
- [ ] 日誌不含敏感資訊（有測試）
- [ ] Secret 不提交到 Git；`.gitignore` 完整
- [ ] 相依套件無已知高風險漏洞（`dotnet list package --vulnerable`、`npm audit --omit=dev`）
- [ ] 無障礙檢查與手機尺寸 E2E 測試
- [ ] 資料庫 Migration 可從零重現
- [ ] 正式評分規則具有單元測試（Phase 2 起）

---

## 13. 開發階段與驗收

**嚴格分階段實作，每次對話只完成一個 Phase。**

### Phase 1 — 基礎建設（版本 1.0.0）

範圍：專案骨架、資料庫、Identity、使用者資料與學習目標、React UI 骨架、前後端 API 連線、PWA 基礎、管理後台入口、測試與 CI。

驗收條件：
1. `docker compose up -d` 啟動 SQL Server 與 Mailpit。
2. 一行指令完成資料庫 Migration，資料庫可從零建立；重複執行 Seed 不產生重複資料。
3. 後端與前端可分別啟動，前端透過 Vite proxy 呼叫 API。
4. 可以註冊 → 在 Mailpit 收到驗證信 → 驗證 Email → 登入 → 完成 Onboarding → 取得個人資料 → 登出。
5. 忘記密碼與重設密碼流程可完成。
6. 未登入呼叫 `/api/users/me` 回傳 401；缺少 CSRF Token 的 POST 被拒絕；Student 呼叫 `/api/admin/*` 回傳 403。
7. 以 user-secrets 設定的 Admin 帳號可進入後台，看到使用者列表與 Audit Log。
8. 介面在手機與桌面尺寸都可正常操作，Light / Dark Mode 可切換。
9. PWA 可安裝；離線時顯示網路狀態提示；API 回應未被 Service Worker 快取。
10. 以下全部通過並附實際結果：`dotnet build`、`dotnet test`（含 Testcontainers 整合測試）、`npm run lint`、`npm run typecheck`、`npm test`、`npm run build`、Playwright E2E。
11. GitHub Actions CI 設定完成（若開發者尚未建立遠端 repo，標示「已撰寫、未在 GitHub 上執行」）。
12. 前後端與 API 回報的版本皆為 1.0.0。

Phase 1 必要測試：
- 單元測試：Validator、`UserExamGoal` 規則（分數範圍與級距、考試日期、每日時間）、UTC 轉換、架構相依規則、前後端版本一致。
- 整合測試（真實 SQL Server + 真實 Migration）：完整註冊驗證登入登出流程、未驗證 Email 不能登入、帳號鎖定、忘記與重設密碼、變更密碼後舊工作階段失效、CSRF、401 / 403、Rate Limit 429、Seed 冪等、Audit Log 寫入、日誌不含敏感資料、學習目標並行衝突回傳 409。
- 前端測試：表單驗證、API client（CSRF Header、401 處理、CSRF 失敗重試）、路由守衛、主題切換。
- E2E：完整註冊到登出流程（透過 Mailpit API 取得驗證信）、手機尺寸底部導航、主要頁面 axe 檢查。

### Phase 2 — 題庫與測驗引擎（版本 2.1.0）
完成題庫、題組、媒體檔、練習、模擬考、自動儲存、交卷、客觀題計分、成績報告、題庫管理後台。

驗收：示範帳號可以完成一次練習與一次模擬考；重整後保留答案；作答期間 API 不洩漏答案；交卷後取得正確報告；重複或並行交卷不會重複計算；逾時自動結束；題目更新不影響歷史成績；計分規則有完整單元測試。

### Phase 3 — 學習系統（版本另行決定）
完成錯題本、單字 SRS、程度診斷、學習計畫、學習分析、學習首頁完整指標與完整後台。

驗收：學習數據可持續累積；錯題與單字複習能正確更新；SRS 排程有單元測試；首頁指標與實際資料一致。

### Phase 4 — AI 功能（版本另行決定）
加入 AI 解題、個人化學習建議、作文批改與口說練習。

驗收：無 API Key 時核心學習功能仍可運作；AI 服務出錯或逾時不影響一般測驗；AI 內容標註為練習參考；Token 與費用有紀錄；有速率限制與不可信輸入防護。

### Phase 5 — 產品化（版本另行決定）
擴充 IELTS / TOEFL、完整 PWA、部署、效能、安全強化與產品化。

### 每個 Phase 的共同要求
1. 先列出實作計畫並等待確認。
2. 完成實際可運作的程式碼，不只建立空介面。
3. 建立必要的 Migration。
4. 撰寫單元與整合測試（含 E2E 主要流程）。
5. 執行 Build 與所有測試，修正到通過。
6. 更新 README、`docs/architecture.md`、`docs/progress.md`、`CLAUDE.md`。
7. 依第 14 節格式回報。
8. 不得虛構已成功執行的測試。

---

## 14. 階段完成報告格式

每個 Phase 結束時，以下列格式回報：

1. **完成項目**：對照驗收條件逐條標示 ✅ 完成 / ⚠️ 部分完成 / ❌ 未完成，並說明原因。
2. **專案架構說明**：本 Phase 新增的模組、主要類別與資料流。
3. **執行方式**：從 clone 到啟動前後端的完整指令。
4. **資料庫建立方式**：Migration 與 Seed 指令、如何重設本機資料庫。
5. **Build 與測試結果**：每個指令與實際結果（通過 / 失敗 / 略過數量）。未執行的項目須標示「未執行」並說明原因。
6. **架構決策**：本 Phase 新增的 ADR 清單。
7. **未解決事項與已知限制**。
8. **需要開發者處理的事項**：例如需要提供的 Secret、需要建立的外部帳號。
9. **下一階段開發建議**。

---

## 附錄 A：`CLAUDE.md` 範本

將下列內容存成專案根目錄的 `CLAUDE.md`。Claude 須在指令或慣例變更時更新它，並保持精簡。

````markdown
# languagelearning

AI 英文檢定學習平台（先做 TOEIC L&R）。ASP.NET Core 10 + EF Core 10 + SQL Server + React/TypeScript/Vite。
完整需求：docs/requirements.md（需求唯一來源）。目前進度：docs/progress.md。

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
- 分層：Api → Application → Domain；Infrastructure 實作 Application 介面。Domain 不相依任何層與框架。
- 模組是各層內的資料夾（Users、Exams…），不另建專案。
- 前後端同源；前端只呼叫相對路徑 /api；Cookie 驗證 + X-XSRF-TOKEN。
- 資料庫時間一律 UTC（欄位以 Utc 結尾），時間來源用 TimeProvider。
- 錯誤回應一律 ProblemDetails；使用者訊息用繁體中文。
- 日誌禁止記錄密碼、Token、Cookie、Email 驗證或重設連結、完整 Email。

## 常用指令（Phase 1 完成後由 Claude 更新為實際指令）
- 啟動依賴服務：docker compose up -d
- 資料庫 Migration：dotnet ef database update -p src/backend/LanguageLearning.Infrastructure -s src/backend/LanguageLearning.Api
- 後端：dotnet build / dotnet test
- 前端（src/frontend）：npm run dev / lint / typecheck / test / build
- E2E（tests/e2e）：npx playwright test

## 版本
目前版本 1.0.0。後端版本只改 Directory.Build.props；前端只改 src/frontend/package.json；兩者必須一致。
````

---

## 附錄 B：Phase 1 啟動指令

在 Plan Mode 中貼上：

```text
請閱讀 CLAUDE.md 與 docs/requirements.md，開始執行 Phase 1（版本 1.0.0）。

1. 先不要寫程式。檢查目前 repository 狀態與開發環境（dotnet --info、node -v、npm -v、docker info、git status），列出缺少或版本不符的工具。若有阻擋項目，先回報並停止。
2. 依 docs/requirements.md 第 2.1 節產出 Phase 1 實作計畫，包含：專案與檔案結構、Entities 與 Migration、API 端點、前端頁面、測試清單、里程碑切分、風險與需要我決定的事項。
3. 等我確認計畫後才開始實作。確認後建立任務清單，依里程碑逐項完成；每個里程碑都要執行 build 與測試，通過後再 commit。
4. 只完成 Phase 1，不建立 Phase 2 之後的任何程式碼、空介面或假頁面。
5. 需要外部憑證時建立安全設定範本並標註由我提供，不填入假密鑰。
6. 完成前對照第 12 節檢查表與第 13 節 Phase 1 驗收條件做自我審查；驗證、授權相關程式碼請另開 subagent 獨立審查。
7. 完成後依第 14 節格式回報，並更新 README.md、docs/architecture.md、docs/progress.md、CLAUDE.md。
```

---

## 附錄 C：Phase 1 開發者驗收步驟

Claude 完成後，請自行執行以下步驟確認（實際指令以 Claude 更新後的 README 為準）：

```bash
# 1. 準備設定
cp .env.example .env                       # 填入 SQL Server SA 密碼
cd src/backend/LanguageLearning.Api
dotnet user-secrets set "Seed:Admin:Email" "你的管理者 Email"
dotnet user-secrets set "Seed:Admin:Password" "你的管理者密碼"
cd ../../..

# 2. 啟動依賴服務與資料庫
docker compose up -d
dotnet tool restore
dotnet ef database update -p src/backend/LanguageLearning.Infrastructure -s src/backend/LanguageLearning.Api

# 3. 執行測試
dotnet test
cd src/frontend && npm ci && npm run lint && npm run typecheck && npm test && npm run build && cd ../..

# 4. 啟動
dotnet run --project src/backend/LanguageLearning.Api
cd src/frontend && npm run dev
```

手動檢查清單：
- [ ] 開啟前端網址，註冊新帳號
- [ ] 開啟 Mailpit（http://localhost:8025）收驗證信並點擊連結
- [ ] 登入後完成 Onboarding（IELTS、TOEFL 顯示尚未開放）
- [ ] 首頁顯示目標分數與距離考試天數
- [ ] 設定頁可修改顯示名稱、切換 Dark Mode，並看到版本 1.0.0
- [ ] 登出後無法進入首頁
- [ ] 忘記密碼 → 收信 → 重設 → 用新密碼登入
- [ ] 以管理者帳號進入 `/admin`，看到使用者列表與 Audit Log
- [ ] 以一般帳號進入 `/admin` 顯示 403
- [ ] 手機尺寸（瀏覽器開發者工具）顯示底部導航
- [ ] 瀏覽器可安裝為 PWA；切換離線時出現提示

---

## 附錄 D：後續 Phase 啟動指令範本

每個 Phase 開新對話（或 `/clear`）後貼上，將 `N` 與版本號替換為實際值：

```text
請閱讀 CLAUDE.md、docs/requirements.md 與 docs/progress.md，開始執行 Phase N（版本 1.X.0）。

1. 先確認上一個 Phase 的狀態：執行 build 與全部測試，回報結果。若有失敗，先列出原因並等我決定是否先修正。
2. 檢視 docs/progress.md 中的未解決事項，說明哪些會影響本 Phase。
3. 依 docs/requirements.md 第 2.1 節產出 Phase N 實作計畫，等我確認後才開始。
4. 只完成 Phase N 範圍，不修改與本 Phase 無關的功能。
5. 版本號更新為 1.X.0（Directory.Build.props 與 src/frontend/package.json）。
6. 完成前對照第 12 節檢查表與第 13 節 Phase N 驗收條件自我審查。
7. 依第 14 節格式回報，並更新 README.md、docs/architecture.md、docs/progress.md、CLAUDE.md。
```

---

## 附錄 E：與原 Codex 版本的差異

| 項目 | 原版本 | 本版本 |
|---|---|---|
| 專案名稱 | AiEnglishAcademy | languagelearning / LanguageLearning |
| 版本 | 未定義 | 1.0.0 起，SemVer，每 Phase 提升 minor |
| 執行者 | Codex | Claude Code（Plan Mode、任務清單、subagent 審查、CLAUDE.md） |
| 工作守則 | 簡要 | 新增第 2 節：驗證迴圈、誠實回報、環境受阻處理、範圍控制、Git 與安全操作 |
| 前後端部署 | 未指定 | 同源；Vite proxy；正式環境反向代理 |
| CSRF | 只寫「CSRF 保護」 | Antiforgery + `X-XSRF-TOKEN`，登入登出後更新 Token |
| Email | 未指定開發方式 | Docker Compose 加入 Mailpit；E2E 透過 Mailpit API 驗證 |
| Admin 帳號 | 未指定 | 只從 user-secrets / 環境變數建立 |
| 初次使用流程 | 未分配 Phase | 目標設定在 Phase 1，程度診斷在 Phase 3 |
| Modular Monolith | 與四層目錄並列 | 模組為各層內資料夾，加入架構測試 |
| 前端單元測試 | `tests/frontend/` | 與原始碼放在一起（套件解析因素） |
| 後端測試專案 | 只有 `tests/backend/` | 分 UnitTests 與 IntegrationTests（Testcontainers） |
| TOEIC 計分 | 依官方考制計分 | 以 ScoringRule 實作並標示「預估分數」；加註商標聲明 |
| 媒體檔 | 未指定儲存 | `IMediaStorage` 抽象層 |
| Entities | 一次列出 | 依 Phase 分配，Phase 1 只建必要資料表 |
| 安全細節 | 原則列舉 | 帳號鎖定、避免帳號列舉、安全標頭、Security Stamp、日誌過濾測試 |
| 驗收 | 各 Phase 一句 | Phase 1 列出 12 項驗收條件與必要測試清單；附開發者驗收步驟 |

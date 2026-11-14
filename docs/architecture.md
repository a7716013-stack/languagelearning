# 架構說明（v2.1.0 開發中 / Phase 2）

## 總覽

```text
瀏覽器（React PWA） ──同源 /api──▶ ASP.NET Core 10 API ──EF Core──▶ SQL Server
        │                                 │                         ▲
   Service Worker                 寄件匣背景服務 ──────────────────┘（EmailOutbox）
  （只快取靜態資源）                      │
                              SMTP（CI：Mailpit）／ 本機開發：HTML 信件檔
```

- 前後端**同源**：開發時由 Vite proxy 轉送 `/api`、`/health`；正式環境由反向代理讓前端與 API 共用網域。
- 驗證採 ASP.NET Core Identity 的 **HttpOnly Cookie**；前端不保存任何憑證或 Token 到 localStorage / sessionStorage。

## 後端分層（Modular Monolith）

| 專案 | 職責 | 可相依 |
|---|---|---|
| `LanguageLearning.Domain` | 實體與領域規則（`ExamType`、`UserExamGoal`、`AuditLog`） | 無（不相依框架） |
| `LanguageLearning.Application` | Use Case、DTO、FluentValidation、介面（`IAppDbContext`、`IEmailSender`、`ICurrentUser`…） | Domain、EF Core 抽象 |
| `LanguageLearning.Infrastructure` | EF Core、Identity、系統信件（寄件匣、SMTP、開發用信件檔）、稽核紀錄、資料庫初始化 | Application、Domain |
| `LanguageLearning.Api` | Minimal API 端點、Middleware、DI 組裝 | Application（Infrastructure 僅用於 DI） |

模組是各層內的資料夾：`Auth`、`Users`、`Exams`、`Goals`、`Admin`。相依方向由 `ArchitectureTests` 檢查。

## 請求處理流程

```text
SerilogRequestLogging → ExceptionHandler（ProblemDetails）→ StatusCodePages
→ HSTS / HTTPS 轉向（正式）→ SecurityHeaders → Authentication
→ RateLimiter → CsrfProtection → Authorization → Endpoint（ValidationFilter → Service）
```

## 橫切關注點

| 項目 | 實作 |
|---|---|
| 錯誤回應 | 一律 ProblemDetails（RFC 9457），含 `code` 與 `traceId`；驗證錯誤為 `errors`（camelCase 欄位） |
| CSRF | `GET /api/auth/csrf` 取得 Token；`CsrfProtectionMiddleware` 驗證所有 `/api` 的 POST/PUT/PATCH/DELETE（Header `X-XSRF-TOKEN`）；Token 綁定使用者，登入 / 登出後前端重新取得 |
| Cookie | `HttpOnly`、`SameSite=Strict`；正式環境 `Secure` + `__Host-` 前綴；API 回 401/403，不轉址 |
| 帳號安全 | 密碼 ≥10 碼含大小寫與數字；5 次失敗鎖定 15 分鐘；需 Email 驗證；避免帳號列舉；變更 / 重設密碼更新 Security Stamp |
| Rate Limiting | `auth` 政策（依 IP）套用於登入、註冊、驗證、密碼相關端點；全域 `/api` 依使用者或 IP 限制；回 429 + `Retry-After` |
| 稽核 | `AuditLogger` 以獨立 DbContext 寫入，`AuditDetailsSanitizer` 遮蔽敏感欄位與 Email |
| 系統信件 | 請求以 `IAccountEmailQueue` 排入 `EmailOutbox`（只存使用者與信件種類）；`EmailOutboxBackgroundService` 寄出時才產生 Token 與連結，失敗重試、用盡後寫稽核 `email.delivery_failed`；`Email:Provider=File` 時存成 HTML 檔（僅限非正式環境）。見 ADR 0006 |
| 日誌 | Serilog；不記錄密碼、Token、Cookie、信件連結、Email（整合測試驗證） |
| 時間 | 資料庫存 UTC（`UtcDateTimeConverter`）、時間來源 `TimeProvider`、「今天」依使用者 IANA 時區計算 |
| 並行控制 | `UserExamGoal.RowVersion`（rowversion）；前端帶 `version`，衝突回 409 |
| 版本 | `Directory.Build.props` → `/api/version`、OpenAPI；前端 `package.json` → 介面頁尾與設定頁 |

## 資料模型（Phase 1）

| 資料表 | 說明 |
|---|---|
| `AspNetUsers`（`ApplicationUser`） | Identity 使用者 + `DisplayName`、`TimeZoneId`、`UiTheme`、`CreatedAtUtc`、`LastLoginAtUtc`、`OnboardingCompletedAtUtc` |
| `AspNetRoles` 等 Identity 資料表 | 角色 `Admin`、`Student`（HasData 固定 Id） |
| `ExamTypes` | TOEIC（開放）、IELTS、TOEFL（尚未開放）；分數範圍與級距 |
| `ExamVersions` | 考制版本（`TOEIC-LR`），Phase 2 的試卷與計分規則會綁定版本 |
| `UserExamGoals` | 使用者學習目標；唯一索引 `(UserId, ExamTypeId)`；rowversion |
| `AuditLogs` | 稽核紀錄；不設外鍵以保留歷史 |
| `EmailOutbox` | 系統信件寄件匣（Phase 2 / F2-01）：種類、使用者、狀態、嘗試次數、下次嘗試時間、遮蔽後的錯誤；不存收件者、內容、連結或 Token；rowversion |

參考資料以 `HasData` 隨 Migration 建立；管理者帳號由 `DatabaseInitializer` 依 `Seed:Admin:*` 設定建立（可重複執行）。

## 前端

| 資料夾 | 內容 |
|---|---|
| `src/app` | Router、Providers（TanStack Query、主題）、版面（AuthLayout、OnboardingLayout、AppLayout）、路由守衛 |
| `src/components` | `ui/`（shadcn/ui 風格元件）、`layout/`（導覽、狀態元件、PWA 更新提示、網路狀態） |
| `src/features` | `auth`、`onboarding`、`dashboard`、`settings`、`exams`、`admin` |
| `src/services` | `api-client.ts`（同源 fetch、CSRF、ProblemDetails → `ApiError`）、`api.ts` |
| `src/hooks`、`src/lib`、`src/types`、`src/styles` | 共用 Hook、工具、DTO 型別、設計 Token |

- 路由拆分：首頁與登入頁直接載入，其餘頁面以 `React.lazy` 拆成獨立 chunk（`src/app/router.tsx`）。
- 路由守衛：`RequireAuth`（未登入導向 `/login?returnTo=`，returnTo 僅允許站內路徑）、`RequireOnboarding`、`RequireRole`、`GuestOnly`。
- 版面：桌面左側導覽、手機底部導航；Light / Dark Mode（偏好存在 localStorage 並同步到使用者設定）。
- PWA：`vite-plugin-pwa`，`registerType: 'prompt'`（新版本提示）；`/api`、`/health` 不走 SPA 回退且 NetworkOnly；正式建置加入 CSP meta。

## 測試策略

| 類型 | 位置 | 重點 |
|---|---|---|
| 單元 | `tests/backend/LanguageLearning.UnitTests` | Domain 規則、Validator、稽核遮蔽、信件範本、UTC 轉換、架構相依、版本一致 |
| 整合 | `tests/backend/LanguageLearning.IntegrationTests` | 真實 SQL Server + 真實 Migration；完整帳號流程、CSRF、401/403、鎖定、429、並行 409、Seed 冪等、日誌不含敏感資料、寄件匣重試與遮蔽 |
| 前端 | `src/frontend/src/**/*.test.ts(x)` | API client（CSRF 重試、錯誤轉換）、表單驗證、路由守衛、主題 |
| E2E | `tests/e2e` | 註冊到登出、忘記密碼、管理後台、手機版、axe 無障礙（關閉動畫）、PWA；信件本機讀檔、CI 讀 Mailpit |

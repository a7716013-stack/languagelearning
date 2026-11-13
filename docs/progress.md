# 開發進度

## Phase 1 — 基礎建設（v1.0.0）

狀態：**建置、單元測試、整合測試已通過；E2E 與 GitHub Actions 尚未執行**（見下方「Build 與測試結果」）。

### 完成項目

- 專案骨架：四層後端、React 前端、測試專案、Docker Compose、GitHub Actions、文件。
- 使用者系統：註冊、Email 驗證、重寄驗證信、登入、登出、忘記 / 重設密碼、變更密碼、Admin / Student 角色、帳號鎖定、避免帳號列舉。
- 安全：Cookie（HttpOnly、SameSite=Strict、正式環境 Secure + `__Host-`）、CSRF、Rate Limiting、安全標頭、稽核紀錄、日誌遮蔽。
- 初次使用流程：選擇檢定（IELTS / TOEFL 顯示尚未開放）、設定目標分數、考試日期、每日學習時間；程度診斷留待 Phase 3。
- 學習首頁：目標分數、考試倒數、每日學習時間；後續功能以 Empty State 說明。
- 設定：個人資料、時區、主題、學習目標、變更密碼、版本資訊。
- 管理後台：摘要、使用者列表（分頁、搜尋）、稽核紀錄（分頁、篩選）。
- PWA：Manifest、圖示、Service Worker、新版本提示、離線提示、API 不快取、正式建置 CSP。

### Build 與測試結果

環境：Windows 11、.NET SDK 10.0.401、Node 24.21.0、npm 11.19.0。本機沒有 Docker 與 Git。執行日期：2026-11-05。

| 項目 | 結果 |
|---|---|
| `dotnet build LanguageLearning.slnx` | **通過**：警告 0、錯誤 0 |
| `dotnet test`（單元） | **通過**：71 通過、0 失敗、0 略過 |
| `dotnet test`（整合） | **通過**：35 通過、0 失敗、0 略過。未使用 Testcontainers，以 `LL_TEST_SQLSERVER` 連線 LocalDB（SQL Server 2025，17.0.4025.3） |
| `dotnet ef migrations add InitialCreate` | **已產生**；`has-pending-model-changes` 回報模型與 Migration 一致；整合測試實際套用過 |
| `npm run lint` / `typecheck` | **通過** |
| `npm test` | **通過**：49 通過（7 個測試檔） |
| `npm run build` | **通過**：有單一 chunk 641 KB 超過 500 KB 的警告 |
| `package-lock.json` | **已產生**：`src/frontend`、`tests/e2e` |
| 實機操作（真實 API + LocalDB + Vite dev server） | **通過**：CSRF 拒絕、錯誤密碼 401、管理者登入、取得個人資料、管理 API、登出後 401；登入頁與首頁畫面正常，手機寬度（375 px）無水平溢出 |
| Playwright E2E | **未執行**：需要 Mailpit（Docker）與 Chromium。僅確認 `playwright test --list` 可載入 18 個測試 |
| 註冊 → 登入 → 進入初次設定（瀏覽器，2026-11-06） | **通過**：本機以 user-secrets 暫時設定 `Auth:RequireConfirmedEmail=false` |
| 驗證信與重設密碼信（瀏覽器完整流程） | **未在瀏覽器執行**：本機沒有 Mailpit。整合測試以替身寄信服務涵蓋 API 流程 |
| PWA 執行期行為（Service Worker 註冊、安裝、離線） | **未驗證**：開發模式不啟用；`npm run build` 確實產生 `sw.js` 與 Manifest |
| Testcontainers、`docker compose` | **未執行**：本機沒有 Docker |
| GitHub Actions | **已撰寫、尚未在 GitHub 上執行** |

### 首次建置時修正的問題

1. **套件弱點**：`System.Security.Cryptography.Pkcs` 10.0.0 有 GHSA-846f-f764-76mx（高）、GHSA-2fmv-rv2c-59x7（中）。已用中央套件管理的傳遞釘選固定為 10.0.14。
2. **稽核紀錄中文被跳脫**：`AuditDetailsSanitizer` 輸出 `\uXXXX`，改用允許 Unicode 的編碼器，HTML 敏感字元仍會跳脫。
3. **日誌安全測試未檢查到內容**：`AddSerilog` 預設使用全域 `Log.Logger`，平行測試的多個主機互相覆寫，測試收到的日誌為空。`Program.cs` 改用 `preserveStaticLogger: true`，測試改以 Serilog sink（`CapturingLogSink`）攔截實際輸出，斷言未放寬。
4. **前端套件版本**：`jsdom@^31.0.0` 不存在，改為 `^30.1.3`。

### 未解決事項 / 已知限制

1. `npm audit` 回報 15 個弱點（低 2、中 13），全在開發工具（ESLint、typescript-eslint、workbox-build 的相依）；`npm audit --omit=dev` 為 0。npm 提供的修法都是降版，因此未套用，待上游更新。
2. 前端主程式 chunk 641 KB，應以路由拆分（code splitting）改善。
3. 整合測試使用 `LL_TEST_SQLSERVER` 時，測試資料庫（`ll_test_*`）不會自動刪除，會累積在該 SQL Server 上。請勿指向共用或正式的執行個體。
4. Data Protection 金鑰使用預設位置；正式部署（多台或容器）需持久化金鑰（Phase 5）。
5. 正式環境的反向代理、Forwarded Headers、`AllowedHosts` 尚未設定（Phase 5）。
6. IELTS / TOEFL 的分數制度須在開放前依官方最新公告確認。
7. 沒有寄信伺服器時，註冊請求要等 SMTP 連線失敗（約 10 秒）才回應，頁面仍顯示「已寄出驗證信」。寄信是同步執行的；正式環境若 SMTP 緩慢或故障，使用者同樣會等待，也收不到信。

### 下一步

1. 安裝 Docker Desktop（或以其他方式提供 Mailpit），執行 Playwright E2E。
2. 安裝 Git、建立 Repository，推送後執行 **CI**。lockfile 與 Migration 已產生，不需要再執行 **Bootstrap generated files**。
3. 通過後建立 tag `v1.0.0`，開始 Phase 2（題庫與測驗引擎）。

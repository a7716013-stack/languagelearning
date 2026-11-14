# 0006：系統信件改用資料庫寄件匣（Outbox），開發環境存成 HTML 檔

- 狀態：已採用（2026-11-14，Phase 2 / F2-01）

## 背景

Phase 1 的驗證信、重設密碼信與「此 Email 已註冊」通知在請求中同步以 SMTP 寄出（見 `docs/progress.md` Phase 1 未解決事項第 7 點）：

- 沒有寄信伺服器時，註冊請求要等 SMTP 連線逾時（約 10 秒）才回應，畫面卻顯示「已寄出驗證信」。
- 正式環境 SMTP 緩慢或故障時，使用者同樣要等待，而且寄失敗就不會再寄。
- 本機沒有 Docker（沒有 Mailpit）時，開發者收不到信，只能暫時關閉 Email 驗證。

開發者決定保留 Email 驗證與忘記密碼寄信，並要求本機開發時把信件存成本機檔案（討論時選擇 HTML 檔，而非 PDF）。

## 決策

### 寄件匣

- 請求只在 `EmailOutbox` 資料表新增一筆「要寄給哪個使用者、哪一種信」，立即回應；由背景服務 `EmailOutboxBackgroundService` 寄出。
- **資料表不保存收件者、信件內容、連結或 Token**。寄出時才由 `AccountEmailComposer` 依使用者目前的資料產生 Token 與連結，因此資料庫外洩不會洩漏可用的驗證或重設連結；Token 有效期限從實際寄出時起算。
- 寄出時使用者狀態已改變則不寄（狀態 `Skipped`）：例如驗證信排隊期間使用者已完成驗證，或重設密碼信的使用者尚未驗證。
- 同一使用者、同一種類已有尚未寄出的信件（`Pending` / `Processing`）時不重複排入。
- 失敗重試：第 1～4 次失敗後分別等待 `RetryBaseDelaySeconds` 的 1、5、15、30 倍（預設 60 秒）；達 `MaxAttempts`（預設 5）次後標記 `Failed`，寫入稽核紀錄 `email.delivery_failed` 與錯誤日誌，不再重試。
- 錯誤訊息以 `AuditDetailsSanitizer.MaskEmail` 遮蔽後才存入 `LastError`（最多 500 字）；日誌只記錄例外類型，不記錄例外本身（SMTP 例外訊息可能含收件者地址）。
- 多個執行個體：以 `RowVersion`（rowversion）搶占信件，同一封信只會由一個執行個體處理。處理中超過 `StaleProcessingSeconds`（預設 300 秒）仍未完成的信件（例如程式中途停止）會重新處理。
- 新信件排入時以 `EmailOutboxSignal` 立即喚醒背景服務；另外每 `PollIntervalSeconds`（預設 15 秒）輪詢一次。

### 開發環境的信件檔

- `Email:Provider` 可設為 `Smtp`（預設，正式環境；CI 使用 Mailpit）或 `File`。
- `appsettings.Development.json` 設為 `File`：信件存成 `src/backend/LanguageLearning.Api/App_Data/mail/*.html`，用瀏覽器開啟即可點擊連結。檔案開頭的 HTML 註解記錄收件者、主旨與時間。
- `App_Data/` 已列入 `.gitignore`。信件檔含有效的驗證或重設連結，**不得提交或分享**。
- 正式環境（`Production`）設定為 `File` 時，設定驗證失敗，程式無法啟動。

## 影響

- 註冊、重寄驗證信、忘記密碼的回應時間不再受 SMTP 影響；SMTP 故障時信件會延後寄出，而不是遺失。
- 寄送語意為「至少一次」：信件已寄出、但寫回狀態前程式停止時，可能在逾時後再寄一次。
- 管理者可由稽核紀錄看到寄送失敗（`email.delivery_failed`）。
- 測試：整合測試改為等待背景服務寄出信件（`CapturingEmailSender.WaitForLinkQueryAsync`），另以 `EmailOutboxTests` 驗證重試、遮蔽、不重複、重試用盡與欄位限制。E2E 在本機讀取信件檔（不需 Mailpit），CI 仍以 SMTP 寄到 Mailpit。
- 新增 Migration `AddEmailOutbox`。

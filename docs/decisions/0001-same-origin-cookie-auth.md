# 0001：同源部署 + Cookie 驗證 + CSRF Token

- 狀態：已採用（Phase 1）

## 背景
需求要求 Cookie 驗證、CSRF 保護，且不得在 localStorage 保存憑證。

## 決策
- 前後端同源：開發用 Vite proxy，正式用反向代理。不使用跨域 CORS 加憑證。
- 使用 ASP.NET Core Identity 的 Application Cookie（HttpOnly、SameSite=Strict；正式環境 Secure + `__Host-`）。
- 自訂 `/api/auth/*` 端點，不使用 `MapIdentityApi`（其路由、角色與錯誤格式無法完整符合需求）。
- CSRF：Antiforgery Token 由 `GET /api/auth/csrf` 取得，前端存於記憶體，放在 `X-XSRF-TOKEN` Header；`CsrfProtectionMiddleware` 驗證所有 `/api` 的變更請求（含登入、註冊）。

## 影響
- 前端只能使用相對路徑呼叫 API。
- 登入 / 登出後必須重新取得 CSRF Token（Token 綁定使用者身分）；前端遇到 `csrf_invalid` 會自動重新取得並重試一次。

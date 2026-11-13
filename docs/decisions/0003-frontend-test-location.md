# 0003：前端單元測試與原始碼放在一起

- 狀態：已採用（Phase 1）

## 背景
原始規劃為 `tests/frontend/`，但 `node_modules` 位於 `src/frontend/`，放在外層目錄時 Vitest 無法解析套件。

## 決策
- Vitest 測試放在 `src/frontend/src/**/*.test.ts(x)`。
- E2E 維持在 `tests/e2e/`（獨立 `package.json`）。
- 後端測試分為 `tests/backend/LanguageLearning.UnitTests` 與 `LanguageLearning.IntegrationTests`。

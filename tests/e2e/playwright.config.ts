import { defineConfig, devices } from '@playwright/test'

/**
 * E2E 測試需要：SQL Server（docker compose）、Mailpit（docker compose）。
 * 預設由 Playwright 啟動 API（http://localhost:5080）與前端 preview（http://localhost:4173，含 Service Worker 與 CSP）。
 * 若已自行啟動服務，設定 E2E_SKIP_WEBSERVER=1。
 */
const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:4173'
const connectionString = process.env.E2E_CONNECTION_STRING
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@languagelearning.test'

export default defineConfig({
  testDir: './specs',
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  workers: 1,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['list'], ['github'], ['html', { open: 'never' }]] : [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL,
    locale: 'zh-TW',
    timezoneId: 'Asia/Taipei',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'desktop-chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'mobile-chromium', use: { ...devices['Pixel 7'] }, testMatch: /mobile\.spec\.ts/ },
  ],
  webServer: process.env.E2E_SKIP_WEBSERVER
    ? undefined
    : [
        {
          command: 'dotnet run --project ../../src/backend/LanguageLearning.Api --no-launch-profile',
          url: 'http://localhost:5080/health/ready',
          timeout: 240_000,
          reuseExistingServer: !process.env.CI,
          stdout: 'pipe',
          env: {
            ASPNETCORE_ENVIRONMENT: 'Development',
            ASPNETCORE_URLS: 'http://localhost:5080',
            ...(connectionString ? { ConnectionStrings__Default: connectionString } : {}),
            App__PublicBaseUrl: baseURL,
            App__UseHttpsRedirection: 'false',
            Database__ApplyMigrationsOnStartup: 'true',
            Email__Smtp__Host: process.env.E2E_SMTP_HOST ?? 'localhost',
            Email__Smtp__Port: process.env.E2E_SMTP_PORT ?? '1025',
            RateLimiting__Auth__PermitLimit: '1000',
            Seed__Admin__Email: adminEmail,
            Seed__Admin__Password: process.env.E2E_ADMIN_PASSWORD ?? '',
          },
        },
        {
          command: 'npm run build && npm run preview',
          cwd: '../../src/frontend',
          url: baseURL,
          timeout: 240_000,
          reuseExistingServer: !process.env.CI,
        },
      ],
})

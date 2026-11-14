import { waitForFileEmailLink } from './mailFiles'
import { waitForMailpitLink } from './mailpit'

/**
 * 取得系統信件中的連結。
 * - 設定 E2E_MAIL_DIR（本機預設）：讀取 API 存成檔案的信件。
 * - E2E_MAIL_MODE=mailpit（CI）：從 Mailpit 讀取實際以 SMTP 寄出的信件。
 */
export async function waitForEmailLink(email: string, linkPath: string): Promise<string> {
  const mailDir = process.env.E2E_MAIL_DIR
  if (process.env.E2E_MAIL_MODE !== 'mailpit' && mailDir) {
    return waitForFileEmailLink(mailDir, email, linkPath)
  }

  return waitForMailpitLink(email, linkPath)
}

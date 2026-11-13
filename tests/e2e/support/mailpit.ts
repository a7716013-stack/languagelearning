import { expect } from '@playwright/test'

const MAILPIT_URL = process.env.E2E_MAILPIT_URL ?? 'http://localhost:8025'

interface MailpitSearchResult {
  messages: { ID: string; Subject: string; To: { Address: string }[] }[]
}

interface MailpitMessage {
  Text: string
}

/** 等待寄給指定 Email、且包含指定路徑連結的信件，回傳連結。 */
export async function waitForEmailLink(email: string, path: string): Promise<string> {
  let link: string | undefined

  await expect
    .poll(
      async () => {
        const search = await fetch(`${MAILPIT_URL}/api/v1/search?query=${encodeURIComponent(`to:"${email}"`)}`)
        if (!search.ok) return undefined
        const result = (await search.json()) as MailpitSearchResult
        for (const message of result.messages) {
          const detail = await fetch(`${MAILPIT_URL}/api/v1/message/${message.ID}`)
          if (!detail.ok) continue
          const body = (await detail.json()) as MailpitMessage
          const match = body.Text.match(new RegExp(`https?://[^\\s]+${path.replace('/', '\\/')}\\?[^\\s]+`))
          if (match) {
            link = match[0]
            return link
          }
        }
        return undefined
      },
      { message: `等待寄給 ${email} 的 ${path} 信件`, timeout: 30_000, intervals: [500, 1000, 2000] },
    )
    .toBeTruthy()

  return link!
}

import { readdir, readFile } from 'node:fs/promises'
import path from 'node:path'
import { expect } from '@playwright/test'

/**
 * 本機開發模式（Email:Provider=File）：API 不寄信，而是把信件存成 HTML 檔。
 * 檔案開頭的 HTML 註解記錄收件者（To:），內文含驗證或重設連結。
 */
export async function waitForFileEmailLink(mailDir: string, email: string, linkPath: string): Promise<string> {
  let link: string | undefined

  await expect
    .poll(
      async () => {
        link = await findLink(mailDir, email, linkPath)
        return link
      },
      { message: `等待寄給 ${email} 的 ${linkPath} 信件檔`, timeout: 30_000, intervals: [250, 500, 1000] },
    )
    .toBeTruthy()

  return link!
}

async function findLink(mailDir: string, email: string, linkPath: string): Promise<string | undefined> {
  let names: string[]
  try {
    names = await readdir(mailDir)
  } catch {
    return undefined
  }

  // 檔名以 UTC 時間開頭，倒序即為最新的信件在前。
  const files = names.filter((name) => name.endsWith('.html')).sort().reverse()
  for (const name of files) {
    const content = await readFile(path.join(mailDir, name), 'utf8')
    const to = /^To:\s*(.+)$/m.exec(content)?.[1]?.trim()
    if (to?.toLowerCase() !== email.toLowerCase()) continue

    for (const match of content.matchAll(/href="([^"]+)"/g)) {
      const href = decodeHtml(match[1])
      if (new URL(href).pathname === linkPath) return href
    }
  }

  return undefined
}

function decodeHtml(value: string): string {
  return value
    .replace(/&amp;/g, '&')
    .replace(/&#x27;|&#39;/g, "'")
    .replace(/&quot;/g, '"')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
}

import { useState } from 'react'
import { RouterProvider } from 'react-router/dom'
import { UpdatePrompt } from '@/components/layout/UpdatePrompt'
import { AppProviders } from './providers/AppProviders'
import { createAppRouter } from './router'

export function App() {
  const [router] = useState(createAppRouter)
  return (
    <AppProviders>
      <RouterProvider router={router} />
      {/* 在所有頁面註冊 Service Worker，並於有新版本時提示更新。 */}
      <UpdatePrompt />
    </AppProviders>
  )
}

import { Toaster as Sonner, type ToasterProps } from 'sonner'
import { useTheme } from '@/app/providers/theme-context'

export function Toaster(props: ToasterProps) {
  const { resolvedTheme } = useTheme()
  return (
    <Sonner
      theme={resolvedTheme}
      position="top-center"
      richColors
      closeButton
      toastOptions={{ duration: 4000 }}
      {...props}
    />
  )
}

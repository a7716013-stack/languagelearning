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
      toastOptions={{
        duration: 4000,
        style: {
          '--success-text': resolvedTheme === 'dark' ? '#59f3a6' : '#006b24',
        } as React.CSSProperties,
      }}
      {...props}
    />
  )
}

import * as React from 'react'
import { cn } from '@/lib/utils'
import { Label } from './label'

/** 產生欄位的無障礙屬性：錯誤訊息以 aria-describedby 關聯。 */
export function fieldProps(id: string, error?: string, hasDescription = false) {
  const describedBy = [hasDescription ? `${id}-description` : null, error ? `${id}-error` : null]
    .filter(Boolean)
    .join(' ')
  return {
    id,
    'aria-invalid': error ? true : undefined,
    'aria-describedby': describedBy || undefined,
  } as const
}

interface FormFieldProps {
  id: string
  label: React.ReactNode
  error?: string
  description?: React.ReactNode
  className?: string
  children: React.ReactNode
}

export function FormField({ id, label, error, description, className, children }: FormFieldProps) {
  return (
    <div className={cn('grid gap-2', className)}>
      <Label htmlFor={id}>{label}</Label>
      {children}
      {description ? (
        <p id={`${id}-description`} className="text-xs text-muted-foreground">
          {description}
        </p>
      ) : null}
      {error ? (
        <p id={`${id}-error`} className="text-sm text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  )
}

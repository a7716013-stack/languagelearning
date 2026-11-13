import { Link } from 'react-router'
import { cn } from '@/lib/utils'

export function BrandLogo({ className, to = '/' }: { className?: string; to?: string }) {
  return (
    <Link to={to} className={cn('flex items-center gap-2 rounded-md font-semibold tracking-tight', className)}>
      <img src="/favicon.svg" alt="" className="size-8" width={32} height={32} />
      <span className="text-base">
        Language<span className="text-primary">Learning</span>
      </span>
    </Link>
  )
}

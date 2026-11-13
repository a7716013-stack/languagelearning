export function SkipLink() {
  return (
    <a
      href="#main-content"
      className="sr-only z-[60] rounded-md bg-primary px-4 py-2 text-primary-foreground focus:not-sr-only focus:fixed focus:top-3 focus:left-3"
    >
      跳到主要內容
    </a>
  )
}

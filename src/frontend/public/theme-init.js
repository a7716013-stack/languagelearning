;(function () {
  try {
    var stored = window.localStorage.getItem('ll-theme')
    var theme = stored === 'light' || stored === 'dark' ? stored : 'system'
    var dark =
      theme === 'dark' ||
      (theme === 'system' && window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches)
    document.documentElement.classList.toggle('dark', dark)
    document.documentElement.style.colorScheme = dark ? 'dark' : 'light'
  } catch (e) {
    // 無法存取 localStorage 時使用預設（淺色）主題。
  }
})()

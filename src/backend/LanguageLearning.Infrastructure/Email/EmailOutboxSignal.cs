namespace LanguageLearning.Infrastructure.Email;

/// <summary>新信件排入寄件匣時喚醒背景服務，不必等到下一次輪詢。</summary>
public sealed class EmailOutboxSignal : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(0, 1);

    public void Notify()
    {
        // 已有待處理的喚醒訊號時不重複釋放。
        if (_semaphore.CurrentCount == 0)
        {
            try
            {
                _semaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                // 另一個執行緒剛好已釋放，忽略即可。
            }
        }
    }

    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        _semaphore.WaitAsync(timeout, cancellationToken);

    public void Dispose() => _semaphore.Dispose();
}

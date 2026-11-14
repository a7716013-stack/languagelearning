using LanguageLearning.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LanguageLearning.Infrastructure.Email;

/// <summary>定期（或收到新信件通知時）處理寄件匣。</summary>
internal sealed class EmailOutboxBackgroundService(
    EmailOutboxDispatcher dispatcher,
    EmailOutboxSignal signal,
    IOptions<EmailOptions> options,
    ILogger<EmailOutboxBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.Value.Outbox.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 一次處理完所有到期的信件。
                while (await dispatcher.ProcessDueAsync(stoppingToken) > 0)
                {
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "處理寄件匣時發生錯誤，將於下次輪詢重試。");
            }

            try
            {
                await signal.WaitAsync(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

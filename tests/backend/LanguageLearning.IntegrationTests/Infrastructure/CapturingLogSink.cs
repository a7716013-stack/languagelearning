using System.Collections.Concurrent;
using System.Globalization;
using Serilog.Core;
using Serilog.Events;

namespace LanguageLearning.IntegrationTests.Infrastructure;

/// <summary>
/// 收集應用程式實際寫出的日誌，用於檢查敏感資料不會被寫入日誌。
/// 以 Serilog sink 註冊（Program.cs 透過 ReadFrom.Services 載入），因此內容與正式環境的日誌管線相同，
/// 包含訊息、所有結構化屬性與例外。
/// </summary>
public sealed class CapturingLogSink : ILogEventSink
{
    private readonly ConcurrentQueue<string> _entries = new();

    public IReadOnlyList<string> Entries => _entries.ToList();

    public void Emit(LogEvent logEvent)
    {
        var properties = string.Join(" ", logEvent.Properties.Select(pair => $"{pair.Key}={pair.Value}"));
        _entries.Enqueue(
            $"[{logEvent.Level}] {logEvent.RenderMessage(CultureInfo.InvariantCulture)} {properties} {logEvent.Exception}");
    }
}

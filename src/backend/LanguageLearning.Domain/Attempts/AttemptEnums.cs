namespace LanguageLearning.Domain.Attempts;

/// <summary>作答模式（ADR 0007）。</summary>
public enum AttemptMode
{
    Practice = 1,
    MockExam = 2,
    ReadingTest = 3,
}

public enum AttemptStatus
{
    InProgress = 1,
    Completed = 2,
}

public enum CompletionReason
{
    Submitted = 1,
    TimedOut = 2,

    /// <summary>練習中途放棄。</summary>
    Abandoned = 3,
}

public static class AttemptRules
{
    /// <summary>截止後的寬限時間：網路延遲送達的答案仍會接受，逾時才自動交卷。</summary>
    public static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(5);

    public const int PracticeCriteriaMaxLength = 1000;
    public const int MaxQuestions = 200;

    /// <summary>單題約略作答時間的上限（毫秒）。</summary>
    public const int MaxElapsedMs = 3 * 60 * 60 * 1000;
}

using LanguageLearning.Domain.Common;
using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.QuestionBank;

namespace LanguageLearning.Domain.Attempts;

/// <summary>
/// 一次作答：練習、完整模擬考或閱讀測驗共用（ADR 0007）。
/// 狀態只能從作答中變成已完成；完成時計算成績並保存，之後不再重算。
/// 計時以伺服器時間為準，截止後有 <see cref="AttemptRules.GracePeriod"/> 的寬限。
/// 操作前須載入 <see cref="Questions"/> 與 <see cref="Answers"/>。
/// </summary>
public sealed class ExamAttempt
{
    private readonly List<AttemptQuestionSnapshot> _questions = [];
    private readonly List<AttemptAnswer> _answers = [];

    private ExamAttempt()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public AttemptMode Mode { get; private set; }

    /// <summary>模擬考與閱讀測驗使用的試卷；練習為 null。</summary>
    public Guid? ExamPaperId { get; private set; }

    public Guid ScoringRuleId { get; private set; }

    public AttemptStatus Status { get; private set; }

    public CompletionReason? CompletionReason { get; private set; }

    /// <summary>模擬考目前的段落；練習為 null。進入閱讀後不能回到聽力。</summary>
    public ExamSection? CurrentSection { get; private set; }

    public int? ReadingTimeLimitSeconds { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? ReadingStartedAtUtc { get; private set; }

    public DateTime? ListeningDeadlineUtc { get; private set; }

    public DateTime? ReadingDeadlineUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>最後一次存答或段落變更的時間；每次存答都會更新，讓同一場作答的並行修改產生衝突。</summary>
    public DateTime LastActivityAtUtc { get; private set; }

    /// <summary>練習的篩選條件（JSON），供歷史紀錄顯示。</summary>
    public string? PracticeCriteria { get; private set; }

    public int TotalQuestions { get; private set; }

    public int CorrectCount { get; private set; }

    public int ListeningTotal { get; private set; }

    public int ListeningCorrect { get; private set; }

    public int ReadingTotal { get; private set; }

    public int ReadingCorrect { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<AttemptQuestionSnapshot> Questions => _questions;

    public IReadOnlyCollection<AttemptAnswer> Answers => _answers;

    /// <summary>整場作答的最後期限（尚未進入閱讀時，以聽力截止加上閱讀時間推算）；練習為 null。</summary>
    public DateTime? FinalDeadlineUtc =>
        Mode == AttemptMode.Practice
            ? null
            : ReadingDeadlineUtc ?? ListeningDeadlineUtc?.AddSeconds(ReadingTimeLimitSeconds ?? 0);

    public static ExamAttempt StartPractice(
        Guid userId,
        Guid scoringRuleId,
        string? practiceCriteria,
        IReadOnlyList<AttemptQuestionInfo> questions,
        DateTime nowUtc)
    {
        Guard.NotEmpty(scoringRuleId, nameof(scoringRuleId));
        var criteria = Guard.Optional(practiceCriteria);
        if (criteria is { Length: > AttemptRules.PracticeCriteriaMaxLength })
        {
            throw new ArgumentException("練習條件過長。", nameof(practiceCriteria));
        }

        var attempt = CreateBase(userId, AttemptMode.Practice, null, scoringRuleId, questions, nowUtc);
        attempt.PracticeCriteria = criteria;
        return attempt;
    }

    public static ExamAttempt StartExam(
        Guid userId,
        ExamPaper paper,
        IReadOnlyList<AttemptQuestionInfo> questions,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(paper);
        ArgumentNullException.ThrowIfNull(questions);

        if (paper.Status != ContentStatus.Published)
        {
            throw new DomainException("paper_not_available", "此試卷目前無法作答。");
        }

        if (questions.Count != paper.TotalQuestions)
        {
            throw new ArgumentException("題目數量與試卷不符。", nameof(questions));
        }

        var mode = paper.Kind == ExamPaperKind.FullTest ? AttemptMode.MockExam : AttemptMode.ReadingTest;
        var attempt = CreateBase(userId, mode, paper.Id, paper.DefaultScoringRuleId, questions, nowUtc);
        attempt.ReadingTimeLimitSeconds = paper.ReadingTimeLimitSeconds;

        if (mode == AttemptMode.MockExam)
        {
            if (paper.ListeningDurationMs is not { } listeningMs || listeningMs <= 0)
            {
                throw new ArgumentException("完整模擬考須有聽力音檔總長。", nameof(paper));
            }

            var firstReading = questions.ToList().FindIndex(q => q.Section == ExamSection.Reading);
            if (firstReading >= 0 && questions.Skip(firstReading).Any(q => q.Section == ExamSection.Listening))
            {
                throw new ArgumentException("聽力題目必須排在閱讀題目之前。", nameof(questions));
            }

            attempt.CurrentSection = ExamSection.Listening;
            attempt.ListeningDeadlineUtc = nowUtc.AddMilliseconds(listeningMs);
        }
        else
        {
            if (questions.Any(q => q.Section != ExamSection.Reading))
            {
                throw new ArgumentException("閱讀測驗只能包含閱讀題目。", nameof(questions));
            }

            attempt.EnterReading(nowUtc);
        }

        return attempt;
    }

    public bool IsOverdue(DateTime nowUtc) =>
        Status == AttemptStatus.InProgress && FinalDeadlineUtc is { } deadline && nowUtc > deadline + AttemptRules.GracePeriod;

    /// <summary>聽力時間結束時自動進入閱讀（閱讀從聽力截止時開始計時）。回傳是否有變更。</summary>
    public bool AdvanceClock(DateTime nowUtc)
    {
        Guard.Utc(nowUtc, nameof(nowUtc));
        if (Status != AttemptStatus.InProgress
            || Mode != AttemptMode.MockExam
            || CurrentSection != ExamSection.Listening
            || ListeningDeadlineUtc is not { } listeningDeadline
            || nowUtc < listeningDeadline)
        {
            return false;
        }

        EnterReading(listeningDeadline);
        LastActivityAtUtc = nowUtc;
        return true;
    }

    /// <summary>提早結束聽力、進入閱讀。剩下的聽力題目之後不能再作答。</summary>
    public bool StartReading(DateTime nowUtc)
    {
        EnsureInProgress();
        if (Mode != AttemptMode.MockExam)
        {
            throw new DomainException("not_a_mock_exam", "只有完整模擬考有聽力與閱讀段落。");
        }

        AdvanceClock(nowUtc);
        if (CurrentSection == ExamSection.Reading)
        {
            return false;
        }

        EnterReading(nowUtc);
        LastActivityAtUtc = nowUtc;
        return true;
    }

    /// <summary>
    /// 保存一題的作答。回傳 false 表示序號較舊而被忽略（多分頁時以最後送出的為準）。
    /// 逾時（超過寬限）時拋出 <see cref="DomainException"/>，呼叫端應改為結算。
    /// </summary>
    public bool SaveAnswer(
        Guid questionId,
        QuestionOption? option,
        long clientSequence,
        bool markedForReview,
        int? elapsedMs,
        DateTime nowUtc)
    {
        EnsureInProgress();
        AdvanceClock(nowUtc);

        if (IsOverdue(nowUtc))
        {
            throw new DomainException("attempt_time_expired", "作答時間已結束。");
        }

        if (clientSequence < 0)
        {
            throw new DomainException("invalid_sequence", "作答序號不正確。", "clientSequence");
        }

        var snapshot = _questions.FirstOrDefault(q => q.QuestionId == questionId)
            ?? throw new DomainException("question_not_in_attempt", "這一題不在本次作答中。", "questionId");

        if (option is not null && option.QuestionId != questionId)
        {
            throw new DomainException("invalid_option", "選項不屬於這一題。", "optionId");
        }

        EnsureSectionOpen(snapshot, nowUtc);

        var elapsed = elapsedMs is { } value ? Math.Clamp(value, 0, AttemptRules.MaxElapsedMs) : (int?)null;
        var answer = _answers.FirstOrDefault(a => a.SnapshotId == snapshot.Id);
        if (answer is null)
        {
            _answers.Add(AttemptAnswer.Create(Id, snapshot.Id, option?.Id, clientSequence, markedForReview, elapsed, nowUtc));
        }
        else
        {
            if (clientSequence <= answer.ClientSequence)
            {
                return false;
            }

            answer.Update(option?.Id, clientSequence, markedForReview, elapsed, nowUtc);
        }

        LastActivityAtUtc = nowUtc;
        return true;
    }

    /// <summary>交卷。重複交卷不會改變成績，回傳 false。超過寬限才交卷視為逾時。</summary>
    public bool Submit(DateTime nowUtc)
    {
        Guard.Utc(nowUtc, nameof(nowUtc));
        if (Status == AttemptStatus.Completed)
        {
            return false;
        }

        AdvanceClock(nowUtc);
        if (IsOverdue(nowUtc))
        {
            Complete(Attempts.CompletionReason.TimedOut, FinalDeadlineUtc!.Value);
        }
        else
        {
            Complete(Attempts.CompletionReason.Submitted, nowUtc);
        }

        return true;
    }

    /// <summary>逾時自動交卷（使用者回來時或背景服務掃描時呼叫）。回傳是否有結算。</summary>
    public bool ExpireIfOverdue(DateTime nowUtc)
    {
        Guard.Utc(nowUtc, nameof(nowUtc));
        if (Status == AttemptStatus.Completed)
        {
            return false;
        }

        AdvanceClock(nowUtc);
        if (!IsOverdue(nowUtc))
        {
            return false;
        }

        Complete(Attempts.CompletionReason.TimedOut, FinalDeadlineUtc!.Value);
        return true;
    }

    /// <summary>放棄練習（只有練習可以放棄；已作答的題目仍會計分）。</summary>
    public bool Abandon(DateTime nowUtc)
    {
        Guard.Utc(nowUtc, nameof(nowUtc));
        if (Mode != AttemptMode.Practice)
        {
            throw new DomainException("cannot_abandon_exam", "模擬考不能放棄，請交卷。");
        }

        if (Status == AttemptStatus.Completed)
        {
            return false;
        }

        Complete(Attempts.CompletionReason.Abandoned, nowUtc);
        return true;
    }

    private static ExamAttempt CreateBase(
        Guid userId,
        AttemptMode mode,
        Guid? examPaperId,
        Guid scoringRuleId,
        IReadOnlyList<AttemptQuestionInfo> questions,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(questions);
        Guard.Utc(nowUtc, nameof(nowUtc));
        Guard.NotEmpty(userId, nameof(userId));

        if (questions.Count is 0 or > AttemptRules.MaxQuestions)
        {
            throw new DomainException(
                "invalid_question_count",
                $"每次作答須有 1 到 {AttemptRules.MaxQuestions} 題。");
        }

        if (questions.Select(q => q.QuestionId).Distinct().Count() != questions.Count)
        {
            throw new ArgumentException("題目不可重複。", nameof(questions));
        }

        if (questions.Any(q => q.QuestionId == Guid.Empty || q.GroupVersionId == Guid.Empty
            || q.QuestionGroupId == Guid.Empty || q.CorrectOptionId == Guid.Empty))
        {
            throw new ArgumentException("題目資料不完整。", nameof(questions));
        }

        var attempt = new ExamAttempt
        {
            Id = Guid.CreateVersion7(nowUtc),
            UserId = userId,
            Mode = mode,
            ExamPaperId = examPaperId,
            ScoringRuleId = scoringRuleId,
            Status = AttemptStatus.InProgress,
            StartedAtUtc = nowUtc,
            LastActivityAtUtc = nowUtc,
            TotalQuestions = questions.Count,
            ListeningTotal = questions.Count(q => q.Section == ExamSection.Listening),
            ReadingTotal = questions.Count(q => q.Section == ExamSection.Reading),
        };

        for (var i = 0; i < questions.Count; i++)
        {
            attempt._questions.Add(AttemptQuestionSnapshot.Create(attempt.Id, i + 1, questions[i], nowUtc));
        }

        return attempt;
    }

    private void EnterReading(DateTime readingStartUtc)
    {
        CurrentSection = ExamSection.Reading;
        ReadingStartedAtUtc = readingStartUtc;
        ReadingDeadlineUtc = readingStartUtc.AddSeconds(ReadingTimeLimitSeconds ?? ToeicBlueprint.ReadingTimeLimitSeconds);
    }

    private void EnsureSectionOpen(AttemptQuestionSnapshot snapshot, DateTime nowUtc)
    {
        if (Mode != AttemptMode.MockExam)
        {
            return;
        }

        if (snapshot.Section == ExamSection.Reading && CurrentSection == ExamSection.Listening)
        {
            throw new DomainException("section_not_started", "閱讀段落尚未開始。");
        }

        if (snapshot.Section == ExamSection.Listening && CurrentSection == ExamSection.Reading)
        {
            // 聽力時間自然結束時，寬限期內送達的聽力答案仍接受；提早進入閱讀則聽力答案立即鎖定。
            var endedNaturally = ListeningDeadlineUtc is { } deadline && ReadingStartedAtUtc == deadline;
            if (!endedNaturally || nowUtc > ListeningDeadlineUtc!.Value + AttemptRules.GracePeriod)
            {
                throw new DomainException("section_closed", "聽力段落已結束，不能再修改聽力答案。");
            }
        }
    }

    private void Complete(CompletionReason reason, DateTime completedAtUtc)
    {
        var correct = 0;
        var listeningCorrect = 0;
        var readingCorrect = 0;

        foreach (var snapshot in _questions)
        {
            var answer = _answers.FirstOrDefault(a => a.SnapshotId == snapshot.Id);
            var isCorrect = answer?.SelectedOptionId is { } selected && selected == snapshot.CorrectOptionId;
            answer?.MarkScored(isCorrect);
            if (!isCorrect)
            {
                continue;
            }

            correct++;
            if (snapshot.Section == ExamSection.Listening)
            {
                listeningCorrect++;
            }
            else
            {
                readingCorrect++;
            }
        }

        CorrectCount = correct;
        ListeningCorrect = listeningCorrect;
        ReadingCorrect = readingCorrect;
        Status = AttemptStatus.Completed;
        CompletionReason = reason;
        CompletedAtUtc = completedAtUtc;
    }

    private void EnsureInProgress()
    {
        if (Status != AttemptStatus.InProgress)
        {
            throw new DomainException("attempt_completed", "這次作答已經結束。");
        }
    }
}

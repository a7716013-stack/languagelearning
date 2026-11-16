using LanguageLearning.Domain.Attempts;
using LanguageLearning.Domain.Common;
using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.QuestionBank;
using LanguageLearning.Infrastructure.Persistence;
using static LanguageLearning.UnitTests.QuestionBankTestData;

namespace LanguageLearning.UnitTests.Domain;

public sealed class ExamAttemptTests
{
    private static readonly TimeSpan ReadingLimit = TimeSpan.FromSeconds(ToeicBlueprint.ReadingTimeLimitSeconds);
    private static readonly TimeSpan ListeningLength = TimeSpan.FromMinutes(45);

    [Fact]
    public void Practice_saves_answers_scores_once_and_becomes_immutable()
    {
        var (attempt, part5, part6) = StartPractice();
        var q1 = part5.Questions.Single();
        var q2 = part6.Questions.OrderBy(q => q.Number).First();

        Assert.Equal(AttemptStatus.InProgress, attempt.Status);
        Assert.Equal(5, attempt.TotalQuestions);
        Assert.Equal(Enumerable.Range(1, 5), attempt.Questions.Select(q => q.Sequence).Order());
        Assert.Null(attempt.FinalDeadlineUtc);

        Assert.True(attempt.SaveAnswer(q1.Id, q1.CorrectOption, 1, false, 12_000, NowUtc.AddSeconds(10)));
        Assert.True(attempt.SaveAnswer(q2.Id, Wrong(q2), 2, true, null, NowUtc.AddSeconds(20)));

        Assert.True(attempt.Submit(NowUtc.AddMinutes(5)));

        Assert.Equal(AttemptStatus.Completed, attempt.Status);
        Assert.Equal(CompletionReason.Submitted, attempt.CompletionReason);
        Assert.Equal(NowUtc.AddMinutes(5), attempt.CompletedAtUtc);
        Assert.Equal(1, attempt.CorrectCount);
        Assert.Equal(1, attempt.ReadingCorrect);
        Assert.Equal(5, attempt.ReadingTotal);
        Assert.Equal(0, attempt.ListeningTotal);
        Assert.True(attempt.Answers.Single(a => a.SelectedOptionId == q1.CorrectOption!.Id).IsCorrect);
        Assert.False(attempt.Answers.Single(a => a.SelectedOptionId != q1.CorrectOption!.Id).IsCorrect);

        // 重複交卷回傳同一份成績。
        Assert.False(attempt.Submit(NowUtc.AddMinutes(6)));
        Assert.Equal(NowUtc.AddMinutes(5), attempt.CompletedAtUtc);
        Assert.Equal("attempt_completed", Assert.Throws<DomainException>(() =>
            attempt.SaveAnswer(q1.Id, Wrong(q1), 3, false, null, NowUtc.AddMinutes(7))).Code);
    }

    [Fact]
    public void Older_client_sequence_is_ignored()
    {
        var (attempt, part5, _) = StartPractice();
        var question = part5.Questions.Single();

        Assert.True(attempt.SaveAnswer(question.Id, Wrong(question), 5, false, null, NowUtc.AddSeconds(1)));
        Assert.False(attempt.SaveAnswer(question.Id, question.CorrectOption, 4, false, null, NowUtc.AddSeconds(2)));
        Assert.False(attempt.SaveAnswer(question.Id, question.CorrectOption, 5, false, null, NowUtc.AddSeconds(3)));
        Assert.Equal(Wrong(question).Id, Assert.Single(attempt.Answers).SelectedOptionId);

        Assert.True(attempt.SaveAnswer(question.Id, null, 6, false, null, NowUtc.AddSeconds(4)));
        Assert.Null(Assert.Single(attempt.Answers).SelectedOptionId);
        Assert.Equal(NowUtc.AddSeconds(4), attempt.LastActivityAtUtc);
    }

    [Fact]
    public void Answer_must_belong_to_attempt_question()
    {
        var (attempt, part5, part6) = StartPractice();
        var question = part5.Questions.Single();
        var other = PublishedPart5("P5-0999").PublishedVersion!.Questions.Single();

        Assert.Equal("invalid_option", Assert.Throws<DomainException>(() =>
            attempt.SaveAnswer(question.Id, part6.Questions.First().CorrectOption, 1, false, null, NowUtc)).Code);
        Assert.Equal("question_not_in_attempt", Assert.Throws<DomainException>(() =>
            attempt.SaveAnswer(other.Id, other.CorrectOption, 1, false, null, NowUtc)).Code);
        Assert.Equal("invalid_sequence", Assert.Throws<DomainException>(() =>
            attempt.SaveAnswer(question.Id, question.CorrectOption, -1, false, null, NowUtc)).Code);
    }

    [Fact]
    public void Practice_can_be_abandoned_but_exam_cannot()
    {
        var (attempt, part5, _) = StartPractice();
        var question = part5.Questions.Single();
        attempt.SaveAnswer(question.Id, question.CorrectOption, 1, false, null, NowUtc);

        Assert.True(attempt.Abandon(NowUtc.AddMinutes(1)));
        Assert.Equal(CompletionReason.Abandoned, attempt.CompletionReason);
        Assert.Equal(1, attempt.CorrectCount);

        var (paper, questions) = PublishedReadingPaper();
        var exam = ExamAttempt.StartExam(StudentId, paper, questions, NowUtc);
        Assert.Equal("cannot_abandon_exam", Assert.Throws<DomainException>(() => exam.Abandon(NowUtc)).Code);
    }

    [Fact]
    public void Reading_test_accepts_answers_within_grace_and_then_times_out()
    {
        var (paper, questions) = PublishedReadingPaper();
        var attempt = ExamAttempt.StartExam(StudentId, paper, questions, NowUtc);
        var deadline = NowUtc + ReadingLimit;
        var first = questions[0];

        Assert.Equal(AttemptMode.ReadingTest, attempt.Mode);
        Assert.Equal(ExamSection.Reading, attempt.CurrentSection);
        Assert.Equal(deadline, attempt.ReadingDeadlineUtc);
        Assert.Equal(deadline, attempt.FinalDeadlineUtc);
        Assert.Equal(QuestionBankReferenceData.ToeicRawScoringRuleId, attempt.ScoringRuleId);

        Assert.True(attempt.SaveAnswer(first.QuestionId, null, 1, false, null, deadline.AddSeconds(4)));
        Assert.False(attempt.IsOverdue(deadline.AddSeconds(5)));
        Assert.True(attempt.IsOverdue(deadline.AddSeconds(6)));
        Assert.Equal("attempt_time_expired", Assert.Throws<DomainException>(() =>
            attempt.SaveAnswer(first.QuestionId, null, 2, false, null, deadline.AddSeconds(6))).Code);

        Assert.False(attempt.ExpireIfOverdue(deadline.AddSeconds(5)));
        Assert.True(attempt.ExpireIfOverdue(deadline.AddSeconds(6)));
        Assert.Equal(CompletionReason.TimedOut, attempt.CompletionReason);
        Assert.Equal(deadline, attempt.CompletedAtUtc);
        Assert.False(attempt.ExpireIfOverdue(deadline.AddMinutes(1)));
    }

    [Fact]
    public void Late_submit_is_recorded_as_timed_out()
    {
        var (paper, questions) = PublishedReadingPaper();
        var attempt = ExamAttempt.StartExam(StudentId, paper, questions, NowUtc);
        var deadline = NowUtc + ReadingLimit;

        Assert.True(attempt.Submit(deadline.AddMinutes(10)));

        Assert.Equal(CompletionReason.TimedOut, attempt.CompletionReason);
        Assert.Equal(deadline, attempt.CompletedAtUtc);
        Assert.Equal(0, attempt.CorrectCount);
    }

    [Fact]
    public void Mock_exam_runs_listening_then_reading()
    {
        var (paper, questions) = PublishedFullPaper();
        var attempt = ExamAttempt.StartExam(StudentId, paper, questions, NowUtc);
        var listening = questions.First(q => q.Section == ExamSection.Listening);
        var reading = questions.First(q => q.Section == ExamSection.Reading);
        var listeningDeadline = NowUtc + ListeningLength;

        Assert.Equal(AttemptMode.MockExam, attempt.Mode);
        Assert.Equal(200, attempt.TotalQuestions);
        Assert.Equal(100, attempt.ListeningTotal);
        Assert.Equal(ExamSection.Listening, attempt.CurrentSection);
        Assert.Equal(listeningDeadline, attempt.ListeningDeadlineUtc);
        Assert.Null(attempt.ReadingDeadlineUtc);
        Assert.Equal(listeningDeadline + ReadingLimit, attempt.FinalDeadlineUtc);

        Assert.True(attempt.SaveAnswer(listening.QuestionId, null, 1, false, null, NowUtc.AddMinutes(1)));
        Assert.Equal("section_not_started", Assert.Throws<DomainException>(() =>
            attempt.SaveAnswer(reading.QuestionId, null, 1, false, null, NowUtc.AddMinutes(1))).Code);

        // 聽力時間結束後自動進入閱讀；寬限期內的聽力答案仍接受。
        Assert.True(attempt.SaveAnswer(listening.QuestionId, null, 2, false, null, listeningDeadline.AddSeconds(3)));
        Assert.Equal(ExamSection.Reading, attempt.CurrentSection);
        Assert.Equal(listeningDeadline, attempt.ReadingStartedAtUtc);
        Assert.Equal(listeningDeadline + ReadingLimit, attempt.ReadingDeadlineUtc);
        Assert.Equal("section_closed", Assert.Throws<DomainException>(() =>
            attempt.SaveAnswer(listening.QuestionId, null, 3, false, null, listeningDeadline.AddSeconds(6))).Code);
        Assert.True(attempt.SaveAnswer(reading.QuestionId, null, 1, false, null, listeningDeadline.AddSeconds(6)));
    }

    [Fact]
    public void Starting_reading_early_locks_listening_answers()
    {
        var (paper, questions) = PublishedFullPaper();
        var attempt = ExamAttempt.StartExam(StudentId, paper, questions, NowUtc);
        var listening = questions.First(q => q.Section == ExamSection.Listening);
        var startedReading = NowUtc.AddMinutes(30);

        Assert.True(attempt.StartReading(startedReading));
        Assert.False(attempt.StartReading(startedReading.AddMinutes(1)));

        Assert.Equal(startedReading + ReadingLimit, attempt.ReadingDeadlineUtc);
        Assert.Equal("section_closed", Assert.Throws<DomainException>(() =>
            attempt.SaveAnswer(listening.QuestionId, null, 1, false, null, startedReading.AddSeconds(1))).Code);
    }

    [Fact]
    public void Start_exam_requires_published_paper_and_matching_questions()
    {
        var (paper, questions) = PublishedReadingPaper();

        Assert.Throws<ArgumentException>(() => ExamAttempt.StartExam(StudentId, paper, questions.Take(99).ToList(), NowUtc));

        paper.Retire(AuthorId, NowUtc);
        Assert.Equal("paper_not_available", Assert.Throws<DomainException>(() =>
            ExamAttempt.StartExam(StudentId, paper, questions, NowUtc)).Code);
    }

    [Fact]
    public void Practice_requires_questions_without_duplicates()
    {
        Assert.Equal("invalid_question_count", Assert.Throws<DomainException>(() =>
            ExamAttempt.StartPractice(StudentId, QuestionBankReferenceData.ToeicRawScoringRuleId, null, [], NowUtc)).Code);

        var version = PublishedPart5().PublishedVersion!;
        var info = ToAttemptQuestions(version, TypeOf(QuestionTypeCodes.IncompleteSentences)).Single();
        Assert.Throws<ArgumentException>(() =>
            ExamAttempt.StartPractice(StudentId, QuestionBankReferenceData.ToeicRawScoringRuleId, null, [info, info], NowUtc));
    }

    private static (ExamAttempt Attempt, QuestionGroupVersion Part5, QuestionGroupVersion Part6) StartPractice()
    {
        var part5 = PublishedPart5().PublishedVersion!;
        var part6 = PublishedPart6("P6-0001").PublishedVersion!;
        var questions = ToAttemptQuestions(part5, TypeOf(QuestionTypeCodes.IncompleteSentences))
            .Concat(ToAttemptQuestions(part6, TypeOf(QuestionTypeCodes.TextCompletion)))
            .ToList();

        var attempt = ExamAttempt.StartPractice(
            StudentId,
            QuestionBankReferenceData.ToeicRawScoringRuleId,
            """{"parts":[5,6]}""",
            questions,
            NowUtc);
        return (attempt, part5, part6);
    }

    private static QuestionOption Wrong(Question question) => question.Options.First(o => !o.IsCorrect);
}

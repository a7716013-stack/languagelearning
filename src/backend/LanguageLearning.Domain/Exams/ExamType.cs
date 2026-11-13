namespace LanguageLearning.Domain.Exams;

/// <summary>
/// 檢定考試類型（TOEIC、IELTS、TOEFL）。分數範圍與級距用於驗證使用者的目標分數。
/// </summary>
public sealed class ExamType
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>是否已開放練習與目標設定。</summary>
    public bool IsAvailable { get; set; }

    public decimal MinScore { get; set; }

    public decimal MaxScore { get; set; }

    public decimal ScoreStep { get; set; }

    public int SortOrder { get; set; }

    public ICollection<ExamVersion> Versions { get; } = new List<ExamVersion>();

    /// <summary>分數是否落在範圍內，且符合級距（例如 TOEIC 以 5 分為單位）。</summary>
    public bool IsValidScore(decimal score)
    {
        if (score < MinScore || score > MaxScore)
        {
            return false;
        }

        if (ScoreStep <= 0)
        {
            return true;
        }

        return (score - MinScore) % ScoreStep == 0;
    }
}

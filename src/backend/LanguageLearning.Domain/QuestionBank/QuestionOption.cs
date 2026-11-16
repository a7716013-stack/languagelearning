namespace LanguageLearning.Domain.QuestionBank;

/// <summary>
/// 題目選項。Part 1、2 的選項只播音，<see cref="Text"/> 保存腳本內容（作答中不顯示，解析時顯示）。
/// </summary>
public sealed class QuestionOption
{
    private QuestionOption()
    {
    }

    public Guid Id { get; private set; }

    public Guid QuestionId { get; private set; }

    public string Label { get; private set; } = string.Empty;

    public string Text { get; private set; } = string.Empty;

    public bool IsCorrect { get; private set; }

    internal static QuestionOption Create(Guid questionId, string label, string text, bool isCorrect, DateTime nowUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(nowUtc),
            QuestionId = questionId,
            Label = label,
            Text = text,
            IsCorrect = isCorrect,
        };

    internal void Update(string text, bool isCorrect)
    {
        Text = text;
        IsCorrect = isCorrect;
    }
}

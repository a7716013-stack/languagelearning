namespace LanguageLearning.Domain.QuestionBank;

/// <summary>題目與主題的多對多關聯。</summary>
public sealed class QuestionTopic
{
    private QuestionTopic()
    {
    }

    public Guid QuestionId { get; private set; }

    public Guid TopicId { get; private set; }

    internal static QuestionTopic Create(Guid questionId, Guid topicId) =>
        new() { QuestionId = questionId, TopicId = topicId };
}

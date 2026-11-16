namespace LanguageLearning.Domain.QuestionBank;

/// <summary>題組版本使用的媒體。圖片的替代文字必填（無障礙）。</summary>
public sealed class QuestionGroupMedia
{
    private QuestionGroupMedia()
    {
    }

    public Guid Id { get; private set; }

    public Guid GroupVersionId { get; private set; }

    public Guid MediaAssetId { get; private set; }

    public MediaAsset? MediaAsset { get; private set; }

    public MediaPurpose Purpose { get; private set; }

    public int SortOrder { get; private set; }

    public string? AltText { get; private set; }

    internal static QuestionGroupMedia Create(
        Guid groupVersionId,
        Guid mediaAssetId,
        MediaPurpose purpose,
        int sortOrder,
        string? altText,
        DateTime nowUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(nowUtc),
            GroupVersionId = groupVersionId,
            MediaAssetId = mediaAssetId,
            Purpose = purpose,
            SortOrder = sortOrder,
            AltText = altText,
        };

    internal void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}

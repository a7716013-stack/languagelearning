using System.Text.RegularExpressions;
using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.QuestionBank;

public static class MediaRules
{
    public const long ImageMaxBytes = 5L * 1024 * 1024;
    public const long AudioMaxBytes = 20L * 1024 * 1024;
    public const int ContentTypeMaxLength = 50;
    public const int StorageKeyMaxLength = 200;
    public const int FileNameMaxLength = 255;
    public const int SourceDescriptionMaxLength = 500;
    public const int LicenseMaxLength = 200;

    public static readonly IReadOnlyList<string> ImageContentTypes = ["image/jpeg", "image/png", "image/webp"];

    /// <summary>MP3 與 M4A（AAC in MP4）。</summary>
    public static readonly IReadOnlyList<string> AudioContentTypes = ["audio/mpeg", "audio/mp4"];
}

/// <summary>
/// 上傳的圖片或音檔。內容不可變：檔案雜湊在建立時記錄，之後不得修改或刪除。
/// 實際檔案由 Infrastructure 的媒體儲存處理，<see cref="StorageKey"/> 不是公開路徑。
/// </summary>
public sealed partial class MediaAsset
{
    private MediaAsset()
    {
    }

    public Guid Id { get; private set; }

    public MediaKind Kind { get; private set; }

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    /// <summary>檔案內容的 SHA-256（小寫十六進位）。</summary>
    public string Sha256 { get; private set; } = string.Empty;

    public string StorageKey { get; private set; } = string.Empty;

    public string OriginalFileName { get; private set; } = string.Empty;

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    /// <summary>音檔長度（毫秒），由管理者上傳時瀏覽器讀取。</summary>
    public int? DurationMs { get; private set; }

    public MediaSourceType SourceType { get; private set; }

    public string SourceDescription { get; private set; } = string.Empty;

    public string License { get; private set; } = string.Empty;

    public Guid UploadedByUserId { get; private set; }

    public DateTime UploadedAtUtc { get; private set; }

    public static MediaAsset Create(
        MediaKind kind,
        string contentType,
        long sizeBytes,
        string sha256,
        string storageKey,
        string originalFileName,
        int? width,
        int? height,
        int? durationMs,
        MediaSourceType sourceType,
        string sourceDescription,
        string license,
        Guid uploadedByUserId,
        DateTime nowUtc)
    {
        Guard.Utc(nowUtc, nameof(nowUtc));
        Guard.NotEmpty(uploadedByUserId, nameof(uploadedByUserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        var normalizedType = (contentType ?? string.Empty).Trim().ToLowerInvariant();
        var allowedTypes = kind == MediaKind.Image ? MediaRules.ImageContentTypes : MediaRules.AudioContentTypes;
        if (!allowedTypes.Contains(normalizedType))
        {
            throw new DomainException(
                "unsupported_media_type",
                kind == MediaKind.Image ? "圖片只接受 JPEG、PNG 或 WebP。" : "音檔只接受 MP3 或 M4A。",
                "file");
        }

        var maxBytes = kind == MediaKind.Image ? MediaRules.ImageMaxBytes : MediaRules.AudioMaxBytes;
        if (sizeBytes <= 0 || sizeBytes > maxBytes)
        {
            throw new DomainException(
                "media_too_large",
                kind == MediaKind.Image ? "圖片不可超過 5 MB。" : "音檔不可超過 20 MB。",
                "file");
        }

        var hash = (sha256 ?? string.Empty).Trim().ToLowerInvariant();
        if (!Sha256Pattern().IsMatch(hash))
        {
            throw new ArgumentException("SHA-256 必須是 64 個十六進位字元。", nameof(sha256));
        }

        if (kind == MediaKind.Image && (width is not > 0 || height is not > 0))
        {
            throw new DomainException("invalid_image_size", "無法讀取圖片尺寸。", "file");
        }

        if (kind == MediaKind.Audio && durationMs is not > 0)
        {
            throw new DomainException("invalid_audio_duration", "請提供音檔長度。", "durationMs");
        }

        var description = Guard.Optional(sourceDescription)
            ?? throw new DomainException("media_source_required", "請填寫媒體來源說明。", "sourceDescription");
        var licenseText = Guard.Optional(license)
            ?? throw new DomainException("media_license_required", "請填寫授權方式。", "license");
        if (description.Length > MediaRules.SourceDescriptionMaxLength)
        {
            throw new DomainException("media_source_too_long", "來源說明過長。", "sourceDescription");
        }

        if (licenseText.Length > MediaRules.LicenseMaxLength)
        {
            throw new DomainException("media_license_too_long", "授權說明過長。", "license");
        }

        // 瀏覽器可能送出含 Windows 路徑的檔名；只保留最後的檔名部分。
        var fileName = Path.GetFileName((originalFileName ?? string.Empty).Trim().Replace('\\', '/'));
        if (fileName.Length > MediaRules.FileNameMaxLength)
        {
            fileName = fileName[^MediaRules.FileNameMaxLength..];
        }

        return new MediaAsset
        {
            Id = Guid.CreateVersion7(nowUtc),
            Kind = kind,
            ContentType = normalizedType,
            SizeBytes = sizeBytes,
            Sha256 = hash,
            StorageKey = storageKey,
            OriginalFileName = fileName,
            Width = kind == MediaKind.Image ? width : null,
            Height = kind == MediaKind.Image ? height : null,
            DurationMs = kind == MediaKind.Audio ? durationMs : null,
            SourceType = sourceType,
            SourceDescription = description,
            License = licenseText,
            UploadedByUserId = uploadedByUserId,
            UploadedAtUtc = nowUtc,
        };
    }

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex Sha256Pattern();
}

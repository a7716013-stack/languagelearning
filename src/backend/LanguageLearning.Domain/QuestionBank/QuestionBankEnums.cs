namespace LanguageLearning.Domain.QuestionBank;

/// <summary>題組版本與試卷的狀態，只能往前：草稿 → 已發布 → 停用。</summary>
public enum ContentStatus
{
    Draft = 1,
    Published = 2,
    Retired = 3,
}

/// <summary>題目來源。只允許原創；禁止匯入官方或市售題目。</summary>
public enum ContentSource
{
    Original = 1,
}

/// <summary>出題者估計的難度；實際答對率另外統計。</summary>
public enum Difficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3,
}

public enum MediaKind
{
    Image = 1,
    Audio = 2,
}

/// <summary>媒體在題組中的用途。</summary>
public enum MediaPurpose
{
    /// <summary>聽力音檔（每個題組最多一個）。</summary>
    Audio = 1,

    /// <summary>Part 1 照片。</summary>
    Photo = 2,

    /// <summary>Part 3、4、7 的圖表（時刻表、價目表等）。</summary>
    Graphic = 3,
}

/// <summary>媒體的來源；來源說明與授權必填。</summary>
public enum MediaSourceType
{
    /// <summary>本專案原創（例如自行製作的圖表）。</summary>
    Original = 1,

    /// <summary>開發者自行拍攝。</summary>
    OwnPhoto = 2,

    /// <summary>有授權的素材。</summary>
    Licensed = 3,
}

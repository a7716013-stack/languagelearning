namespace LanguageLearning.Infrastructure.Persistence;

/// <summary>參考資料使用固定 Id，確保 Migration 可重複執行且不產生重複資料。</summary>
public static class SeedData
{
    public static readonly Guid AdminRoleId = new("0b6f2c5e-3c1a-4d5f-9a59-6f0d3a1c0001");
    public static readonly Guid StudentRoleId = new("0b6f2c5e-3c1a-4d5f-9a59-6f0d3a1c0002");

    public const string AdminRoleConcurrencyStamp = "7d1b9a5e-2f4c-4b8e-9c61-1a2b3c4d0001";
    public const string StudentRoleConcurrencyStamp = "7d1b9a5e-2f4c-4b8e-9c61-1a2b3c4d0002";

    public static readonly Guid ToeicExamTypeId = new("5a3e8c21-7b4d-4e6f-8a1b-2c3d4e5f0001");
    public static readonly Guid IeltsExamTypeId = new("5a3e8c21-7b4d-4e6f-8a1b-2c3d4e5f0002");
    public static readonly Guid ToeflExamTypeId = new("5a3e8c21-7b4d-4e6f-8a1b-2c3d4e5f0003");

    public static readonly Guid ToeicLrVersionId = new("9c2d4f6a-1e3b-4c5d-8e7f-0a1b2c3d0001");
}

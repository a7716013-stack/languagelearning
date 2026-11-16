using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LanguageLearning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    StorageKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SourceDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    License = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaAssets_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Part = table.Column<int>(type: "int", nullable: false),
                    Section = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MinQuestions = table.Column<int>(type: "int", nullable: false),
                    MaxQuestions = table.Column<int>(type: "int", nullable: false),
                    OptionCount = table.Column<int>(type: "int", nullable: false),
                    MinPassages = table.Column<int>(type: "int", nullable: false),
                    MaxPassages = table.Column<int>(type: "int", nullable: false),
                    RequiresAudio = table.Column<bool>(type: "bit", nullable: false),
                    RequiresPhoto = table.Column<bool>(type: "bit", nullable: false),
                    AllowsGraphic = table.Column<bool>(type: "bit", nullable: false),
                    RequiresStem = table.Column<bool>(type: "bit", nullable: false),
                    ShowsOptionText = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionTypes_ExamVersions_ExamVersionId",
                        column: x => x.ExamVersionId,
                        principalTable: "ExamVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScoringRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoringRules_ExamVersions_ExamVersionId",
                        column: x => x.ExamVersionId,
                        principalTable: "ExamVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Section = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QuestionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedVersionNumber = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionGroups_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionGroups_QuestionTypes_QuestionTypeId",
                        column: x => x.QuestionTypeId,
                        principalTable: "QuestionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamPapers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReadingTimeLimitSeconds = table.Column<int>(type: "int", nullable: false),
                    ListeningDurationMs = table.Column<int>(type: "int", nullable: true),
                    DefaultScoringRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RetiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetiredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamPapers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamPapers_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamPapers_AspNetUsers_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamPapers_AspNetUsers_RetiredByUserId",
                        column: x => x.RetiredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamPapers_ExamVersions_ExamVersionId",
                        column: x => x.ExamVersionId,
                        principalTable: "ExamVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamPapers_ScoringRules_DefaultScoringRuleId",
                        column: x => x.DefaultScoringRuleId,
                        principalTable: "ScoringRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionGroupVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Transcript = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: true),
                    Explanation = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AuthorNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ContentHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RetiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetiredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionGroupVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionGroupVersions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionGroupVersions_AspNetUsers_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionGroupVersions_AspNetUsers_RetiredByUserId",
                        column: x => x.RetiredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionGroupVersions_QuestionGroups_QuestionGroupId",
                        column: x => x.QuestionGroupId,
                        principalTable: "QuestionGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExamPaperId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScoringRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompletionReason = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CurrentSection = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReadingTimeLimitSeconds = table.Column<int>(type: "int", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadingStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ListeningDeadlineUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadingDeadlineUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastActivityAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PracticeCriteria = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    ListeningTotal = table.Column<int>(type: "int", nullable: false),
                    ListeningCorrect = table.Column<int>(type: "int", nullable: false),
                    ReadingTotal = table.Column<int>(type: "int", nullable: false),
                    ReadingCorrect = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAttempts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamAttempts_ExamPapers_ExamPaperId",
                        column: x => x.ExamPaperId,
                        principalTable: "ExamPapers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamAttempts_ScoringRules_ScoringRuleId",
                        column: x => x.ScoringRuleId,
                        principalTable: "ScoringRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamPaperItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamPaperId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Part = table.Column<int>(type: "int", nullable: false),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamPaperItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamPaperItems_ExamPapers_ExamPaperId",
                        column: x => x.ExamPaperId,
                        principalTable: "ExamPapers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamPaperItems_QuestionGroupVersions_GroupVersionId",
                        column: x => x.GroupVersionId,
                        principalTable: "QuestionGroupVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamPaperItems_QuestionGroups_QuestionGroupId",
                        column: x => x.QuestionGroupId,
                        principalTable: "QuestionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamPaperItems_QuestionTypes_QuestionTypeId",
                        column: x => x.QuestionTypeId,
                        principalTable: "QuestionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionGroupMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionGroupMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionGroupMedia_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionGroupMedia_QuestionGroupVersions_GroupVersionId",
                        column: x => x.GroupVersionId,
                        principalTable: "QuestionGroupVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionGroupPassages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionGroupPassages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionGroupPassages_QuestionGroupVersions_GroupVersionId",
                        column: x => x.GroupVersionId,
                        principalTable: "QuestionGroupVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Stem = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntendedDifficulty = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_QuestionGroupVersions_GroupVersionId",
                        column: x => x.GroupVersionId,
                        principalTable: "QuestionGroupVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Questions_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionTopics",
                columns: table => new
                {
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionTopics", x => new { x.QuestionId, x.TopicId });
                    table.ForeignKey(
                        name: "FK_QuestionTopics_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionTopics_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttemptQuestionSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    QuestionGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Part = table.Column<int>(type: "int", nullable: false),
                    Section = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CorrectOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptQuestionSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttemptQuestionSnapshots_ExamAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttemptQuestionSnapshots_QuestionGroupVersions_GroupVersionId",
                        column: x => x.GroupVersionId,
                        principalTable: "QuestionGroupVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttemptQuestionSnapshots_QuestionGroups_QuestionGroupId",
                        column: x => x.QuestionGroupId,
                        principalTable: "QuestionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttemptQuestionSnapshots_QuestionOptions_CorrectOptionId",
                        column: x => x.CorrectOptionId,
                        principalTable: "QuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttemptQuestionSnapshots_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttemptAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelectedOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClientSequence = table.Column<long>(type: "bigint", nullable: false),
                    MarkedForReview = table.Column<bool>(type: "bit", nullable: false),
                    ElapsedMs = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttemptAnswers_AttemptQuestionSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "AttemptQuestionSnapshots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttemptAnswers_ExamAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "ExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttemptAnswers_QuestionOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "QuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "QuestionTypes",
                columns: new[] { "Id", "AllowsGraphic", "Code", "ExamVersionId", "MaxPassages", "MaxQuestions", "MinPassages", "MinQuestions", "Name", "OptionCount", "Part", "RequiresAudio", "RequiresPhoto", "RequiresStem", "Section", "ShowsOptionText", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0101"), false, "TOEIC_P1_PHOTOGRAPHS", new Guid("9c2d4f6a-1e3b-4c5d-8e7f-0a1b2c3d0001"), 0, 1, 0, 1, "Part 1 照片描述", 4, 1, true, true, false, "Listening", false, 10 },
                    { new Guid("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0102"), false, "TOEIC_P2_QUESTION_RESPONSE", new Guid("9c2d4f6a-1e3b-4c5d-8e7f-0a1b2c3d0001"), 0, 1, 0, 1, "Part 2 應答問題", 3, 2, true, false, false, "Listening", false, 20 },
                    { new Guid("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0103"), true, "TOEIC_P3_CONVERSATIONS", new Guid("9c2d4f6a-1e3b-4c5d-8e7f-0a1b2c3d0001"), 0, 3, 0, 3, "Part 3 簡短對話", 4, 3, true, false, true, "Listening", true, 30 },
                    { new Guid("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0104"), true, "TOEIC_P4_TALKS", new Guid("9c2d4f6a-1e3b-4c5d-8e7f-0a1b2c3d0001"), 0, 3, 0, 3, "Part 4 簡短獨白", 4, 4, true, false, true, "Listening", true, 40 },
                    { new Guid("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0105"), false, "TOEIC_P5_INCOMPLETE_SENTENCES", new Guid("9c2d4f6a-1e3b-4c5d-8e7f-0a1b2c3d0001"), 0, 1, 0, 1, "Part 5 句子填空", 4, 5, false, false, true, "Reading", true, 50 },
                    { new Guid("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0106"), false, "TOEIC_P6_TEXT_COMPLETION", new Guid("9c2d4f6a-1e3b-4c5d-8e7f-0a1b2c3d0001"), 1, 4, 1, 4, "Part 6 段落填空", 4, 6, false, false, false, "Reading", true, 60 },
                    { new Guid("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0107"), true, "TOEIC_P7_SINGLE_PASSAGE", new Guid("9c2d4f6a-1e3b-4c5d-8e7f-0a1b2c3d0001"), 1, 4, 1, 2, "Part 7 單篇閱讀", 4, 7, false, false, true, "Reading", true, 70 },
                    { new Guid("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0108"), true, "TOEIC_P7_MULTIPLE_PASSAGES", new Guid("9c2d4f6a-1e3b-4c5d-8e7f-0a1b2c3d0001"), 3, 5, 2, 5, "Part 7 多篇閱讀", 4, 7, false, false, true, "Reading", true, 71 }
                });

            migrationBuilder.InsertData(
                table: "ScoringRules",
                columns: new[] { "Id", "Code", "Description", "ExamVersionId", "IsDefault", "Kind", "ParametersJson", "Version" },
                values: new object[] { new Guid("a1f7e5d4-8f6a-4c4b-9d5e-6f7a8b9c0401"), "TOEIC-LR-RAW", "答對題數與答對率（總分、聽力、閱讀、各 Part）。不換算官方分數。", new Guid("9c2d4f6a-1e3b-4c5d-8e7f-0a1b2c3d0001"), true, "RawCount", null, 1 });

            migrationBuilder.InsertData(
                table: "Skills",
                columns: new[] { "Id", "Code", "Name", "Section", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("8d5c3b21-6d4e-4a2f-8b3c-4d5e6f7a0001"), "PHOTO_DESCRIPTION", "照片描述", "Listening", 1 },
                    { new Guid("8d5c3b21-6d4e-4a2f-8b3c-4d5e6f7a0002"), "RESPONSE", "應答判斷", "Listening", 2 },
                    { new Guid("8d5c3b21-6d4e-4a2f-8b3c-4d5e6f7a0003"), "SPEAKER_INTENT", "說話者意圖", "Listening", 3 },
                    { new Guid("8d5c3b21-6d4e-4a2f-8b3c-4d5e6f7a0004"), "MAIN_IDEA", "主旨與目的", null, 4 },
                    { new Guid("8d5c3b21-6d4e-4a2f-8b3c-4d5e6f7a0005"), "DETAIL", "細節理解", null, 5 },
                    { new Guid("8d5c3b21-6d4e-4a2f-8b3c-4d5e6f7a0006"), "INFERENCE", "推論", null, 6 },
                    { new Guid("8d5c3b21-6d4e-4a2f-8b3c-4d5e6f7a0007"), "GRAPHIC", "圖表整合", null, 7 },
                    { new Guid("8d5c3b21-6d4e-4a2f-8b3c-4d5e6f7a0008"), "PARAPHRASE", "同義改寫", null, 8 },
                    { new Guid("8d5c3b21-6d4e-4a2f-8b3c-4d5e6f7a0009"), "VOCABULARY", "字彙", null, 9 },
                    { new Guid("8d5c3b21-6d4e-4a2f-8b3c-4d5e6f7a0010"), "GRAMMAR", "文法", "Reading", 10 },
                    { new Guid("8d5c3b21-6d4e-4a2f-8b3c-4d5e6f7a0011"), "SENTENCE_INSERTION", "句子插入", "Reading", 11 },
                    { new Guid("8d5c3b21-6d4e-4a2f-8b3c-4d5e6f7a0012"), "MULTI_PASSAGE", "多篇整合", "Reading", 12 }
                });

            migrationBuilder.InsertData(
                table: "Topics",
                columns: new[] { "Id", "Code", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0001"), "OFFICE", "辦公室", 1 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0002"), "PERSONNEL", "人事與招募", 2 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0003"), "MARKETING", "行銷與銷售", 3 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0004"), "FINANCE", "財務與會計", 4 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0005"), "PURCHASING", "採購與訂單", 5 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0006"), "MANUFACTURING", "製造與品管", 6 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0007"), "CUSTOMER_SERVICE", "客戶服務", 7 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0008"), "TECHNOLOGY", "科技與設備", 8 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0009"), "TRAVEL", "旅遊與出差", 9 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0010"), "TRANSPORTATION", "交通", 10 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0011"), "DINING", "餐飲", 11 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0012"), "LODGING", "住宿與房地產", 12 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0013"), "EVENTS", "會議與活動", 13 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0014"), "HEALTH", "健康與醫療", 14 },
                    { new Guid("9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b0015"), "ENTERTAINMENT", "娛樂與文化", 15 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswers_AttemptId",
                table: "AttemptAnswers",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswers_SelectedOptionId",
                table: "AttemptAnswers",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswers_SnapshotId",
                table: "AttemptAnswers",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestionSnapshots_AttemptId_QuestionId",
                table: "AttemptQuestionSnapshots",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestionSnapshots_AttemptId_Sequence",
                table: "AttemptQuestionSnapshots",
                columns: new[] { "AttemptId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestionSnapshots_CorrectOptionId",
                table: "AttemptQuestionSnapshots",
                column: "CorrectOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestionSnapshots_GroupVersionId",
                table: "AttemptQuestionSnapshots",
                column: "GroupVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestionSnapshots_QuestionGroupId",
                table: "AttemptQuestionSnapshots",
                column: "QuestionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptQuestionSnapshots_QuestionId",
                table: "AttemptQuestionSnapshots",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_ExamPaperId",
                table: "ExamAttempts",
                column: "ExamPaperId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_ScoringRuleId",
                table: "ExamAttempts",
                column: "ScoringRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_Status_ListeningDeadlineUtc_ReadingDeadlineUtc",
                table: "ExamAttempts",
                columns: new[] { "Status", "ListeningDeadlineUtc", "ReadingDeadlineUtc" },
                filter: "[Status] = 'InProgress'");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_UserId_StartedAtUtc",
                table: "ExamAttempts",
                columns: new[] { "UserId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_UserId_Status",
                table: "ExamAttempts",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperItems_ExamPaperId_GroupVersionId",
                table: "ExamPaperItems",
                columns: new[] { "ExamPaperId", "GroupVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperItems_ExamPaperId_QuestionGroupId",
                table: "ExamPaperItems",
                columns: new[] { "ExamPaperId", "QuestionGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperItems_ExamPaperId_SortOrder",
                table: "ExamPaperItems",
                columns: new[] { "ExamPaperId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperItems_GroupVersionId",
                table: "ExamPaperItems",
                column: "GroupVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperItems_QuestionGroupId",
                table: "ExamPaperItems",
                column: "QuestionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperItems_QuestionTypeId",
                table: "ExamPaperItems",
                column: "QuestionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPapers_Code",
                table: "ExamPapers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamPapers_CreatedByUserId",
                table: "ExamPapers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPapers_DefaultScoringRuleId",
                table: "ExamPapers",
                column: "DefaultScoringRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPapers_ExamVersionId",
                table: "ExamPapers",
                column: "ExamVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPapers_Kind_Status",
                table: "ExamPapers",
                columns: new[] { "Kind", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamPapers_PublishedByUserId",
                table: "ExamPapers",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPapers_RetiredByUserId",
                table: "ExamPapers",
                column: "RetiredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_Kind_Sha256",
                table: "MediaAssets",
                columns: new[] { "Kind", "Sha256" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_StorageKey",
                table: "MediaAssets",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_UploadedByUserId",
                table: "MediaAssets",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroupMedia_GroupVersionId_MediaAssetId",
                table: "QuestionGroupMedia",
                columns: new[] { "GroupVersionId", "MediaAssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroupMedia_MediaAssetId",
                table: "QuestionGroupMedia",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroupPassages_GroupVersionId_SortOrder",
                table: "QuestionGroupPassages",
                columns: new[] { "GroupVersionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroups_Code",
                table: "QuestionGroups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroups_CreatedByUserId",
                table: "QuestionGroups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroups_QuestionTypeId",
                table: "QuestionGroups",
                column: "QuestionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroupVersions_CreatedByUserId",
                table: "QuestionGroupVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroupVersions_PublishedByUserId",
                table: "QuestionGroupVersions",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroupVersions_QuestionGroupId_Draft",
                table: "QuestionGroupVersions",
                column: "QuestionGroupId",
                unique: true,
                filter: "[Status] = 'Draft'");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroupVersions_QuestionGroupId_Status",
                table: "QuestionGroupVersions",
                columns: new[] { "QuestionGroupId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroupVersions_QuestionGroupId_VersionNumber",
                table: "QuestionGroupVersions",
                columns: new[] { "QuestionGroupId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroupVersions_RetiredByUserId",
                table: "QuestionGroupVersions",
                column: "RetiredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroupVersions_Status",
                table: "QuestionGroupVersions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_QuestionId_Label",
                table: "QuestionOptions",
                columns: new[] { "QuestionId", "Label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_GroupVersionId_Number",
                table: "Questions",
                columns: new[] { "GroupVersionId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SkillId",
                table: "Questions",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionTopics_TopicId",
                table: "QuestionTopics",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionTypes_Code",
                table: "QuestionTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionTypes_ExamVersionId",
                table: "QuestionTypes",
                column: "ExamVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoringRules_Code_Version",
                table: "ScoringRules",
                columns: new[] { "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoringRules_ExamVersionId",
                table: "ScoringRules",
                column: "ExamVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Code",
                table: "Skills",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Code",
                table: "Topics",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttemptAnswers");

            migrationBuilder.DropTable(
                name: "ExamPaperItems");

            migrationBuilder.DropTable(
                name: "QuestionGroupMedia");

            migrationBuilder.DropTable(
                name: "QuestionGroupPassages");

            migrationBuilder.DropTable(
                name: "QuestionTopics");

            migrationBuilder.DropTable(
                name: "AttemptQuestionSnapshots");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "ExamAttempts");

            migrationBuilder.DropTable(
                name: "QuestionOptions");

            migrationBuilder.DropTable(
                name: "ExamPapers");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "ScoringRules");

            migrationBuilder.DropTable(
                name: "QuestionGroupVersions");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "QuestionGroups");

            migrationBuilder.DropTable(
                name: "QuestionTypes");
        }
    }
}

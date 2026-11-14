using System.Net;
using LanguageLearning.Application.Admin;
using LanguageLearning.Application.Common.Paging;
using LanguageLearning.Infrastructure.Identity;
using LanguageLearning.Infrastructure.Persistence;
using LanguageLearning.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.IntegrationTests;

public sealed class AdminAndSystemTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private ApiFactory Factory => fixture.Factory;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Admin_can_view_summary_users_and_audit_logs()
    {
        var studentEmail = await TestUsers.RegisterConfirmedAsync(Factory);
        using (var student = Factory.CreateTestClient())
        {
            await student.LoginAsync(studentEmail, TestUsers.Password);
        }

        using var admin = await TestUsers.AdminAsync(Factory);

        var summary = await admin.GetJsonAsync<AdminSummaryDto>("/api/admin/summary");
        Assert.True(summary.TotalUsers >= 2);
        Assert.Equal(1, summary.AdminCount);
        Assert.True(summary.SuccessfulLoginsLast24Hours >= 2);

        var users = await admin.GetJsonAsync<PagedResult<AdminUserDto>>(
            $"/api/admin/users?search={Uri.EscapeDataString(studentEmail)}&page=1&pageSize=10");
        var found = Assert.Single(users.Items);
        Assert.Equal(studentEmail, found.Email);
        Assert.Equal(new[] { "Student" }, found.Roles);

        var logs = await admin.GetJsonAsync<PagedResult<AuditLogDto>>("/api/admin/audit-logs?action=auth.login&pageSize=100");
        Assert.NotEmpty(logs.Items);
        Assert.All(logs.Items, log => Assert.StartsWith("auth.login", log.Action, StringComparison.Ordinal));
        Assert.Contains(logs.Items, log => log.Action == "auth.login.succeeded" && log.ActorEmail == studentEmail);
    }

    [Fact]
    public async Task Admin_user_list_is_paginated()
    {
        using var admin = await TestUsers.AdminAsync(Factory);

        var page = await admin.GetJsonAsync<PagedResult<AdminUserDto>>("/api/admin/users?page=1&pageSize=1000");

        Assert.Equal(100, page.PageSize);
        Assert.Equal(1, page.Page);
    }

    [Fact]
    public async Task Database_initializer_is_idempotent()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await initializer.InitializeAsync(Ct);
        await initializer.InitializeAsync(Ct);

        Assert.Equal(2, await db.Roles.CountAsync(Ct));
        Assert.Equal(3, await db.ExamTypes.CountAsync(Ct));
        Assert.Equal(1, await db.ExamVersions.CountAsync(Ct));
        Assert.Equal(1, await db.Users.CountAsync(u => u.Email == ApiFactory.AdminEmail, Ct));

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync(ApiFactory.AdminEmail);
        Assert.NotNull(admin);
        Assert.True(await userManager.IsInRoleAsync(admin, "Admin"));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync(Ct));
    }

    [Fact]
    public async Task Version_and_health_endpoints_respond()
    {
        using var client = Factory.CreateTestClient();

        var version = await TestClient.ReadJsonAsync(await client.GetAsync("/api/version"));
        Assert.Equal("2.1.0", version.GetProperty("version").GetString());
        Assert.Equal("Testing", version.GetProperty("environment").GetString());

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task Unknown_api_route_returns_problem_details()
    {
        using var client = Factory.CreateTestClient();

        var response = await client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}

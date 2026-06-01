using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Courses.Queries.GetCourseById;
using RunRoutes.Infrastructure.Queries.Courses;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Queries.Courses;

[Collection("Database")]
public class GetCourseByIdQueryHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_公開コースを取得できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid courseId;
        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated();
            db.Users.Add(user);

            var course = Course.Create(
                userId: user.Id,
                title: "テストコース",
                description: "テストコースの説明",
                difficulty: Difficulty.Easy,
                route: TestCourseBuilder.CreateTestRoute(),
                isPublic: true,
                tags: []);
            db.Courses.Add(course);

            await db.SaveChangesAsync();
            courseId = course.Id;
        }

        // Act
        GetCourseResponse response;
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new GetCourseByIdQueryHandler(db);
            response = await handler.HandleAsync(
                new GetCourseByIdQuery(courseId, CurrentUserId: null),
                default);
        }

        // Assert
        Assert.Equal(courseId, response.Course.Id);
        Assert.Equal("テストコース", response.Course.Title);
        Assert.Equal("easy", response.Course.Difficulty);
        Assert.True(response.Course.IsPublic);
    }

    [Fact]
    public async Task HandleAsync_存在しないコースを指定すると_NotFoundException_を投げる()
    {
        // Arrange
        await _fixture.ResetAsync();

        // Act & Assert
        await using var db = _fixture.CreateDbContext();
        var handler = new GetCourseByIdQueryHandler(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
        handler.HandleAsync(
            new GetCourseByIdQuery(Guid.NewGuid(), CurrentUserId: null),
            default));
    }

    [Fact]
    public async Task HandleAsync_非公開コースを他人が取得すると_ForbiddenException_を投げる()
    {
        //  Arrange
        await _fixture.ResetAsync();

        Guid courseId;
        await using (var db = _fixture.CreateDbContext())
        {
            var owner = TestUserBuilder.CreateActivated();
            db.Users.Add(owner);

            var course = Course.Create(
                userId: owner.Id,
                title: "非公開コース",
                description: null,
                difficulty: Difficulty.Hard,
                route: TestCourseBuilder.CreateTestRoute(),
                isPublic: false,
                tags: []);
            db.Courses.Add(course);

            await db.SaveChangesAsync();
            courseId = course.Id;
        }

        // Act & Assert（別ユーザーとしてアクセス）
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new GetCourseByIdQueryHandler(db);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.HandleAsync(
                new GetCourseByIdQuery(courseId, CurrentUserId: Guid.NewGuid()),
                default));
        }
    }

}


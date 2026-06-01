using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Courses.Queries.ListCourses;
using RunRoutes.Infrastructure.Queries.Courses;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Queries.Courses;

[Collection("Database")]
public class ListCoursesQueryHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_公開コースと自分の非公開コースのみ取得できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid publicCourseId;
        Guid viewerPrivateCourseId;
        Guid viewerId;
        await using (var db = _fixture.CreateDbContext())
        {
            var viewer = TestUserBuilder.CreateActivated();
            db.Users.Add(viewer);
            viewerId = viewer.Id;

            var other = TestUserBuilder.CreateActivated();
            db.Users.Add(other);

            var publicCourse = Course.Create(
                userId: other.Id,
                title: "公開コース",
                description: null,
                difficulty: Difficulty.Easy,
                route: TestCourseBuilder.CreateTestRoute(),
                isPublic: true,
                tags: []);
            db.Courses.Add(publicCourse);

            var othersPrivateCourse = Course.Create(
                userId: other.Id,
                title: "他人の非公開コース",
                description: null,
                difficulty: Difficulty.Easy,
                route: TestCourseBuilder.CreateTestRoute(),
                isPublic: false,
                tags: []);
            db.Courses.Add(othersPrivateCourse);

            var myPrivateCourse = Course.Create(
                userId: viewer.Id,
                title: "自分の非公開コース",
                description: null,
                difficulty: Difficulty.Easy,
                route: TestCourseBuilder.CreateTestRoute(),
                isPublic: false,
                tags: []);
            db.Courses.Add(myPrivateCourse);

            await db.SaveChangesAsync();
            publicCourseId = publicCourse.Id;
            viewerPrivateCourseId = myPrivateCourse.Id;
        }

        // Act
        GetCoursesResponse response;
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new ListCoursesQueryHandler(db);
            var query = new ListCoursesQuery(null, null, null, null, null, 1, 20, viewerId);
            response = await handler.HandleAsync(query, default);
        }

        // Assert
        Assert.Equal(
            new[] { publicCourseId, viewerPrivateCourseId }.OrderBy(id => id),
            response.Courses.Select(c => c.Id).OrderBy(id => id));

    }

    [Fact]
    public async Task HandleAsync_近傍検索で半径内のコースのみ取得できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid nearCourseId;
        Guid userId;

        // 基準座標
        (double lat, double lon) = (43.068, 141.35);

        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated();
            db.Users.Add(user);
            userId = user.Id;

            // 中心 (141.35, 43.068) の近く (緯度 +0.01 ≒ 1.1km)
            var nearCourse = Course.Create(user.Id, "近いコース", null, Difficulty.Easy,
                TestCourseBuilder.CreateTestRoute(lon, lat + 0.01), true, []);
            db.Courses.Add(nearCourse);

            // 中心から遠い (緯度 +0.5 ≒ 55km)
            var farCourse = Course.Create(user.Id, "遠いコース", null, Difficulty.Easy,
                TestCourseBuilder.CreateTestRoute(lon, lat + 0.5), true, []);
            db.Courses.Add(farCourse);

            await db.SaveChangesAsync();
            nearCourseId = nearCourse.Id;
        }

        // Act
        GetCoursesResponse response;
        await using (var db = _fixture.CreateDbContext())
        {
            var handler = new ListCoursesQueryHandler(db);
            // 基準座標から半径 5km
            var query = new ListCoursesQuery(lat, lon, 5.0, null, null, 1, 20, userId);
            response = await handler.HandleAsync(query, default);
        }

        // Assert: 近いコースだけ返る
        Assert.Equal(nearCourseId, Assert.Single(response.Courses).Id);
    }
}
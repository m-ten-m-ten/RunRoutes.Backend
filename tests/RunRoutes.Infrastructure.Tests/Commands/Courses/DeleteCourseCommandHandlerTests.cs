using NetTopologySuite.Geometries;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.DeleteCourse;
using RunRoutes.Infrastructure.Commands.Courses;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Courses;

[Collection("Database")]
public class DeleteCourseCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_本人が削除できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid userId;
        Guid courseId;
        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated();
            db.Users.Add(user);

            Coordinate[] coords = [new Coordinate(141.3507, 43.0686), new Coordinate(141.3522, 43.0700)];
            var route = new LineString(coords) { SRID = 4326 };
            var course = Course.Create(user.Id, "コースタイトル", null, Difficulty.Easy,
            route, true, []);
            db.Courses.Add(course);

            await db.SaveChangesAsync();
            userId = user.Id;
            courseId = course.Id;
        }

        // Act
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new CourseRepository(db);
            var handler = new DeleteCourseCommandHandler(repo);
            var command = new DeleteCourseCommand(courseId, userId);
            await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var course = await db.Courses.FindAsync(courseId);
            Assert.Null(course);
        }
    }

    [Fact]
    public async Task HandleAsync_他人が削除でForbiddenException()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid courseId;
        await using (var db = _fixture.CreateDbContext())
        {
            var owner = TestUserBuilder.CreateActivated();
            db.Users.Add(owner);

            Coordinate[] coords = [new Coordinate(141.3507, 43.0686), new Coordinate(141.3522, 43.0700)];
            var route = new LineString(coords) { SRID = 4326 };
            var course = Course.Create(owner.Id, "コースタイトル", null, Difficulty.Easy,
            route, true, []);
            db.Courses.Add(course);

            await db.SaveChangesAsync();
            courseId = course.Id;
        }

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new CourseRepository(db);
            var handler = new DeleteCourseCommandHandler(repo);
            var command = new DeleteCourseCommand(courseId, Guid.NewGuid());
            await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(command, default));
        }
    }
}
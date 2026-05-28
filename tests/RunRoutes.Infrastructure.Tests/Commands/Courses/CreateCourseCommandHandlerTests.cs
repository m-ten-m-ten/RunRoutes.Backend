using Microsoft.EntityFrameworkCore;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Commands.CreateCourse;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Infrastructure.Commands.Courses;
using RunRoutes.Infrastructure.Repositories;
using RunRoutes.Infrastructure.Tests.Infrastructure;

namespace RunRoutes.Infrastructure.Tests.Commands.Courses;

[Collection("Database")]
public class CreateCourseCommandHandlerTests(PostgresContainerFixture fixture)
{
    private readonly PostgresContainerFixture _fixture = fixture;

    [Fact]
    public async Task HandleAsync_コースを作成できる()
    {
        // Arrange
        await _fixture.ResetAsync();

        Guid userId;
        await using (var db = _fixture.CreateDbContext())
        {
            var user = TestUserBuilder.CreateActivated();
            db.Users.Add(user);

            await db.SaveChangesAsync();
            userId = user.Id;
        }

        // Act
        Guid courseId;
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new CourseRepository(db);
            var handler = new CreateCourseCommandHandler(repo);
            var route = new GeoJsonLineStringDto("LineString", [[141.3507, 43.0686], [141.3522, 43.0700]]);
            var command = new CreateCourseCommand("コースタイトル", null, "easy", true, route, null, [], userId);
            courseId = await handler.HandleAsync(command, default);
        }

        // Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var course = await db.Courses.FirstAsync(c => c.Id == courseId);
            Assert.Equal("コースタイトル", course.Title);
            Assert.Equal(Difficulty.Easy, course.Difficulty);
            Assert.True(course.Distance.Meters > 0);
        }
    }

    [Fact]
    public async Task HandleAsync_不正な難易度でValidationExceptionを投げる()
    {
        // Arrange
        await _fixture.ResetAsync();

        // Act & Assert
        await using (var db = _fixture.CreateDbContext())
        {
            var repo = new CourseRepository(db);
            var handler = new CreateCourseCommandHandler(repo);
            var route = new GeoJsonLineStringDto("LineString", [[141.3507, 43.0686], [141.3522, 43.0700]]);
            var command = new CreateCourseCommand("コースタイトル", null, "invalid", true, route, null, [], Guid.NewGuid());
            await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(command, default));
        }
    }
}
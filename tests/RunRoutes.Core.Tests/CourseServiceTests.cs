using Moq;
using NetTopologySuite.Geometries;
using RunRoutes.Core.DTOs.Common;
using RunRoutes.Core.DTOs.Courses;
using RunRoutes.Core.Entities;
using RunRoutes.Core.Exceptions;
using RunRoutes.Core.Interfaces.Repositories;
using RunRoutes.Core.Services;

namespace RunRoutes.Core.Tests;

public class CourseServiceTests
{
    private readonly Mock<ICourseRepository> _courseRepoMock = new();
    private readonly CourseService _sut;

    public CourseServiceTests()
    {
        _sut = new CourseService(_courseRepoMock.Object);
    }

    private static Course MakeCourse(Guid? userId = null, Guid? courseId = null)
    {
        var uid = userId ?? Guid.NewGuid();
        return new Course
        {
            Id = courseId ?? Guid.NewGuid(),
            UserId = uid,
            Title = "Test Course",
            Difficulty = "easy",
            Route = new LineString([new Coordinate(135.0, 35.0), new Coordinate(135.1, 35.1)]) { SRID = 4326 },
            DistanceM = 1000,
            IsPublic = true,
            User = new User { Id = uid, Email = "a@example.com", Username = "user", CreatedAt = DateTime.UtcNow },
            Tags = [],
            Comments = [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    [Fact]
    public async Task Create_正常に登録できる()
    {
        var userId = Guid.NewGuid();
        var request = new CreateCourseRequest(
            "Test Course", null, "easy", true,
            new GeoJsonLineStringDto("LineString", [[135.0, 35.0], [135.1, 35.1]]),
            null, []);

        Course? addedCourse = null;
        _courseRepoMock.Setup(r => r.GetTagsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);
        _courseRepoMock.Setup(r => r.AddAsync(It.IsAny<Course>()))
            .Callback<Course>(c => addedCourse = c)
            .Returns(Task.CompletedTask);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => addedCourse is null ? null : MakeCourse(userId, addedCourse.Id));

        var result = await _sut.CreateAsync(request, userId);

        Assert.NotNull(result.Course);
        Assert.True(result.Course.DistanceM > 0);
    }

    [Fact]
    public async Task Create_不正な難易度でValidationException()
    {
        var request = new CreateCourseRequest(
            "Test", null, "invalid", true,
            new GeoJsonLineStringDto("LineString", [[135.0, 35.0], [135.1, 35.1]]),
            null, []);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetById_正常に取得できる()
    {
        var userId = Guid.NewGuid();
        var course = MakeCourse(userId);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);

        var result = await _sut.GetByIdAsync(course.Id, userId);

        Assert.Equal(course.Id, result.Course.Id);
    }

    [Fact]
    public async Task GetById_存在しないIDでNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Course?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid(), null));
    }

    [Fact]
    public async Task Update_本人が更新できる()
    {
        var userId = Guid.NewGuid();
        var course = MakeCourse(userId);
        var request = new UpdateCourseRequest("Updated Title", null, null, null, null, null, null);

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
        _courseRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Course>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(course.Id, request, userId);

        Assert.Equal("Updated Title", result.Course.Title);
    }

    [Fact]
    public async Task Update_他人の更新でForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var course = MakeCourse(ownerId);
        var request = new UpdateCourseRequest(null, null, null, null, null, null, null);

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.UpdateAsync(course.Id, request, Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_本人が削除できる()
    {
        var userId = Guid.NewGuid();
        var course = MakeCourse(userId);

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
        _courseRepoMock.Setup(r => r.DeleteAsync(course)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(course.Id, userId);

        _courseRepoMock.Verify(r => r.DeleteAsync(course), Times.Once);
    }

    [Fact]
    public async Task Delete_他人の削除でForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var course = MakeCourse(ownerId);

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.DeleteAsync(course.Id, Guid.NewGuid()));
    }
}

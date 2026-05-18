using Moq;
using NetTopologySuite.Geometries;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Courses.Dtos;
using RunRoutes.Core.Users;

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
        var course = Course.Create(
            userId: uid,
            title: "Test Course",
            description: null,
            difficulty: Difficulty.Easy,
            route: new LineString([new Coordinate(135.0, 35.0), new Coordinate(135.1, 35.1)]) { SRID = 4326 },
            isPublic: true,
            tags: []);

        if (courseId is not null)
            SetPrivate(course, nameof(Course.Id), courseId.Value);

        var user = new User { Id = uid, Email = EmailAddress.Create("a@example.com"), Username = Username.Create("user"), CreatedAt = DateTime.UtcNow };
        SetPrivate(course, nameof(Course.User), user);

        return course;
    }

    private static void SetPrivate<T>(T target, string propertyName, object value) where T : class
    {
        typeof(T).GetProperty(propertyName)!.SetValue(target, value);
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
        _courseRepoMock.Setup(r => r.GetTagsByIdsForUpdateAsync(It.IsAny<IEnumerable<Guid>>()))
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

        _courseRepoMock.Setup(r => r.GetByIdForUpdateAsync(course.Id)).ReturnsAsync(course);
        _courseRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Course>())).Returns(Task.CompletedTask);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(MakeCourse(userId, course.Id));

        var result = await _sut.UpdateAsync(course.Id, request, userId);

        Assert.Equal(course.Id, result.Course.Id);
    }

    [Fact]
    public async Task Update_他人の更新でForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var course = MakeCourse(ownerId);
        var request = new UpdateCourseRequest(null, null, null, null, null, null, null);

        _courseRepoMock.Setup(r => r.GetByIdForUpdateAsync(course.Id)).ReturnsAsync(course);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.UpdateAsync(course.Id, request, Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_本人が削除できる()
    {
        var userId = Guid.NewGuid();
        var course = MakeCourse(userId);

        _courseRepoMock.Setup(r => r.GetByIdForUpdateAsync(course.Id)).ReturnsAsync(course);
        _courseRepoMock.Setup(r => r.DeleteAsync(course)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(course.Id, userId);

        _courseRepoMock.Verify(r => r.DeleteAsync(course), Times.Once);
    }

    [Fact]
    public async Task Delete_他人の削除でForbiddenException()
    {
        var ownerId = Guid.NewGuid();
        var course = MakeCourse(ownerId);

        _courseRepoMock.Setup(r => r.GetByIdForUpdateAsync(course.Id)).ReturnsAsync(course);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.DeleteAsync(course.Id, Guid.NewGuid()));
    }
}

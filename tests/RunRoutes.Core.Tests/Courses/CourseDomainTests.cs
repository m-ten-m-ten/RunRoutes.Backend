using NetTopologySuite.Geometries;
using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Courses;
using RunRoutes.Core.Tags;
using RunRoutes.Core.Users;

namespace RunRoutes.Core.Tests.Courses;

public class CourseDomainTests
{
    // ========================================
    // Create / Reconstruct
    // ========================================

    [Fact]
    public void Create_正常な引数で生成できる()
    {
        var userId = Guid.NewGuid();
        var course = MakeMinimalCourse(userId: userId);

        Assert.NotEqual(Guid.Empty, course.Id);
        Assert.Equal(userId, course.UserId);
        Assert.Equal("Test", course.Title);
        Assert.True(course.Distance.Meters > 0);
        Assert.Empty(course.Comments);
        Assert.Empty(course.Tags);
    }

    [Fact]
    public void Create_空タイトルで_ValidationException()
    {
        Assert.Throws<ValidationException>(() => Course.Create(
            userId: Guid.NewGuid(),
            title: "   ",
            description: null,
            difficulty: Difficulty.Easy,
            route: MakeRoute(),
            isPublic: true,
            tags: []));
    }

    [Fact]
    public void Create_座標2点未満の_LineString_で_ValidationException()
    {
        var oneVertexRoute = new LineString([]) { SRID = 4326 };
        Assert.Throws<ValidationException>(() => Course.Create(
            userId: Guid.NewGuid(),
            title: "Test",
            description: null,
            difficulty: Difficulty.Easy,
            route: oneVertexRoute,
            isPublic: true,
            tags: []));
    }

    [Fact]
    public void Reconstruct_全フィールドが復元される()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = createdAt.AddDays(1);
        var user = new User { Id = userId, Email = "a@example.com", Username = "u", CreatedAt = DateTime.UtcNow };

        var course = Course.Reconstruct(
            id: id,
            userId: userId,
            title: "T",
            description: "D",
            difficulty: Difficulty.Hard,
            route: MakeRoute(),
            distance: Distance.FromMeters(1000),
            isPublic: false,
            createdAt: createdAt,
            updatedAt: updatedAt,
            user: user,
            comments: [],
            tags: [],
            commentCount: 0);

        Assert.Equal(id, course.Id);
        Assert.Equal("T", course.Title);
        Assert.Equal("D", course.Description);
        Assert.Equal(Difficulty.Hard, course.Difficulty);
        Assert.Equal(1000, course.Distance.Meters);
        Assert.False(course.IsPublic);
        Assert.Equal(createdAt, course.CreatedAt);
        Assert.Equal(updatedAt, course.UpdatedAt);
    }

    // ========================================
    // UpdateTitle / UpdateDescription / ChangeDifficulty
    // ========================================

    [Fact]
    public void UpdateTitle_新しいタイトルが反映される()
    {
        var course = MakeMinimalCourse();
        var beforeUpdatedAt = course.UpdatedAt;
        Thread.Sleep(10);

        course.UpdateTitle("New Title");

        Assert.Equal("New Title", course.Title);
        Assert.True(course.UpdatedAt > beforeUpdatedAt, "UpdatedAt が進んでいるべき");
    }

    [Fact]
    public void UpdateTitle_空文字で_ValidationException()
    {
        var course = MakeMinimalCourse();
        Assert.Throws<ValidationException>(() => course.UpdateTitle("   "));
    }

    [Fact]
    public void UpdateDescription_新しい説明が反映される()
    {
        var course = MakeMinimalCourse();
        course.UpdateDescription("New description");
        Assert.Equal("New description", course.Description);
    }

    [Fact]
    public void UpdateDescription_null_でクリアできる()
    {
        var course = MakeMinimalCourse();
        course.UpdateDescription("something");
        course.UpdateDescription(null);
        Assert.Null(course.Description);
    }

    [Fact]
    public void ChangeDifficulty_新しい難易度が反映される()
    {
        var course = MakeMinimalCourse();
        course.ChangeDifficulty(Difficulty.Hard);
        Assert.Equal(Difficulty.Hard, course.Difficulty);
    }

    // ========================================
    // ChangeRoute
    // ========================================

    [Fact]
    public void ChangeRoute_新しいルートが反映され_距離が再計算される()
    {
        var course = MakeMinimalCourse();
        var newRoute = new LineString([
            new Coordinate(139.0, 35.0),
            new Coordinate(139.5, 35.5),
            new Coordinate(140.0, 36.0),
        ]) { SRID = 4326 };

        course.ChangeRoute(newRoute);

        Assert.Equal(newRoute, course.Route);
        Assert.True(course.Distance.Meters > 0);
    }

    [Fact]
    public void ChangeRoute_座標2点未満は_ValidationException()
    {
        var course = MakeMinimalCourse();
        var emptyRoute = new LineString([]) { SRID = 4326 };
        Assert.Throws<ValidationException>(() => course.ChangeRoute(emptyRoute));
    }

    // ========================================
    // Publish / Unpublish
    // ========================================

    [Fact]
    public void Publish_でIsPublicがtrueになる()
    {
        var course = MakeMinimalCourse(isPublic: false);
        course.Publish();
        Assert.True(course.IsPublic);
    }

    [Fact]
    public void Unpublish_でIsPublicがfalseになる()
    {
        var course = MakeMinimalCourse(isPublic: true);
        course.Unpublish();
        Assert.False(course.IsPublic);
    }

    // ========================================
    // ReplaceTags
    // ========================================

    [Fact]
    public void ReplaceTags_新しいタグセットに置き換わる()
    {
        var course = MakeMinimalCourse();
        var newTags = new[] { Tag.Create("a"), Tag.Create("b") };

        course.ReplaceTags(newTags);

        Assert.Equal(2, course.Tags.Count);
        Assert.Contains(course.Tags, t => t.Name == "a");
        Assert.Contains(course.Tags, t => t.Name == "b");
    }

    [Fact]
    public void ReplaceTags_空配列でタグを全削除できる()
    {
        var course = MakeMinimalCourse();
        course.ReplaceTags([Tag.Create("temp")]);

        course.ReplaceTags([]);

        Assert.Empty(course.Tags);
    }

    // ========================================
    // AddComment / EditComment / RemoveComment
    // ========================================

    [Fact]
    public void AddComment_でCommentsに追加され_戻り値で取れる()
    {
        var course = MakeMinimalCourse();
        var authorId = Guid.NewGuid();

        var comment = course.AddComment(authorId, "Hello");

        Assert.Single(course.Comments);
        Assert.Equal(authorId, comment.UserId);
        Assert.Equal("Hello", comment.Body);
    }

    [Fact]
    public void EditComment_本人が編集できる()
    {
        var course = MakeMinimalCourse();
        var authorId = Guid.NewGuid();
        var comment = course.AddComment(authorId, "before");

        var edited = course.EditComment(comment.Id, authorId, "after");

        Assert.Equal("after", edited.Body);
        Assert.True(edited.IsEdited);
    }

    [Fact]
    public void EditComment_他人の編集で_ForbiddenException()
    {
        var course = MakeMinimalCourse();
        var authorId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var comment = course.AddComment(authorId, "x");

        Assert.Throws<ForbiddenException>(() => course.EditComment(comment.Id, otherId, "hack"));
    }

    [Fact]
    public void EditComment_存在しないIDで_NotFoundException()
    {
        var course = MakeMinimalCourse();
        Assert.Throws<NotFoundException>(() =>
            course.EditComment(Guid.NewGuid(), Guid.NewGuid(), "body"));
    }

    [Fact]
    public void RemoveComment_本人が削除できる()
    {
        var course = MakeMinimalCourse();
        var authorId = Guid.NewGuid();
        var comment = course.AddComment(authorId, "x");

        course.RemoveComment(comment.Id, authorId);

        Assert.Empty(course.Comments);
    }

    [Fact]
    public void RemoveComment_他人の削除で_ForbiddenException()
    {
        var course = MakeMinimalCourse();
        var authorId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var comment = course.AddComment(authorId, "x");

        Assert.Throws<ForbiddenException>(() => course.RemoveComment(comment.Id, otherId));
    }

    [Fact]
    public void RemoveComment_存在しないIDで_NotFoundException()
    {
        var course = MakeMinimalCourse();
        Assert.Throws<NotFoundException>(() =>
            course.RemoveComment(Guid.NewGuid(), Guid.NewGuid()));
    }

    // ========================================
    // コレクション不変性(Phase 6 で重要)
    // ========================================

    [Fact]
    public void Comments_はキャスト経由でも改変できない()
    {
        var course = MakeMinimalCourse();
        var comments = course.Comments;

        Assert.Throws<InvalidCastException>(() => _ = (List<Comment>)comments);
    }

    [Fact]
    public void Tags_はキャスト経由でも改変できない()
    {
        var course = MakeMinimalCourse();
        var tags = course.Tags;

        Assert.Throws<InvalidCastException>(() => _ = (List<Tag>)tags);
    }

    // ========================================
    // テストヘルパー
    // ========================================

    private static Course MakeMinimalCourse(Guid? userId = null, bool isPublic = true) =>
        Course.Create(
            userId: userId ?? Guid.NewGuid(),
            title: "Test",
            description: null,
            difficulty: Difficulty.Easy,
            route: MakeRoute(),
            isPublic: isPublic,
            tags: []);

    private static LineString MakeRoute() =>
        new([new Coordinate(135.0, 35.0), new Coordinate(135.1, 35.1)]) { SRID = 4326 };
}

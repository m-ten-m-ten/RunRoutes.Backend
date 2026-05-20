using RunRoutes.Core.Common.Exceptions;
using RunRoutes.Core.Tags;

namespace RunRoutes.Core.Tests.Tags;

public class TagDomainTests
{
    // ========================================
    // Create
    // ========================================

    [Fact]
    public void Create_正常な名前で生成できる()
    {
        var tag = Tag.Create("trail");

        Assert.NotEqual(Guid.Empty, tag.Id);
        Assert.Equal("trail", tag.Name);
        Assert.NotEqual(default, tag.CreatedAt);
    }

    [Fact]
    public void Create_前後の空白がトリムされる()
    {
        var tag = Tag.Create("  trail  ");
        Assert.Equal("trail", tag.Name);
    }

    [Fact]
    public void Create_空白名は_ValidationException()
    {
        Assert.Throws<ValidationException>(() => Tag.Create("   "));
    }

    [Fact]
    public void Create_null名は_ValidationException()
    {
        Assert.Throws<ValidationException>(() => Tag.Create(null!));
    }

    [Fact]
    public void Create_超過長は_ValidationException()
    {
        var longName = new string('a', Tag.MaxNameLength + 1);
        Assert.Throws<ValidationException>(() => Tag.Create(longName));
    }

    [Fact]
    public void Create_境界長は通る()
    {
        var boundaryName = new string('a', Tag.MaxNameLength);
        var tag = Tag.Create(boundaryName);
        Assert.Equal(boundaryName, tag.Name);
    }

    // ========================================
    // Rename
    // ========================================

    [Fact]
    public void Rename_新しい名前が反映される()
    {
        var tag = Tag.Create("before");
        tag.Rename("after");
        Assert.Equal("after", tag.Name);
    }

    [Fact]
    public void Rename_前後の空白がトリムされる()
    {
        var tag = Tag.Create("before");
        tag.Rename("  after  ");
        Assert.Equal("after", tag.Name);
    }

    [Fact]
    public void Rename_同名なら名前が変わらない()
    {
        var tag = Tag.Create("same");
        tag.Rename("same");
        Assert.Equal("same", tag.Name);
    }

    [Fact]
    public void Rename_トリム後同名でも名前が変わらない()
    {
        var tag = Tag.Create("same");
        tag.Rename("  same  ");
        Assert.Equal("same", tag.Name);
    }

    [Fact]
    public void Rename_空白名は_ValidationException()
    {
        var tag = Tag.Create("before");
        Assert.Throws<ValidationException>(() => tag.Rename("   "));
    }

    [Fact]
    public void Rename_超過長は_ValidationException()
    {
        var tag = Tag.Create("before");
        var longName = new string('a', Tag.MaxNameLength + 1);
        Assert.Throws<ValidationException>(() => tag.Rename(longName));
    }

    // ========================================
    // Reconstruct
    // ========================================

    [Fact]
    public void Reconstruct_全フィールドが復元される()
    {
        var id = Guid.NewGuid();
        var createdAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var tag = Tag.Reconstruct(id, "preserved", createdAt, version: 42);

        Assert.Equal(id, tag.Id);
        Assert.Equal("preserved", tag.Name);
        Assert.Equal(createdAt, tag.CreatedAt);
        Assert.Equal(42u, tag.Version);
    }
}

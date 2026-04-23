using Moq;
using RunRoutes.Core.DTOs.Tags;
using RunRoutes.Core.Entities;
using RunRoutes.Core.Exceptions;
using RunRoutes.Core.Interfaces.Repositories;
using RunRoutes.Core.Services;

namespace RunRoutes.Core.Tests;

public class TagServiceTests
{
    private readonly Mock<ITagRepository> _tagRepoMock = new();
    private readonly TagService _sut;

    public TagServiceTests()
    {
        _sut = new TagService(_tagRepoMock.Object);
    }

    private static Tag MakeTag(string name = "trail", uint version = 1, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name,
        CreatedAt = DateTime.UtcNow,
        Version = version,
    };

    [Fact]
    public async Task Create_正常に作成できる()
    {
        var request = new CreateTagRequest("mountain");
        _tagRepoMock.Setup(r => r.ExistsByNameAsync("mountain", null)).ReturnsAsync(false);
        _tagRepoMock.Setup(r => r.AddAsync(It.IsAny<Tag>())).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(request);

        Assert.Equal("mountain", result.Tag.Name);
        _tagRepoMock.Verify(r => r.AddAsync(It.Is<Tag>(t =>
            t.Name == "mountain" && t.Id != Guid.Empty
        )), Times.Once);
    }

    [Fact]
    public async Task Create_空白名でValidationException()
    {
        var request = new CreateTagRequest("   ");

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task Create_超過長でValidationException()
    {
        var request = new CreateTagRequest(new string('a', 51));

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task Create_前後空白をトリムして保存する()
    {
        var request = new CreateTagRequest("  hills  ");
        _tagRepoMock.Setup(r => r.ExistsByNameAsync("hills", null)).ReturnsAsync(false);
        _tagRepoMock.Setup(r => r.AddAsync(It.IsAny<Tag>())).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(request);

        Assert.Equal("hills", result.Tag.Name);
    }

    [Fact]
    public async Task Create_重複名でConflictException()
    {
        var request = new CreateTagRequest("trail");
        _tagRepoMock.Setup(r => r.ExistsByNameAsync("trail", null)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task Update_対象なしでNotFoundException()
    {
        _tagRepoMock.Setup(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>())).ReturnsAsync((Tag?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), new UpdateTagRequest("x", 1)));
    }

    [Fact]
    public async Task Update_同名は重複チェックをスキップする()
    {
        var tag = MakeTag("trail", version: 3);
        _tagRepoMock.Setup(r => r.GetByIdForUpdateAsync(tag.Id)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.UpdateWithConcurrencyCheckAsync(tag, 3u)).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(tag.Id, new UpdateTagRequest("trail", 3));

        Assert.Equal("trail", result.Tag.Name);
        _tagRepoMock.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task Update_別名への変更で重複チェックが走る()
    {
        var tag = MakeTag("trail", version: 5);
        _tagRepoMock.Setup(r => r.GetByIdForUpdateAsync(tag.Id)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.ExistsByNameAsync("mountain", tag.Id)).ReturnsAsync(false);
        _tagRepoMock.Setup(r => r.UpdateWithConcurrencyCheckAsync(tag, 5u)).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(tag.Id, new UpdateTagRequest("mountain", 5));

        Assert.Equal("mountain", result.Tag.Name);
        _tagRepoMock.Verify(r => r.ExistsByNameAsync("mountain", tag.Id), Times.Once);
    }

    [Fact]
    public async Task Update_別IDの同名でConflictException()
    {
        var tag = MakeTag("trail", version: 1);
        _tagRepoMock.Setup(r => r.GetByIdForUpdateAsync(tag.Id)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.ExistsByNameAsync("mountain", tag.Id)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.UpdateAsync(tag.Id, new UpdateTagRequest("mountain", 1)));
    }

    [Fact]
    public async Task Update_並行性違反でConflictException()
    {
        var tag = MakeTag("trail", version: 1);
        _tagRepoMock.Setup(r => r.GetByIdForUpdateAsync(tag.Id)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.ExistsByNameAsync("mountain", tag.Id)).ReturnsAsync(false);
        _tagRepoMock
            .Setup(r => r.UpdateWithConcurrencyCheckAsync(tag, 1u))
            .ThrowsAsync(new ConflictException("stale"));

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.UpdateAsync(tag.Id, new UpdateTagRequest("mountain", 1)));
    }

    [Fact]
    public async Task Delete_対象なしでNotFoundException()
    {
        _tagRepoMock.Setup(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>())).ReturnsAsync((Tag?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid(), 1));
    }

    [Fact]
    public async Task Delete_使用中でConflictException()
    {
        var tag = MakeTag(version: 2);
        _tagRepoMock.Setup(r => r.GetByIdForUpdateAsync(tag.Id)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.HasCoursesAsync(tag.Id)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(tag.Id, 2));

        _tagRepoMock.Verify(
            r => r.DeleteWithConcurrencyCheckAsync(It.IsAny<Tag>(), It.IsAny<uint>()),
            Times.Never);
    }

    [Fact]
    public async Task Delete_未使用なら正常に削除()
    {
        var tag = MakeTag(version: 4);
        _tagRepoMock.Setup(r => r.GetByIdForUpdateAsync(tag.Id)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.HasCoursesAsync(tag.Id)).ReturnsAsync(false);
        _tagRepoMock.Setup(r => r.DeleteWithConcurrencyCheckAsync(tag, 4u)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(tag.Id, 4);

        _tagRepoMock.Verify(r => r.DeleteWithConcurrencyCheckAsync(tag, 4u), Times.Once);
    }

    [Fact]
    public async Task Delete_並行性違反でConflictException()
    {
        var tag = MakeTag(version: 1);
        _tagRepoMock.Setup(r => r.GetByIdForUpdateAsync(tag.Id)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.HasCoursesAsync(tag.Id)).ReturnsAsync(false);
        _tagRepoMock
            .Setup(r => r.DeleteWithConcurrencyCheckAsync(tag, 1u))
            .ThrowsAsync(new ConflictException("stale"));

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(tag.Id, 1));
    }
}

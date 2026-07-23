using RunRoutes.Core.Courses;

namespace RunRoutes.Core.Tests.Courses;

public class DifficultyNamesTests
{
    [Fact]
    public void 難易度の外部表現はAPI契約なので変更時はフロントの修正が必要()
    {
        // このリテラルは CourseForm.tsx の難易度セレクトの value、および
        // 各テストに直打ちされている "easy" 等と対になっている。
        // 赤くなった場合は Difficulty enum の変更が API の破壊的変更に
        // なっていないか（フロント・既存データの移行が必要でないか）を確認すること。
        string[] expected = ["easy", "medium", "hard"];

        Assert.Equal(expected, DifficultyNames.Allowed);
    }
}
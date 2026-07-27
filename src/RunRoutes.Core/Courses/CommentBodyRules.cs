namespace RunRoutes.Core.Courses;

/// <summary>
/// コメント本文に対する入力上の制限。
/// Comment 集約には上限が無く（既存データ保護のため）、外部入力の門番としてのみ課す。
/// </summary>
public static class CommentBodyRules
{
    public const int MaxLength = 1000;
}
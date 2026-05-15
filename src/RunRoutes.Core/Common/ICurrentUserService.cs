namespace RunRoutes.Core.Common;

public interface ICurrentUserService
{
    /// <summary>
    /// 現在の HTTP コンテキストから操作ユーザーの ID を取得する。
    /// HTTP コンテキスト外(バックグラウンドジョブ等)や未認証の場合は null。
    /// </summary>
    Guid? UserId { get; }
}
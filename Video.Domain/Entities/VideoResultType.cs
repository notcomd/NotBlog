namespace Video.Domain.Entities;

/// <summary>
/// 视频操作结果类型
/// </summary>
public enum VideoResultType
{
    /// <summary>
    /// 成功
    /// </summary>
    VideoResultOk,
    /// <summary>
    /// 错误
    /// </summary>
    VideoResultError,
    /// <summary>
    /// 未找到
    /// </summary>
    VideoResultNotFound,

    VideoResultUnauthorized,

    VideoResultForbidden,

    VideoResultBadRequest,

    VideoResultUnprocessableEntity,

    VideoResultInternalServerError,

    VideoResultServiceUnavailable,

    VideoResultGatewayTimeout,

    VideoResultTooManyRequests,

    VideoResultUnavailable,

    VideoResultUnavailableForLegalReasons
}
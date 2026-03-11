namespace Video.Domain.Entities;

public enum VideoResultType
{
    VideoResultOk,

    VideoResultError,

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
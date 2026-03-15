namespace Video.Domain.Entities;

public record IVideoResult<TResult>(
    VideoResultType VideoResultType,
    int ResultCode,
    string? ResultMessage,
    TResult? ResultBody) where TResult : class;

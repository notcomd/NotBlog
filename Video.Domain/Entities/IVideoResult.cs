namespace Video.Domain.Entities;

public record VideoResult<TResult>(
    VideoResultType VideoResultType,
    int ResultCode,
    string? ResultMessage,
    TResult? ResultBody) where TResult : class;

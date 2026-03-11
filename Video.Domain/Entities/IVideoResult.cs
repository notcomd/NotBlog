namespace Video.Domain.Entities;

public record IVideoResult<TResult>(
    VideoResultType VideoResultType,
    int ResultCode,
    string? ResultMessage,
    TResult? ResultBody) where TResult : class;
// {
//     public VideoResultType VideoResultType { get; set; }
//     
//     public int  ResultCode { get; set; }
//     
//     public string? ResultMessage { get; set; }
//     
//     public TResult? ResultBody { get; set; }
//     
//    
// }
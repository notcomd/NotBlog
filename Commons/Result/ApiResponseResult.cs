

namespace Commons.Result;

public sealed class ApiResponseResult<TResponse> 
{

    public long StatusCode { get; init; }

    public string Message { get; init; }

    public TResponse? ResponseData { get; init; }

    public bool IsSuccess { get; init; }

    public DateTime ResponseDateTime{get; init;}

    public ApiResponseResult(long statusCode,string message,TResponse responseData,bool isSuccess){
            StatusCode=statusCode;
            Message=message;
            ResponseData=responseData;
            IsSuccess=isSuccess;
            ResponseDateTime=DateTime.UtcNow;
    }

    public static ApiResponseResult<TResponse> Success(long statusCode,string message,TResponse responseData,bool isSuccess)
    {
        return new ApiResponseResult<TResponse>(statusCode,message,responseData,isSuccess);
    }

}
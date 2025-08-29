using System.Text.Json;

namespace Identity.Domain.Entities
{
    public sealed class IdentityResult<TResponse> 
    {
        private object _lock = new object();
       
        /// <summary>
        /// 返回结果消息
        /// </summary>
        public string ResultMessage { get; set; }
        /// <summary>
        /// 状态码
        /// </summary>
        public EnumStatusCode StatusCode { get; set; }
        /// <summary>
        /// 返回数据
        /// </summary>
        public TResponse? ResultData { get; set; }
        /// <summary>
        /// 返回格式类型
        /// </summary>
        public ResultType ResultType { get; set; }

        public Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;

            response.ContentType = ResultType switch
            {
                ResultType.ApplicationJson => "application/json",
                ResultType.ApplicationXml => "application/xml",
                _ => "application/json"
            };

            response.StatusCode = StatusCode switch
            {
                EnumStatusCode.Ok => 200,
                EnumStatusCode.Error => 400,
                EnumStatusCode.TimeOut => 408,
                EnumStatusCode.Reset => 205,
                EnumStatusCode.NotAuthorized => 401,
                EnumStatusCode.InternalServerError => 500,
                _ => 500
            };

            var resultObj = new
            {
                message = ResultMessage,
                status = StatusCode.ToString(),
                data = ResultData
            };

            if (ResultType == ResultType.ApplicationXml)
            {
                var xmlSerializer = new XmlSerializer(resultObj.GetType());
                using var stringWriter = new StringWriter();
                xmlSerializer.Serialize(stringWriter, resultObj);
                return response.WriteAsync(stringWriter.ToString());
            }
            else
            {
                var json = JsonSerializer.Serialize(resultObj);
                return response.WriteAsync(json);
            }
        }

        public static IdentityResult<TResponse> Result(TResponse data,  EnumStatusCode statusCode, string? message, ResultType resultType = ResultType.ApplicationJson)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = statusCode switch
                {
                    EnumStatusCode.Ok => "操作成功",
                    EnumStatusCode.Error => "操作失败",
                    EnumStatusCode.TimeOut => "请求超时",
                    EnumStatusCode.Reset => "重置成功",
                    EnumStatusCode.NotAuthorized => "未授权",
                    EnumStatusCode.InternalServerError => "服务器内部错误",
                    _ => "未知状态"
                };
            }
            return new IdentityResult<TResponse>
            {
                ResultMessage = message,
                StatusCode = statusCode,
                ResultData = data,
                ResultType = resultType
            };
        }

        public static ValueTask<IdentityResult<TResponse>> ResultAsync(TResponse data, EnumStatusCode statusCode, string? message, ResultType resultType = ResultType.ApplicationJson)
        {
            


            return new ValueTask<IdentityResult<TResponse>>(Result(data,  statusCode, message, resultType));
        }

    }
}
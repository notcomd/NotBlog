using System.Text.Json;

namespace Identity.Domain.Entities
{
    public sealed class IdentityResult<TResponse> : IActionResult where TResponse : class
    {

        private IdentityResult(string resultMessage, EnumStatusCode statusCode, TResponse resultData, ResultType resultType = ResultType.ApplicationJson)
        {
            ResultMessage = resultMessage;
            StatusCode = statusCode;
            ResultData = resultData;
            ResultType = resultType;
        }
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

        public static IdentityResult<TResponse> Success(string message, TResponse data, ResultType resultType = ResultType.ApplicationJson)
        {
            return new IdentityResult<TResponse>(message, EnumStatusCode.Ok, data, resultType);
        }

        public static IdentityResult<TResponse> Error(string message, TResponse data, ResultType resultType = ResultType.ApplicationJson)
        {
            return new IdentityResult<TResponse>(message, EnumStatusCode.Error, data, resultType);
        }

        public static IdentityResult<TResponse> TimeOut(string message, TResponse data, ResultType resultType = ResultType.ApplicationJson)
        {
            return new IdentityResult<TResponse>(message, EnumStatusCode.TimeOut, data, resultType);
        }

        public static IdentityResult<TResponse> Reset(string message, TResponse data, ResultType resultType = ResultType.ApplicationJson)
        {
            return new IdentityResult<TResponse>(message, EnumStatusCode.Reset, data, resultType);
        }

        public static IdentityResult<TResponse> NotAuthorized(string message, TResponse data, ResultType resultType = ResultType.ApplicationJson)
        {
            return new IdentityResult<TResponse>(message, EnumStatusCode.NotAuthorized, data, resultType);
        }

        public static IdentityResult<TResponse> InternalServerError(string message, TResponse data, ResultType resultType = ResultType.ApplicationJson)
        {
            return new IdentityResult<TResponse>(message, EnumStatusCode.InternalServerError, data, resultType);
        }


        public static IdentityResult<TResponse> Other(string message, EnumStatusCode statusCode, TResponse data, ResultType resultType = ResultType.ApplicationJson)
        {
            return new IdentityResult<TResponse>(message, statusCode, data, resultType);
        }
    }
}
namespace Notcomd.Net.Reasult
{
    public class HttpRequest<TResponst>
    {
        public int HttpCode { get; set; }
        public TResponst? HttpResponstBody { get; set; }
        public string? Message { get; set; }


        public HttpRequest(int code, TResponst body, string message)
        {
            HttpCode = code;
            HttpResponstBody = body;
            Message = message;
        }

        public HttpRequest() { }

    }
}

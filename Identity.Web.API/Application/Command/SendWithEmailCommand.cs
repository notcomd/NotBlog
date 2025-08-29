namespace Identity.Web.API.Application.Command
{
    public class SendWithEmailCommand:IRequest<bool>
    {
        public SendWithEmailCommand(string subject,string toEmailAddress, string generatedCode)
        {
            ToEmailAddress = toEmailAddress;
            GeneratedCode = generatedCode;
            Subject = subject;
        }

        public string ToEmailAddress { get; set; }= null!;

        public string GeneratedCode { get; set; }  = null!;

        public string Subject { get; set; } = null!;
    }
}

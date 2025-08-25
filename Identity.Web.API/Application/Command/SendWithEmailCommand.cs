namespace Identity.Web.API.Application.Command
{
    public class SendWithEmailCommand:IRequest<bool>
    {
        public SendWithEmailCommand(string toEmailAddress, string generatedCode)
        {
            ToEmailAddress = toEmailAddress;
            GeneratedCode = generatedCode;
        }

        public string ToEmailAddress { get; set; }= null!;

        public string GeneratedCode { get; set; }  = null!;

    }
}

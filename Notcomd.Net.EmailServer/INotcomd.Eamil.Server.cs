namespace Notcomd.Net.EmailServer
{
    public  interface INotcomd_Email_Server
    {
        Task SendEmailAsync(string UserEmailAdddress,params string[] args);
    }
}

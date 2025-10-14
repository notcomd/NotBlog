using DomainCommon;

namespace Notcomd.DomainCommand
{
    public class NotDateTime : INotDateTime
    {
        public DateTime Now => DateTime.Now;

        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime Today => DateTime.Today;

        public DateTimeOffset NowOffset => DateTimeOffset.Now;


    }
}

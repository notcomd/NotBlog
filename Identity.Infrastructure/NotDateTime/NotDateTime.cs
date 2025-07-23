using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.NotDateTime
{
    public class NotDateTime: INotDateTime
    {
        /// <summary>
        ///     Gets the current date and time.
        /// </summary>
        public DateTime Now => DateTime.Now;
        /// <summary>
        ///     Gets the current date and time in UTC.
        /// </summary>
        public DateTime UtcNow => DateTime.UtcNow;
        /// <summary>
        ///     Gets the current date.
        /// </summary>
        public DateTime Today => DateTime.Today;

        /// <summary>
        ///  get the current date and time with offset.
        /// </summary>
        public DateTimeOffset NowOffset => DateTimeOffset.Now;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.INotDateTime
{
    public interface INotDateTime
    {
        /// <summary>
        ///     Gets the current date and time.
        /// </summary>
        DateTime Now { get; }
        /// <summary>
        ///     Gets the current date and time in UTC.
        /// </summary>
        DateTime UtcNow { get; }
        /// <summary>
        ///     Gets the current date.
        /// </summary>
        DateTime Today { get; }

        DateTimeOffset NowOffset { get; }
    }
}

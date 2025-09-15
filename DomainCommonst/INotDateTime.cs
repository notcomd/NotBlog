namespace DomainCommonst
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

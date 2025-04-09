namespace Video.Domain.Entities;

public record VideoQuote
{

    private readonly static object _lock = new();

    private VideoQuote()
    {

    }
    public long Upvote { get; private set; }

    public long Stars { get; private set; }

    public long Watch { get; private set; }

    public long Down { get; private set; }

    public long Ballot { get; private set; }

    public long Share { get; private set; }

    public static VideoQuote VideoQuoteBuilder()
    {
        return new VideoQuote
        {
            Upvote = 0,
            Stars = 0,
            Watch = 0,
            Down = 0,
            Ballot = 0,
            Share = 0
        };
    }

    public void UpUpvote()
    {
        lock (_lock)
        {
            Upvote += 1;
        }

    }

    public void UpStars()
    {
        lock (_lock)
        {
            Stars += 1;
        }

    }

    public void UpWatch()
    {
        lock (_lock)
        {
            Watch += 1;
        }

    }

    public void UpDown()
    {
        lock (_lock)
        {
            Down += 1;
        }

    }

    public void UpBallot()
    {
        lock (_lock)
        {
            Ballot += 1;
        }

    }

    public void UpShare()
    {
        lock (_lock)
        {
            Share += 1;
        }

    }

    public void DownUpvote()
    {
        lock (_lock)
        {
            if (Upvote == 0) return;
            Upvote -= 1;
        }

    }

    public void DownStars()
    {
        lock (_lock)
        {
            if (Upvote == 0) return;
            Stars -= 1;
        }

    }

    public void DownWatch()
    {
        lock (_lock)
        {
            if (Watch == 0) return;
            Watch -= 1;
        }

    }

    public void DownDown()
    {
        lock (_lock)
        {
            if (Down == 0) return;
            Down -= 1;
        }

    }

    public void DownBallot()
    {
        lock (_lock)
        {
            if (Ballot == 0) return;
            Ballot -= 1;
        }

    }

    public void DownShare()
    {
        lock (_lock)
        {
            if (Share == 0) return;
            Share -= 1;
        }
    }
}
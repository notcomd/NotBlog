namespace Markdown.Domain.Entities;

public class MarkQuote
{
    private readonly object _lock = new object();
    
    private long _loveSome;
    private long _reviewSome;
    private long _commentSome;
    private long _shareSome;
    private long _viewSome;

    public long LoveSome
    {
        get { lock (_lock) return _loveSome; }
    }

    public long ReviewSome
    {
        get { lock (_lock) return _reviewSome; }
    }

    public long CommentSome
    {
        get { lock (_lock) return _commentSome; }
    }

    public long ShareSome
    {
        get { lock (_lock) return _shareSome; }
    }

    public long ViewSome
    {
        get { lock (_lock) return _viewSome; }
    }

    public MarkQuote(long loveSome = 0, long reviewSome = 0, long commentSome = 0, long shareSome = 0, long viewSome = 0)
    {
        _loveSome = loveSome;
        _reviewSome = reviewSome;
        _commentSome = commentSome;
        _shareSome = shareSome;
        _viewSome = viewSome;
    }

    public void AddLove(long count = 1)
    {
        lock (_lock)
        {
            _loveSome += count;
        }
    }

    public void AddReview(long count = 1)
    {
        lock (_lock)
        {
            _reviewSome += count;
        }
    }

    public void AddComment(long count = 1)
    {
        lock (_lock)
        {
            _commentSome += count;
        }
    }

    public void AddShare(long count = 1)
    {
        lock (_lock)
        {
            _shareSome += count;
        }
    }

    public void AddView(long count = 1)
    {
        lock (_lock)
        {
            _viewSome += count;
        }
    }
}
namespace Markdown.Domain.Entities;

public class MarkQuote
{
    /// <summary>
    /// 点赞数
    /// </summary>
    public long LoveSome { get; private set; }

    /// <summary>
    /// 评论数
    /// </summary>
    public long ReviewSome { get; private set; }

    /// <summary>
    /// 回复数
    /// </summary>
    public long CommentSome { get; private set; }

    /// <summary>
    /// 引用数
    /// </summary>
    public long QuoteSome { get; private set; }

    /// <summary>
    /// 引用点赞数
    /// </summary>
    public long QuoteStar { get; private set; }

    /// <summary>
    /// 构造函数，初始化引用相关数据
    /// </summary>
    /// <param name="loveSome">点赞数</param>
    /// <param name="reviewSome">评论数</param>
    /// <param name="commentSome">回复数</param>
    /// <param name="quoteSome">引用数</param>
    /// <param name="quoteStar">引用点赞数</param>
    public MarkQuote(long loveSome, long reviewSome, long commentSome, long quoteSome, long quoteStar)
    {
        LoveSome = loveSome;
        ReviewSome = reviewSome;
        CommentSome = commentSome;
        QuoteSome = quoteSome;
        QuoteStar = quoteStar;
    }

    /// <summary>
    /// 增加点赞数
    /// </summary>
    /// <param name="count">要增加的点赞数，默认增加1</param>
    public void AddLoveSome(long count = 1)
    {
        LoveSome += count;
    }

    /// <summary>
    /// 增加评论数
    /// </summary>
    /// <param name="count">要增加的评论数，默认增加1</param>
    public void AddReviewSome(long count = 1)
    {
        ReviewSome += count;
    }

    /// <summary>
    /// 增加回复数
    /// </summary>
    /// <param name="count">要增加的回复数，默认增加1</param>
    public void AddCommentSome(long count = 1)
    {
        CommentSome += count;
    }

    /// <summary>
    /// 增加引用数
    /// </summary>
    /// <param name="count">要增加的引用数，默认增加1</param>
    public void AddQuoteSome(long count = 1)
    {
        QuoteSome += count;
    }

    /// <summary>
    /// 增加引用点赞数
    /// </summary>
    /// <param name="count">要增加的引用点赞数，默认增加1</param>
    public void AddQuoteStar(long count = 1)
    {
        QuoteStar += count;
    }

    /// <summary>
    /// 获取引用的总互动数
    /// </summary>
    /// <returns>总互动数</returns>
    public long GetTotalInteractions()
    {
        return LoveSome + ReviewSome + CommentSome + QuoteSome + QuoteStar;
    }
}
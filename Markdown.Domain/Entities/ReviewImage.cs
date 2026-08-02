namespace Markdown.Domain.Entities;

public class ReviewImage(Uri imageUrl, string imageName)
{
    public int Id { get; set; }

    public Uri ImageUrl { get; set; } = imageUrl;

    public string ImageName { get; set; } = imageName;

    /// <summary>
    ///     所属评论的外键（指向 MarkReview.Id）
    /// </summary>
    public int MarkReviewId { get; set; }

    /// <summary>
    ///     所属评论导航属性
    /// </summary>
    public MarkReview? MarkReview { get; set; }
}
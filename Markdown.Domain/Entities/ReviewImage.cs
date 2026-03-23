namespace Markdown.Domain.Entities;

public class ReviewImage(Uri imageUrl, string imageName)
{
    public Uri ImageUrl { get; set; } = imageUrl;
    public string ImageName { get; set; } = imageName;
}
namespace Video.Domain.ValueObjects;


public record VideoImage
{
    public Uri ImageUrl { get; }
    public int SortOrder { get; }
    public string? Description { get; }
    
    private VideoImage()
    {
        Description = string.Empty;
        SortOrder = 0;
    }
    
    public VideoImage(Uri imageUrl, int sortOrder = 0, string? description = null)
    {
        ImageUrl = imageUrl ?? throw new ArgumentException("图片URL不能为空", nameof(ImageUrl));
        SortOrder = sortOrder;
        Description = description;
    }
    
    public static VideoImage VideoImageBuilder()
    {
        return new VideoImage();
    }
}
namespace Video.Web.API.Dto.Request;

/// <summary>
/// Comment interaction request — increment or decrement a specific interaction counter on a review.
/// Supported fields: upvote, stars, watch, down, ballot, share
/// </summary>
public class RequestReviewInteraction
{
    /// <summary>视频ID</summary>
    public Guid VideoGuid { get; init; }

    /// <summary>用于修改的互动字段</summary>
    public string Field { get; init;}

    /// <summary>是否增加</summarysummary>
    public bool IsIncrement { get; init; } = true;

    public bool IsValid(out string? error)
    {
        error = null;

        var validFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "upvote", "stars", "watch", "down", "ballot", "share"
        };

        if (VideoGuid == Guid.Empty)
        {
            error = "VideoGuid is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Field))
        {
            error = "Field is required.";
            return false;
        }

        if (!validFields.Contains(Field))
        {
            error = $"Invalid field '{Field}'. Valid fields: upvote, stars, watch, down, ballot, share.";
            return false;
        }

        if (!IsIncrement && Field.Equals("watch", StringComparison.OrdinalIgnoreCase))
        {
            error = "Watch count cannot be decremented.";
            return false;
        }

        return true;
    }
}

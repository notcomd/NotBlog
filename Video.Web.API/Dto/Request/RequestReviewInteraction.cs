namespace Video.Web.API.Dto.Request;

/// <summary>
/// Comment interaction request — increment or decrement a specific interaction counter on a review.
/// Supported fields: upvote, stars, watch, down, ballot, share
/// </summary>
public class RequestReviewInteraction
{
    /// <summary>Video GUID that owns the review.</summary>
    public Guid VideoGuid { get; init; }

    /// <summary>The interaction field to modify.</summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>True to increment (+1), false to decrement (-1). Watch only supports increment.</summary>
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

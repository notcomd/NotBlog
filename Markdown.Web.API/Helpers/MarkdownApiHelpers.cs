using Commons.Security;

namespace Markdown.Web.API.Helpers;

/// <summary>
///     Markdown API 共享辅助方法（角色判断、参数解析、输入校验、可见性规则）
/// </summary>
internal static class MarkdownApiHelpers
{
    /// <summary>
    ///     判断当前用户是否具备管理员角色（用于审核操作授权，F-10.2）。
    ///     统一口径：兼容 Root / Administrator / Admin（大小写不敏感），详见 <see cref="AdminRoleExtensions"/>。
    ///     直接复用 <see cref="ICurrentUserService.IsAdmin"/>，避免与身份实现出现口径分叉。
    /// </summary>
    internal static bool IsAdmin(ICurrentUserService currentUserService)
        => currentUserService.IsAdmin();

    /// <summary>
    ///     获取幂等 key：优先取请求头 Idempotency-Key（客户端提供稳定 key 时幂等保护生效，
    ///     防止网络重试导致重复写入）；缺失/非法时回退为新生成值（保持向后兼容）
    /// </summary>
    internal static Guid GetIdempotencyKey(HttpContext httpContext)
    {
        var header = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
        return Guid.TryParse(header, out var key) ? key : Guid.CreateVersion7();
    }

    /// <summary>
    ///     尝试获取当前用户 ID（未认证或解析失败返回 null，不抛异常）
    /// </summary>
    internal static Guid? TryGetCurrentUserId(ICurrentUserService currentUserService)
    {
        var claim = currentUserService.GetClaim(ClaimTypes.NameIdentifier)
                    ?? currentUserService.GetClaim("sub")
                    ?? currentUserService.GetClaim("user_guid");

        return Guid.TryParse(claim, out var userId) ? userId : null;
    }

    /// <summary>
    ///     评论可见性校验：公开评论所有人可见；私有/受保护评论仅所有者可见
    /// </summary>
    internal static bool IsReviewVisible(MarkReview review, Guid? userId)
    {
        if (review.MarkReviewAuth == MarkReviewAuth.ReviewAuthPublic)
            return true;

        return userId.HasValue && review.UserId == userId.Value;
    }

    /// <summary>
    ///     解析文章权限类型（fail-closed：非法值返回 null，由调用方拒绝请求，避免静默公开）
    /// </summary>
    internal static MarkDownAuth? ParseAuth(string? auth)
    {
        if (string.IsNullOrWhiteSpace(auth))
            return MarkDownAuth.PublicMark;

        return auth.ToLowerInvariant() switch
        {
            "public" => MarkDownAuth.PublicMark,
            "private" => MarkDownAuth.PrivateMark,
            "protected" => MarkDownAuth.ProtectedMark,
            "admin" => MarkDownAuth.AdminMark,
            "root" => MarkDownAuth.RootMark,
            _ => null
        };
    }

    /// <summary>
    ///     解析评论权限类型（fail-closed：非法值返回 null，由调用方拒绝请求）
    /// </summary>
    internal static MarkReviewAuth? ParseReviewAuth(string? auth)
    {
        if (string.IsNullOrWhiteSpace(auth))
            return MarkReviewAuth.ReviewAuthPublic;

        return auth.ToLowerInvariant() switch
        {
            "public" => MarkReviewAuth.ReviewAuthPublic,
            "private" => MarkReviewAuth.ReviewAuthPrivate,
            "protected" => MarkReviewAuth.ReviewAuthProtected,
            _ => null
        };
    }

    /// <summary>
    ///     解析审核状态过滤参数（fail-closed）：
    ///     省略/空白视为「不限」（parsed=null 且返回 true）；非法值返回 false 由调用方拒绝请求
    /// </summary>
    internal static bool TryParseStatus(string? status, out MarkStatus? parsed)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            parsed = null;
            return true;
        }

        if (Enum.TryParse<MarkStatus>(status.Trim(), ignoreCase: true, out var value) && Enum.IsDefined(value))
        {
            parsed = value;
            return true;
        }

        parsed = null;
        return false;
    }

    /// <summary>
    ///     将图片 URL 字符串列表映射为 ReviewImage 域实体（对外 DTO 不直接暴露域实体），
    ///     同时完成基础校验：数量上限、URL 必须为合法的 http/https 绝对地址
    ///     （非法输入抛 InvalidOperationException，由全局异常处理器统一映射为 400）
    /// </summary>
    internal static List<ReviewImage>? MapReviewImages(List<string>? imageUrls)
    {
        if (imageUrls is null || imageUrls.Count == 0)
            return null;

        if (imageUrls.Count > 9)
            throw new InvalidOperationException("评论配图最多 9 张");

        var images = new List<ReviewImage>(imageUrls.Count);
        foreach (var url in imageUrls)
        {
            if (string.IsNullOrWhiteSpace(url) || url.Length > 2048 ||
                !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new InvalidOperationException($"非法图片地址：{url}");

            var name = Path.GetFileName(uri.AbsolutePath);
            images.Add(new ReviewImage(uri, string.IsNullOrWhiteSpace(name) ? "image" : name));
        }

        return images;
    }

    /// <summary>
    ///     校验标签列表：数量上限 20、单标签长度不超过 50（空白标签由领域层过滤）
    /// </summary>
    internal static bool TryValidateTags(List<string>? tags, out string? error)
    {
        if (tags is null)
        {
            error = null;
            return true;
        }

        if (tags.Count > 20)
        {
            error = "标签数量不能超过 20 个";
            return false;
        }

        foreach (var tag in tags)
        {
            if (!string.IsNullOrWhiteSpace(tag) && tag.Length > 50)
            {
                error = "单个标签长度不能超过 50 个字符";
                return false;
            }
        }

        error = null;
        return true;
    }
}

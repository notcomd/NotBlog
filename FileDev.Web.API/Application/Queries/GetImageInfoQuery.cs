namespace FileDev.Web.API.Application.Queries;

/// <summary>获取图片信息（含尺寸解析与归属校验）</summary>
public class GetImageInfoQuery : IRequest<ImageInfoResult>
{
    public Guid UserId { get; set; }
    public Guid FileId { get; set; }
}

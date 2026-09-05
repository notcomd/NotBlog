namespace FileDev.Web.API.Application.Commands;

/// <summary>删除标签（软删除）。文件仍保留，只是不再归属于该标签。</summary>
public record DeleteTagCommand(
    Guid UserId,
    Guid TagId) : IRequest<bool>;
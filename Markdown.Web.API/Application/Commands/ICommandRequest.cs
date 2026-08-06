namespace Markdown.Web.API.Application.Commands;

/// <summary>
///     命令标记接口：TransactionBehavior 仅对实现此接口的请求开启数据库事务，
///     查询等只读请求直接穿透，避免每次 GET 都开事务（P1-7）
/// </summary>
public interface ICommandRequest;

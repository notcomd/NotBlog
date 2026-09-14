namespace Message.Web.API.Application.Queries.Users;

/// <summary>
/// 查找用户查询（添加好友前的用户定位）。
/// <para>安全口径：仅<b>精确匹配</b>，不做模糊/前缀查询，避免被用于批量枚举账号；
/// 关键词含 @ 视为邮箱，否则视为昵称（均忽略大小写）。</para>
/// </summary>
/// <param name="Keyword">邮箱或昵称（调用方负责去除首尾空白并做长度校验）</param>
/// <param name="Limit">昵称命中时的最大返回条数（昵称允许重名）</param>
public record LookupUsersQuery(string Keyword, int Limit) : IRequest<IEnumerable<UserBriefDto>>;

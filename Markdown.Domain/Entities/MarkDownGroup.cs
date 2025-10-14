




using DomainCommon;

namespace Markdown.Domain.Entities;

public class MarkDownGroup : Entity, IAggregateRoot
{
    /// <summary>
    /// 分组名称
    /// </summary>
    public string MarkGroupName { get; private set; } = null!;

    /// <summary>
    /// 分组用户ID
    /// </summary>
    public Guid MarkDownUserId { get; private set; }

    /// <summary>
    /// 关系外键
    /// </summary>
    public ICollection<Guid>? MarkDownsGuid { get; private set; } = new HashSet<Guid>();

    /// <summary>
    /// 分组描述
    /// </summary>
    public string MarkDownGroupDescription { get; private set; } = null!;

    /// <summary>
    /// 分组标签
    /// </summary>
    public ICollection<string>? MarkDownGroupTags { get; private set; } = new HashSet<string>();

    /// <summary>
    /// 关系外键
    /// </summary>
    public ICollection<Guid>? MarkReviews { get; private set; } = new List<Guid>();

    /// <summary>
    /// 分组权限
    /// </summary>
    public MarkDownType MarkDownType { get; private set; } = MarkDownType.PublicMark;

    /// <summary>
    /// 分组创建时间
    /// </summary>
    public DateTime CreateAt { get; private set; } = DateTime.Now;

    /// <summary>
    /// 分组更新时间
    /// </summary>
    public DateTime UpdateAt { get; private set; } = DateTime.Now;


    protected MarkDownGroup()
    {

    }


    public MarkDownGroup(Guid markGroupId, string markGroupName, Guid markDownUserId,
     MarkDownType markDownType = MarkDownType.PublicMark, DateTime createAt = default, DateTime updateAt = default)
    {
        Id = markGroupId;
        MarkGroupName = markGroupName;
        MarkDownUserId = markDownUserId;
        MarkDownType = markDownType;
        CreateAt = createAt == default ? DateTime.Now : createAt;
        UpdateAt = updateAt == default ? DateTime.Now : updateAt;
    }



    /// 更新分组名称
    /// </summary>
    /// <param name="newGroupName">新的分组名称</param>
    public void UpdateGroupName(string newGroupName)
    {
        MarkGroupName = newGroupName;
        UpdateAt = DateTime.Now;
    }

    /// <summary>
    /// 更新分组权限
    /// </summary>
    /// <param name="newAuth">新的分组权限</param>
    public void UpdateMarkDownType(MarkDownType newType)
    {
        MarkDownType = newType;
        UpdateAt = DateTime.Now;
    }

    /// <summary>
    /// 添加MarkDown实体到分组
    /// </summary>
    /// <param name="markDown">要添加的MarkDown实体</param>
    public void AddMarkDown(Guid markDownGuid)
    {
        MarkDownsGuid?.Add(markDownGuid);
        UpdateAt = DateTime.Now;
    }

    /// <summary>
    /// 从分组中移除MarkDown实体
    /// </summary>
    /// <param name="markDown">要移除的MarkDown实体</param>
    public void RemoveMarkDown(Guid markDownGuid)
    {
        MarkDownsGuid?.Remove(markDownGuid);
        UpdateAt = DateTime.Now;
    }

    /// <summary>
    /// 添加MarkReview实体到分组
    /// </summary>
    /// <param name="markReview">要添加的MarkReview实体</param>
    public void AddMarkReview(Guid markReview)
    {
        MarkReviews?.Add(markReview);
        UpdateAt = DateTime.Now;
    }

    /// <summary>
    /// 从分组中移除MarkReview实体
    /// </summary>
    /// <param name="markReview">要移除的MarkReview实体</param>
    public void RemoveMarkReview(Guid markReview)
    {
        MarkReviews?.Remove(markReview);
        UpdateAt = DateTime.Now;
    }

    /// <summary>
    /// 获取MarkDownGroup实体的MarkDown列表
    /// </summary>
    /// <returns>MarkDownGroup实体的MarkDown列表</returns>
    public List<MarkDown> GetMarkDowns()
    {
        return (List<MarkDown>)(MarkDownsGuid ?? new List<Guid>());
    }

    /// <summary>
    /// 获取MarkDownGroup实体的MarkReview列表
    /// </summary>
    /// <returns>MarkDownGroup实体的MarkReview列表</returns>
    public List<Guid> GetMarkReviews()
    {
        return (List<Guid>)(MarkReviews ?? new List<Guid>());
    }

    public void ChangeDescription(string newDescription)
    {
        MarkDownGroupDescription = newDescription;
        UpdateAt = DateTime.Now;
    }
}

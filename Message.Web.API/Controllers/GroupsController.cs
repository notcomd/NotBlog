using Message.Domain.Entities.Group;
using Message.Domain.IProvider;
using Message.Domain.IServices;
using Message.Web.API.Dto;
using Message.Web.API.Dto.Request;
using Message.Web.API.Dto.Response;
using Microsoft.AspNetCore.Mvc;

namespace Message.Web.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupsController(IGroupProvider groupProvider, ICurrentUserService currentUserService)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<GroupDto>>> CreateGroup([FromBody] CreateGroupRequest request)
    {
        var userId = currentUserService.GetUserId();
        var group = await groupProvider.CreateGroupAsync(userId, request.GroupName, request.MaxMembers,
            request.IsPublic);

        if (request.InitialMembers != null && request.InitialMembers.Any())
        {
            foreach (var memberId in request.InitialMembers)
            {
                await groupProvider.AddMemberAsync(group.GroupId, memberId);
            }
        }

        return Ok(ApiResponse<GroupDto>.Created(MapToDto(group), "群组创建成功"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<GroupDto>>>> GetGroups()
    {
        var userId = currentUserService.GetUserId();
        var groups = await groupProvider.GetUserGroupsAsync(userId);
        return Ok(ApiResponse<IEnumerable<GroupDto>>.Ok(groups.Select(MapToDto)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<GroupDto>>> GetGroup(Guid id)
    {
        var group = await groupProvider.GetGroupAsync(id);
        if (group == null)
            return NotFound(ApiResponse<GroupDto>.NotFound("群组不存在"));

        return Ok(ApiResponse<GroupDto>.Ok(MapToDto(group)));
    }

    [HttpPut("{id}/info")]
    public async Task<ActionResult<ApiResponse>> UpdateGroupInfo(Guid id, [FromBody] UpdateGroupInfoRequest request)
    {
        await groupProvider.UpdateGroupInfoAsync(id, request.GroupName, request.Description);
        return Ok(ApiResponse.Ok("群组信息已更新"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DismissGroup(Guid id)
    {
        await groupProvider.DismissGroupAsync(id);
        return Ok(ApiResponse.Ok("群组已解散"));
    }

    [HttpGet("{id}/members")]
    public async Task<ActionResult<ApiResponse<IEnumerable<GroupMemberDto>>>> GetMembers(Guid id)
    {
        var members = await groupProvider.GetMembersAsync(id);
        return Ok(ApiResponse<IEnumerable<GroupMemberDto>>.Ok(members.Select(MapMemberToDto)));
    }

    [HttpPost("{id}/members")]
    public async Task<ActionResult<ApiResponse>> AddMember(Guid id, [FromBody] AddGroupMemberRequest request)
    {
        await groupProvider.AddMemberAsync(id, request.UserId, request.Role);
        return Ok(ApiResponse.Ok("成员已添加"));
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<ActionResult<ApiResponse>> RemoveMember(Guid id, Guid userId)
    {
        await groupProvider.RemoveMemberAsync(id, userId);
        return Ok(ApiResponse.Ok("成员已移除"));
    }

    [HttpPut("{id}/admins")]
    public async Task<ActionResult<ApiResponse>> SetAdmin(Guid id, [FromBody] SetAdminRequest request)
    {
        if (request.IsAdmin)
            await groupProvider.PromoteToAdminAsync(id, request.UserId);
        else
            await groupProvider.DemoteToMemberAsync(id, request.UserId);

        return Ok(ApiResponse.Ok(request.IsAdmin ? "已设为管理员" : "已取消管理员"));
    }

    [HttpPut("{id}/transfer")]
    public async Task<ActionResult<ApiResponse>> TransferOwnership(Guid id, [FromBody] TransferOwnershipRequest request)
    {
        await groupProvider.TransferOwnershipAsync(id, request.NewOwnerId);
        return Ok(ApiResponse.Ok("群主已转让"));
    }

    [HttpPut("{id}/members/{userId}/mute")]
    public async Task<ActionResult<ApiResponse>> MuteMember(Guid id, Guid userId, [FromBody] MuteMemberRequest request)
    {
        await groupProvider.MuteMemberAsync(id, userId, TimeSpan.FromMinutes(request.DurationMinutes));
        return Ok(ApiResponse.Ok("成员已禁言"));
    }

    [HttpDelete("{id}/members/{userId}/mute")]
    public async Task<ActionResult<ApiResponse>> UnmuteMember(Guid id, Guid userId)
    {
        await groupProvider.UnmuteMemberAsync(id, userId);
        return Ok(ApiResponse.Ok("成员已解除禁言"));
    }

    [HttpPut("{id}/members/{userId}/ban")]
    public async Task<ActionResult<ApiResponse>> BanMember(Guid id, Guid userId)
    {
        await groupProvider.BanMemberAsync(id, userId);
        return Ok(ApiResponse.Ok("成员已封禁"));
    }

    [HttpDelete("{id}/members/{userId}/ban")]
    public async Task<ActionResult<ApiResponse>> UnbanMember(Guid id, Guid userId)
    {
        await groupProvider.UnbanMemberAsync(id, userId);
        return Ok(ApiResponse.Ok("成员已解除封禁"));
    }

    [HttpGet("public")]
    public async Task<ActionResult<ApiResponse<IEnumerable<GroupDto>>>> GetPublicGroups()
    {
        var groups = await groupProvider.GetPublicGroupsAsync();
        return Ok(ApiResponse<IEnumerable<GroupDto>>.Ok(groups.Select(MapToDto)));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<PagedResult<GroupDto>>>> SearchGroups(
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var groups = await groupProvider.SearchGroupsAsync(searchTerm, page, pageSize);
        var result = new PagedResult<GroupDto>
        {
            Items = groups.Select(MapToDto).ToList(),
            TotalCount = groups.Count(),
            Page = page,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PagedResult<GroupDto>>.Ok(result));
    }

    [HttpGet("{id}/member-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetMemberCount(Guid id)
    {
        var count = await groupProvider.GetMemberCountAsync(id);
        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpGet("{id}/is-member/{userId}")]
    public async Task<ActionResult<ApiResponse<bool>>> IsMember(Guid id, Guid userId)
    {
        var isMember = await groupProvider.IsMemberAsync(id, userId);
        return Ok(ApiResponse<bool>.Ok(isMember));
    }

    private static GroupDto MapToDto(Group group) => new()
    {
        GroupId = group.GroupId,
        GroupName = group.GroupName,
        Description = group.Description,
        OwnerId = group.OwnerId,
        MaxMembers = group.MaxMembers,
        MemberCount = group.MemberCount,
        IsPublic = group.IsPublic,
        CreatedTime = group.CreatedTime,
        IsDismissed = group.IsDismissed
    };

    private static GroupMemberDto MapMemberToDto(GroupMember member) => new()
    {
        MemberId = member.MemberId,
        GroupId = member.GroupId,
        UserId = member.UserId,
        Role = member.Role,
        Nickname = member.Nickname,
        JoinTime = member.JoinTime,
        IsMuted = member.IsMuted,
        IsBanned = member.IsBanned
    };

    public record SetAdminRequest(Guid UserId, bool IsAdmin);
}
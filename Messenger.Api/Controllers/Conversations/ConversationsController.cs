using Messenger.Application.Conversations.DTOs;
using Messenger.Application.Conversations.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Messenger.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Bắt buộc đăng nhập[cite: 6]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;

    public ConversationsController(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    [HttpGet]
    public async Task<IActionResult> GetConversations()
    {
        var result = await _conversationService.GetConversationsAsync(GetCurrentUserId());
        return Ok(result);
    }

    [HttpPost("private")]
    public async Task<IActionResult> OpenPrivate([FromBody] int friendId)
    {
        var conversationId = await _conversationService.OpenPrivateConversationAsync(GetCurrentUserId(), friendId);
        return Ok(new { ConversationId = conversationId });
    }

    [HttpPost("group")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
    {
        var conversationId = await _conversationService.CreateGroupAsync(GetCurrentUserId(), dto);
        return Ok(new { ConversationId = conversationId });
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> InviteMembers(int id, [FromBody] InviteMembersDto dto)
    {
        await _conversationService.InviteMemberAsync(GetCurrentUserId(), id, dto);
        return Ok();
    }

    [HttpPut("{id}/transfer-owner")]
    public async Task<IActionResult> TransferOwner(int id, [FromBody] int newOwnerId)
    {
        await _conversationService.TransferOwnershipAsync(GetCurrentUserId(), id, newOwnerId);
        return Ok();
    }

    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveGroup(int id)
    {
        await _conversationService.LeaveGroupAsync(GetCurrentUserId(), id);
        return Ok();
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _conversationService.GetMessageAsync(GetCurrentUserId(), id, page, pageSize);
        return Ok(result);
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageDto dto)
    {
        var result = await _conversationService.SendMessageAsync(GetCurrentUserId(), id, dto);
        return Ok(result);
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id, [FromBody] MarkReadDto dto)
    {
        await _conversationService.MarkAsReadAsync(GetCurrentUserId(), id, dto);
        return Ok();
    }
    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers(int id)
    {
        var result = await _conversationService.GetMembersAsync(id);
        return Ok(result);
    }
    [HttpPut("{conversationId}/nickname")]
    public async Task<IActionResult> ChangeNickname(int conversationId,[FromBody] ChangeNicknameDto dto)
    {
        await _conversationService.ChangeNicknameAsync(GetCurrentUserId(), conversationId, dto);
        return Ok(new { Message = "Nickname changed successfully" });
    }

    [HttpPut("{conversationId}/title")]
    public async Task<IActionResult> ChangeGroupTitleAsync(int conversationId, [FromBody] ChangeGroupTitleDto dto)
    {
        await _conversationService.ChangeGroupTitleAsync(GetCurrentUserId(), conversationId, dto);
        return Ok(new { Message = "Group title changed successfully" });
    }
   

}
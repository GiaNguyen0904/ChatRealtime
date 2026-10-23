using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Messenger.Application.Conversations.Interfaces;

namespace Messenger.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IChatNotificationService _notificationService;
    private readonly PresenceTracker _presenceTracker;

    public ChatHub(
        ILogger<ChatHub> logger,
        IChatNotificationService notificationService,
        PresenceTracker presenceTracker)
    {
        _logger = logger;
        _notificationService = notificationService;
        _presenceTracker = presenceTracker;
    }

    public override async Task OnConnectedAsync()
    {
        // lấy thông tin userId từ token
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);

        // nếu user ko có claim id, ngắn kết nối
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId)){
            Context.Abort(); // ngắt kết nối nếu ko có claim id
            return;
        }
        
        var groupName = $"User_{userId}"; // tạo tên group name dựa trên userId, ví dụ User_1,User_2,..
        
        await Groups.AddToGroupAsync( // thêm user vào group cá nhân của họ 
            Context.ConnectionId,// lấy connectionId của user hiện tại
            groupName);

        var isOnline = await _presenceTracker.UserConnected(userId, Context.ConnectionId);
        if (isOnline)
        {
            await Clients.Others.SendAsync("UserIsOnline", userId);
        }

        var currentUsers = await _presenceTracker.GetOnlineUsers();
        await Clients.Caller.SendAsync("GetOnlineUsers", currentUsers);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            var isOffline = await _presenceTracker.UserDisconnected(userId, Context.ConnectionId);
            if (isOffline)
            {
                await Clients.Others.SendAsync("UserIsOffline", userId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<int[]> GetOnlineUsers()
    {
        return await _presenceTracker.GetOnlineUsers();
    }

    public async Task SendTyping(int conversationId, bool isTyping)
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return;
        }

        await _notificationService.SendTypingNotificationAsync(conversationId, userId, isTyping);
    }
}
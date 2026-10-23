using System.Collections.Concurrent;

namespace Messenger.Api.Hubs;

public class PresenceTracker
{
    // Dictionary lưu trữ UserId -> Danh sách ConnectionId của user đó
    private readonly Dictionary<int, HashSet<string>> _onlineUsers = new();

    public Task<bool> UserConnected(int userId, string connectionId)
    {
        bool isOnline = false;

        lock (_onlineUsers) 
        {
            if (_onlineUsers.TryGetValue(userId, out var connections)) // nếu user đã có connection 
            {
                connections.Add(connectionId); // thêm connection vào list
            }
            else
            {
                _onlineUsers.Add(userId, new HashSet<string> { connectionId }); // nếu chưa có thì thêm mới user vào là connection
                isOnline = true; // User bắt đầu có ít nhất 1 kết nối -> chuyển sang Online
            }
        }

        return Task.FromResult(isOnline); // Trả về true nếu user mới online
    }

    public Task<bool> UserDisconnected(int userId, string connectionId)
    {
        bool isOffline = false;

        lock (_onlineUsers) // khóa luồng để tránh xung đột nếu nhiều user disconnect cùng lúc
        {
            if (!_onlineUsers.TryGetValue(userId, out var connections)) // kiểm tra xem nếu user không có trong danh sách onlineUsers
            {
                return Task.FromResult(isOffline); // trả về false
            }

            connections.Remove(connectionId); // xóa connectionId khỏi danh sách 

            if (connections.Count == 0)  // nếu user đó ko có kết nối nào
            {
                _onlineUsers.Remove(userId); // trong danh sách, xóa user đó 
                isOffline = true; // Không còn kết nối nào -> User chính thức Offline
            }
        }

        return Task.FromResult(isOffline); // trả về true nếu offline, ngược lại false
    }

    public Task<int[]> GetOnlineUsers()
    {
        int[] onlineUsers; // danh sách user onl

        lock (_onlineUsers)
        {
            onlineUsers = _onlineUsers.Keys.OrderBy(k => k).ToArray(); // lấy tất cả các user id  trong list onlUser 
        }

        return Task.FromResult(onlineUsers); // trả về danh sách user onl 
    }

    public Task<bool> IsUserOnline(int userId)
    {
        lock (_onlineUsers)
        {
            return Task.FromResult(_onlineUsers.ContainsKey(userId)); // trả về true nếu danh sách user onl có userid truyền vào
        }
    }
}

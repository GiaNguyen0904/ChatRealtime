using Microsoft.EntityFrameworkCore;
using Messenger.Application.Conversations.DTOs;
using Messenger.Application.Conversations.Interfaces;
using Messenger.Application.Common.Exceptions;
using Messenger.Domain.Entities;
using Messenger.Domain.Enums;
using Messenger.Application.Common.Interfaces;
using Messenger.Application.Common;
using System.Runtime.InteropServices;


namespace Messenger.Application.Conversations.Services;
public class ConversationService : IConversationService
{
    private readonly IApplicationDbContext _context;
    private readonly IChatNotificationService _notificationService;
    public ConversationService(IApplicationDbContext context, IChatNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(int currentUserId)
{
    // 1. Lấy danh sách các cuộc hội thoại mà user đang tham gia
    var memberships = await _context.ConversationMembers
        .Include(cm => cm.Conversation)
            .ThenInclude(c => c.Messages)
        .Where(cm => cm.UserId == currentUserId && cm.LeftAt == null)
        .ToListAsync();

    var conversationIds = memberships.Select(m => m.ConversationId).ToList();

    // 2. Lấy TẤT CẢ các thành viên của các cuộc hội thoại này (kèm theo User info)
    // Cách này đảm bảo 100% không bị sót Nickname và FullName
    var allMembers = await _context.ConversationMembers
        .Include(m => m.User)
        .Where(m => conversationIds.Contains(m.ConversationId))
        .ToListAsync();

    var result = new List<ConversationDto>();

    foreach (var member in memberships)
    {
        var conversation = member.Conversation;
        string displayTitle = conversation.Title ?? "";

        // 3. Xử lý tên cho Chat Private
        if (conversation.Type == ConversationType.Private)
        {
            // Tìm người còn lại trong đoạn chat (dựa vào list allMembers đã query ở trên)
            var otherMember = allMembers.FirstOrDefault(m => m.ConversationId == conversation.Id && m.UserId != currentUserId);
            
            if (otherMember != null)
            {
                // NẾU CÓ BIỆT DANH THÌ LẤY BIỆT DANH, KHÔNG THÌ LẤY TÊN THẬT
                displayTitle = !string.IsNullOrWhiteSpace(otherMember.Nickname) 
                                ? otherMember.Nickname 
                                : otherMember.User.FullName;
            }
        }

        var unreadcount = conversation.Messages.Count(m => 
            m.CreatedAt >= member.JoinedAt && 
            (member.LeftAt == null || m.CreatedAt <= member.LeftAt) && 
            (member.LastReadMessageId == null || m.Id > member.LastReadMessageId)); 
        
        result.Add(new ConversationDto
        {
            Id = conversation.Id,
            Type = conversation.Type,
            Title = displayTitle,
            UpdatedAt = conversation.UpdatedAt,
            UnreadCount = unreadcount
        }); 
    }

    return result.OrderByDescending(c => c.UpdatedAt); 
}

    public async Task<int> OpenPrivateConversationAsync( int currentUserId, int friendId)
    {
        string directKey = currentUserId < friendId 
            ? $"{currentUserId}_{friendId}"
            : $"{friendId}_{currentUserId}";

        var existingConv = await _context.Conversations // kiểm tra xem cuộc trò chuyện riêng tư đã tồn tại chưa
                .FirstOrDefaultAsync(c => c.DirectKey == directKey);

        if(existingConv != null) // nếu cuộc trò chuyện đã tồn tại, trả về Id của cuộc trò chuyện đó
        {
            return existingConv.Id;
        }

        // nếu chưa tồn tại cuộc trò chuyện sẽ tạo mới cuộc trò chuyện
        var newConv = new Conversation
        {
            Type= ConversationType.Private,
            DirectKey = directKey,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Conversations.Add(newConv); // thêm cuộc trò chuyện mới vào cơ sở dữ liệu
        await _context.SaveChangesAsync();

        _context.ConversationMembers.AddRange(
            new ConversationMember { ConversationId = newConv.Id, UserId = currentUserId, Role = ConversationMemberRole.Member, JoinedAt = DateTime.UtcNow },
            new ConversationMember { ConversationId = newConv.Id, UserId = friendId, Role = ConversationMemberRole.Member, JoinedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();
        return newConv.Id; // trả về Id của cuộc trò chuyện mới tạo
    }

    public async Task<int> CreateGroupAsync(int currentUserId, CreateGroupDto createGroupDto)
    {
        var conversation = new Conversation
        {
            Type = ConversationType.Group,
            Title = createGroupDto.Title,
            DirectKey = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        _context.ConversationMembers.Add(new ConversationMember
        {
            ConversationId = conversation.Id,
            UserId = currentUserId,
            Role = ConversationMemberRole.Owner,
            JoinedAt = DateTime.UtcNow
        });

        foreach ( var memberId in createGroupDto.MemberIds)
        {
            _context.ConversationMembers.Add(new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = memberId,
                Role = ConversationMemberRole.Member,
                JoinedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return conversation.Id;

    }

    public async Task InviteMemberAsync (int currentUserId, int conversationId, InviteMembersDto inviteMembersDto)
    {
        // check user có quyền mời không
        var currentUser = await _context.ConversationMembers
            .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId 
                                    && cm.UserId == currentUserId
                                    && cm.LeftAt == null);

        if(currentUser == null)
        {
            throw new NotFoundException("You do not have permission to invite members to this conversation.");
        }

        foreach ( var memberId in inviteMembersDto.MemberIds)
        {
            var existingMember = await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == memberId && cm.LeftAt == null);
          if (existingMember == null)
            {
                _context.ConversationMembers.Add(new ConversationMember
                {
                    ConversationId = conversationId,
                    UserId= memberId,
                    Role = ConversationMemberRole.Member, // Người mới vào auto là Member
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task TransferOwnershipAsync( int currentUserId, int conversationId, int newOwnerId)
    {
        var currentMember = await _context.ConversationMembers
            .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == currentUserId && cm.LeftAt== null);

        var newOwner = await _context.ConversationMembers
            .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == newOwnerId && cm.LeftAt== null);
        
        if(currentMember?.Role != ConversationMemberRole.Owner || newOwner == null)
        {
            throw new Exception("You do not have permission to transfer ownership or the new owner is not a member of the conversation.");
        }

        currentMember.Role = ConversationMemberRole.Member;
        newOwner.Role = ConversationMemberRole.Owner;

        await _context.SaveChangesAsync();
    }

    public async Task LeaveGroupAsync ( int currentUserId, int conversationId)
    {
        // check user này có ở trong conversation ko
        var member = await _context.ConversationMembers
            .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == currentUserId && cm.LeftAt == null);
        
        if(member == null) return;

        if(member.Role == ConversationMemberRole.Owner) // nếu còn quyền trưởng nhóm thì phải trao cho người khác
        {
            var otherMemberCount = await _context.ConversationMembers
                .CountAsync(m => m.ConversationId == conversationId && m.UserId != currentUserId && m.LeftAt == null);

            if(otherMemberCount > 0)
            {
                throw new Exception("You must transfer the Owner to others before leaving group");
            }
        }

        member.LeftAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
    }

    public async Task<IEnumerable<MessageDto>> GetMessageAsync(int currentUserId,int conversationId, 
    int page, int pageSize)
    {
        // check xem user có tồn tại trong conversation
       var member = await _context.ConversationMembers
        .Where(cm => cm.ConversationId == conversationId && cm.UserId == currentUserId)
        .OrderByDescending(cm => cm.JoinedAt) // Luôn ưu tiên record ra/vào nhóm mới nhất
        .FirstOrDefaultAsync();
        if(member == null)
        {
            throw new Exception("You are not belong to this conversations");
        }
        // lấy danh sách thành viên hiện tại để map sang trạng thái seen
        var activeMembers = await _context.ConversationMembers
            .Include(m => m.User)
            .Where(m => m.ConversationId == conversationId && m.LeftAt == null)
            .ToListAsync();
        
        var messages = await _context.Messages
            .Where(m => m.ConversationId == conversationId && 
                      //  m.CreatedAt >= member.JoinedAt && 
                        (member.LeftAt == null || m.CreatedAt <= member.LeftAt))
            .OrderByDescending(m => m.CreatedAt) // Mới nhất xếp trước[cite: 1]
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<MessageDto>();
        foreach(var msg in messages)
        {
            var dto = new MessageDto
            {
                Id= msg.Id,
                SenderId = msg.SenderId,
                Content = msg.Content,
                MessageType= msg.MessageType,
                CreatedAt = msg.CreatedAt,
                SeenBy = activeMembers
                            .Where(am => am.UserId != msg.SenderId && am.LastReadMessageId >= msg.Id)
                            .Select(am => new SeenByDto{UserId = am.UserId , FullName = am.User.FullName})
                            .ToList()
            };

            result.Add(dto);
            
        }

        return result.OrderBy(m => m.CreatedAt);

    }

    public async Task<MessageDto> SendMessageAsync(int currentUserId, int conversationId, SendMessageDto dto)
    {
        // check user có tồn tại cho conversation này k
        var member = await _context.ConversationMembers
            .FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == currentUserId && m.LeftAt == null);

        if (member == null) throw new Exception("Can not send messages");

        // tạo 1 message để hứng thông tin truyền từ tham số 
        var message = new Message 
        {
            ConversationId = conversationId,
            SenderId = currentUserId,
            Content = dto.Content,
            MessageType = dto.MessageType,
            CreatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        
        // Cập nhật lại UpdatedAt của Conversation[cite: 1]
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation != null) conversation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Tự động đánh dấu tin nhắn vừa gửi là đã xem cho người gửi
        member.LastReadMessageId = message.Id;
        await _context.SaveChangesAsync();

        var resultDto=  new MessageDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            Content = message.Content,
            MessageType = message.MessageType,
            CreatedAt = message.CreatedAt,
            SeenBy = new List<SeenByDto>()
        };
        // Gửi thông báo đến các client khác trong cuộc trò chuyện
        await _notificationService.SendNewMessageNotificationAsync(conversationId, resultDto);
        return resultDto;
    }

    public async Task MarkAsReadAsync(int currentUserId, int conversationId, MarkReadDto dto)
    {
        var member = await _context.ConversationMembers
            .FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == currentUserId && m.LeftAt == null);

        if (member != null && (member.LastReadMessageId == null || dto.LastReadMessageId > member.LastReadMessageId))
        {
            member.LastReadMessageId = dto.LastReadMessageId;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<MemberDto>> GetMembersAsync(int conversationId)
    {
        var members = await _context.ConversationMembers
            .Include(cm => cm.User)
            .Where(cm => cm.ConversationId == conversationId && cm.LeftAt == null)
            .Select(cm => new MemberDto
            {
                UserId = cm.UserId,
                FullName = cm.User.FullName,
                Role = cm.Role.ToString(),
                Nickname= cm.Nickname,
            })
            .ToListAsync();

            return members;
    }

    public async Task ChangeNicknameAsync(int currentUserId, int conversationId, ChangeNicknameDto dto)
    {
        // kiem tra nguoi doi nickname co trong chat ko
        var isMember = await _context.ConversationMembers
            .AnyAsync(cm => cm.ConversationId == conversationId && cm.UserId == currentUserId && cm.LeftAt == null);

        if (!isMember)
        {
            throw new NotFoundException("You are not a member of this conversation.");
        }
        // tim nguoi doi ten 
        var targetMember = await _context.ConversationMembers
            .FirstOrDefaultAsync(cm => cm.UserId == dto.TargetUserId && cm.ConversationId == conversationId && cm.LeftAt == null);
   
        if(targetMember == null)
        {
            throw new NotFoundException("Target user is not in this conversation.");
        }

        targetMember.Nickname = string.IsNullOrWhiteSpace(dto.NewNickname) ? null : dto.NewNickname.Trim();
        await _context.SaveChangesAsync();
    }

   public async Task ChangeGroupTitleAsync ( int currentUserId, int conversationId, ChangeGroupTitleDto dto)
{
    if (dto == null) throw new Exception("Dữ liệu đổi tên không hợp lệ.");
    
    // 1. Kiểm tra quyền (xem user có trong nhóm không)
    var member = await _context.ConversationMembers
        .FirstOrDefaultAsync(cm => cm.UserId == currentUserId && cm.ConversationId == conversationId && cm.LeftAt == null);
    
    if(member == null)
    {
        throw new NotFoundException("You are not here to change title");
    }

     
    var conversation = await _context.Conversations.FindAsync(conversationId);

    // 3. Truy xuất trực tiếp conversation.Type
    if (conversation == null || conversation.Type != ConversationType.Group)
        throw new Exception("Only change title for group");
    
    // 4. Gán trực tiếp tên mới
    conversation.Title = string.IsNullOrWhiteSpace(dto.NewTitle) ? "Nhóm chưa đặt tên" : dto.NewTitle.Trim();
    conversation.UpdatedAt = DateTime.UtcNow; // Đẩy nhóm lên đầu danh sách     
    
    await _context.SaveChangesAsync();
}
}



    


 
import React, { useState, useEffect } from 'react';
import { useAuthStore } from '../stores/useAuthStore';
import { useChatStore } from '../stores/useChatStore';
import { MainLayout } from '../layouts/MainLayout';
import { ChatNav } from '../components/chat/ChatNav';
import { ChatSidebar } from '../components/chat/ChatSidebar';
import { ChatWindow, type MessageItem } from '../components/chat/ChatWindow';
import FriendList from '../components/contacts/FriendList';
import FriendRequests from '../components/contacts/FriendRequests';
import { SearchUsers } from '../components/contacts/SearchUsers';
import { conversationService } from '../services/conversationService'; // <--- IMPORT THÊM ĐỂ GỌI API TẠO NHÓM
import { signalRService } from '../services/signalRService';
export const ChatPage: React.FC = () => {
  const { user, logout } = useAuthStore();

  const {
    conversations,
    messages,
    members,
    fetchConversations,
    fetchMessages,
    sendMessage,
    markAsRead,
    leaveGroup,
    transferOwner,
    fetchMembers,
    inviteMembers,
    changeNickname,
    changeGroupTitle,
    setActiveConversationId
  } = useChatStore();

  const [activeNav, setActiveNav] = useState<'chats' | 'contacts' | 'settings'>('chats');
  const [activeContactTab, setActiveContactTab] = useState<'list' | 'requests' | 'search'>('list');
  const [selectedChatId, setSelectedChatId] = useState<number | null>(null);
  const [inputMessage, setInputMessage] = useState<string>('');
  useEffect(() => {
      signalRService.startConnection().catch((error) => {
        console.error("Failed to start SignalR:", error);
      });
    }, []);

      useEffect(() => {
        fetchConversations();
      }, [fetchConversations]);

  const activeChatId = selectedChatId ?? (conversations.length > 0 ? conversations[0].id : null);
  useEffect(() => {
    if (setActiveConversationId) {
      setActiveConversationId(activeChatId);
    }
  }, [activeChatId, setActiveConversationId]);

  useEffect(() => {
    if (!activeChatId) return;

    fetchMessages(activeChatId).then(async (fetchedMessages) => {
      if (fetchedMessages.length > 0) {
        const lastMsgId = fetchedMessages[fetchedMessages.length - 1].id;
        await markAsRead(activeChatId, lastMsgId);
        await fetchConversations();
      }
    });
    fetchMembers(activeChatId);
  }, [activeChatId, fetchMessages, markAsRead, fetchConversations, fetchMembers]);

  const currentUserId = user?.id ? Number(user.id) : -1;

  const currentUserAvatar =
    user?.fullName
      ?.trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((name) => name.charAt(0))
      .join('')
      .toUpperCase() || 'ME';

  const mappedConversations = conversations.map((c) => ({
    id: c.id,
    type: c.type,
    name: c.title || `Chat cá nhân #${c.id}`,
    lastMessage: c.unreadCount > 0 ? `Có ${c.unreadCount} tin nhắn mới` : 'Không có tin mới',
    time: new Date(c.updatedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    avatar: c.title ? c.title.substring(0, 2).toUpperCase() : 'PC',
    unreadCount: c.unreadCount,
  }));

  const currentChat = mappedConversations.find((c) => c.id === activeChatId);

  const getDisplayName = (userId: number, fallbackName: string) => {
    const member = members.find(m => m.userId === userId);
    return member?.nickname || member?.fullName || fallbackName;
  };

  const mappedMessages: MessageItem[] = messages.map((m) => {
    const messageSenderId = m.senderId ? Number(m.senderId) : -999;
    const isMe = currentUserId !== -1 && messageSenderId !== -999 && messageSenderId === currentUserId;

    const senderInfo = m.seenBy?.find((s) => Number(s.userId) === messageSenderId);
    
    const senderName = isMe 
      ? getDisplayName(currentUserId, user?.fullName || 'Me') 
      : getDisplayName(messageSenderId, senderInfo?.fullName || `User ${messageSenderId}`);

    const getAvatar = (name: string) => name.trim().split(/\s+/).slice(0, 2).map((n) => n.charAt(0)).join('').toUpperCase();
    const senderAvatar = isMe ? currentUserAvatar : getAvatar(senderName);

    const seenByData = m.seenBy
      ?.filter(s => Number(s.userId) !== messageSenderId)
      .map(s => ({
        userId: s.userId,
        fullName: getDisplayName(s.userId, s.fullName),
        avatar: getAvatar(getDisplayName(s.userId, s.fullName))
      })) || [];

    return {
      id: m.id,
      sender: isMe ? 'me' : 'them',
      senderName,
      senderAvatar,
      text: m.content,
      time: new Date(m.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      rawDate: new Date(m.createdAt),
      seenBy: seenByData,
    };
  });

  const handleSendMessage = (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputMessage.trim() || !activeChatId) return;
    sendMessage(activeChatId, inputMessage);
    setInputMessage('');
  };

  // <--- THÊM HÀM XỬ LÝ TẠO NHÓM MỚI
  const handleCreateGroup = async (title: string, memberIds: number[]) => {
    try {
      const finalTitle = title.trim() === '' ? 'Nhóm chat' : title.trim();
      const res = await conversationService.createGroupChat({ title: finalTitle, memberIds });
      await fetchConversations();
      setSelectedChatId(res.data.conversationId); // Chuyển sang chat vừa tạo
    } catch (error) {
      console.error('Lỗi khi tạo nhóm:', error);
      alert('Không thể tạo nhóm. Vui lòng thử lại!');
    }
  };

  return (
    <MainLayout
      topNav={
        <ChatNav activeNav={activeNav} setActiveNav={setActiveNav} userEmail={user?.email} onLogout={logout} />
      }
      subSidebar={
        <ChatSidebar
          activeNav={activeNav}
          conversations={mappedConversations}
          selectedChatId={activeChatId || 0}
          onSelectChat={setSelectedChatId}
          activeContactTab={activeContactTab}
          setActiveContactTab={setActiveContactTab}
          userEmail={user?.email}
          currentUserId={currentUserId} // <--- TRUYỀN ID XUỐNG ĐỂ FILTER TÌM KIẾM
          onLeaveGroup={leaveGroup}
          onCreateGroup={handleCreateGroup} // <--- TRUYỀN HÀM XUỐNG SIDEBAR
        />
      }
    >
      {activeNav === 'chats' && activeChatId && currentChat && (
        <ChatWindow
          currentChat={currentChat}
          currentUserId={currentUserId} 
          members={members}
          messages={mappedMessages}
          inputMessage={inputMessage}
          setInputMessage={setInputMessage}
          onSendMessage={handleSendMessage}
          onTransferOwner={transferOwner}
          onLeaveGroup={leaveGroup}
          onInviteMembers={inviteMembers}
          onChangeNickname={changeNickname}
          onChangeGroupTitle={changeGroupTitle}
        />
      )}

      {activeNav === 'contacts' && (
        <div className="flex-1 p-8 overflow-y-auto bg-gray-50/30">
          {activeContactTab === 'list' && <FriendList />}
          {activeContactTab === 'requests' && <FriendRequests />}
          {activeContactTab === 'search' && <SearchUsers />}
        </div>
      )}

      {activeNav === 'settings' && (
        <div className="flex-1 p-8 text-gray-400 flex items-center justify-center bg-white">
          <p className="text-xs">Chức năng Cài đặt đang được phát triển.</p>
        </div>
      )}
    </MainLayout>
  );
};
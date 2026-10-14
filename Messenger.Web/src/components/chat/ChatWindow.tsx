import React, { useState, useEffect, useRef } from 'react';
import axiosClient from '../../api/axiosClient';
import { useChatStore } from '../../stores/useChatStore';
import { signalRService } from '../../services/signalRService';

export interface SeenByItem {
  userId: number;
  fullName: string;
  avatar: string;
}

export interface MessageItem {
  id: number;
  sender: 'me' | 'them';
  senderName?: string;
  senderAvatar?: string;
  text: string;
  time: string;
  rawDate: Date;
  seenBy: SeenByItem[];
}

export interface ChatMember {
  userId: number;
  fullName: string;
  role: string;
  nickname?: string;
}

export interface UserSearchDto {
  id: number;
  email: string;
  fullName: string;
}

interface ChatWindowProps {
  currentChat?: {
    id: number;
    name: string;
    avatar: string;
    type: 'PRIVATE' | 'GROUP' | string;
  };
  currentUserAvatar?: string;
  currentUserId: number;
  members: ChatMember[];
  messages: MessageItem[];
  inputMessage: string;
  setInputMessage: (val: string) => void;
  onSendMessage: (e: React.FormEvent) => void;
  onTransferOwner?: (chatId: number, newOwnerId: number) => void;
  onLeaveGroup?: (chatId: number) => void;
  onInviteMembers?: (chatId: number, memberIds: number[]) => void;
  onChangeNickname?: (chatId: number, targetUserId: number, newNickname: string | null) => void; 
  onChangeGroupTitle?: (chatId: number, newTitle: string) => void;
}

export const ChatWindow: React.FC<ChatWindowProps> = ({
  currentChat,
  currentUserId,
  members,
  messages,
  inputMessage,
  setInputMessage,
  onSendMessage,
  onTransferOwner,
  onLeaveGroup,
  onInviteMembers,
  onChangeNickname,
  onChangeGroupTitle,
}) => {
  const isGroup = currentChat?.type?.toUpperCase() === 'GROUP';
  
  const [showInfoPanel, setShowInfoPanel] = useState(false);
  const [isMembersOpen, setIsMembersOpen] = useState(true);

  // State Modal Nickname
  const [showNicknameModal, setShowNicknameModal] = useState(false);
  const [targetNicknameUser, setTargetNicknameUser] = useState<ChatMember | null>(null);
  const [nicknameInput, setNicknameInput] = useState('');
  
  // State quản lý việc chuyển quyền
  const [selectedNewOwner, setSelectedNewOwner] = useState<number | null>(null);
  
  // State Modal Thêm người
  const [showAddModal, setShowAddModal] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<UserSearchDto[]>([]);
  const [selectedUsersToAdd, setSelectedUsersToAdd] = useState<number[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  
  // State Đổi tên nhóm
  const [showGroupTitleModal, setShowGroupTitleModal] = useState(false);
  const [groupTitleInput, setGroupTitleInput] = useState('');

  // Auto-scroll ref
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Typing indicator refs & state
  const typingTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const isTypingRef = useRef(false);

  const typingUsersMap = useChatStore((state) => state.typingUsers);
  const currentChatTypingUsers = currentChat?.id ? (typingUsersMap[currentChat.id] || {}) : {};
  const typingUserIds = Object.keys(currentChatTypingUsers)
    .map(Number)
    .filter((uid) => uid !== currentUserId && currentChatTypingUsers[uid]);

  // Tự động cuộn xuống khi có tin nhắn mới hoặc có người gõ
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages.length, typingUserIds.length]);

  // Hủy trạng thái typing khi chuyển sang cuộc hội thoại khác
  useEffect(() => {
    return () => {
      if (isTypingRef.current && currentChat?.id) {
        isTypingRef.current = false;
        signalRService.sendTyping(currentChat.id, false);
      }
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
    };
  }, [currentChat?.id]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setInputMessage(value);

    if (!currentChat?.id) return;

    if (value.trim().length > 0) {
      if (!isTypingRef.current) {
        isTypingRef.current = true;
        signalRService.sendTyping(currentChat.id, true);
      }

      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }

      typingTimeoutRef.current = setTimeout(() => {
        isTypingRef.current = false;
        if (currentChat?.id) {
          signalRService.sendTyping(currentChat.id, false);
        }
      }, 2500);
    } else {
      if (isTypingRef.current) {
        isTypingRef.current = false;
        if (typingTimeoutRef.current) {
          clearTimeout(typingTimeoutRef.current);
        }
        signalRService.sendTyping(currentChat.id, false);
      }
    }
  };

  const handleFormSubmit = (e: React.FormEvent) => {
    if (isTypingRef.current && currentChat?.id) {
      isTypingRef.current = false;
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
      signalRService.sendTyping(currentChat.id, false);
    }
    onSendMessage(e);
  };

  const getTypingText = () => {
    if (typingUserIds.length === 0) return null;

    const names = typingUserIds.map((uid) => {
      const member = members.find((m) => m.userId === uid);
      return member?.nickname || member?.fullName || `User ${uid}`;
    });

    if (names.length === 1) {
      return `${names[0]} đang soạn tin...`;
    } else if (names.length === 2) {
      return `${names[0]} và ${names[1]} đang soạn tin...`;
    } else {
      return `${names[0]} và ${names.length - 1} người khác đang soạn tin...`;
    }
  };

  const currentUserMember = members.find(m => m.userId === currentUserId);
  const isCurrentUserOwner = currentUserMember?.role?.toUpperCase() === 'OWNER';

  // Cập nhật tìm kiếm / Lấy danh sách mặc định
  useEffect(() => {
    if (!showAddModal) return;

    const fetchUsers = async () => {
      setIsSearching(true);
      try {
        const res = await axiosClient.get<UserSearchDto[]>(`/Users/search?keyword=${encodeURIComponent(searchQuery)}`);
        const filteredUsers = res.data.filter((u: UserSearchDto) => 
          !members.some(m => m.userId === u.id)
        );
        setSearchResults(filteredUsers);
      } catch (error) {
        console.error("Lỗi khi tìm kiếm user:", error);
      } finally {
        setIsSearching(false);
      }
    };

    if (searchQuery.trim().length === 0) {
      fetchUsers();
    } else {
      const delayDebounceFn = setTimeout(() => {
        fetchUsers();
      }, 400);
      return () => clearTimeout(delayDebounceFn);
    }
  }, [searchQuery, members, showAddModal]);

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(e.target.value);
  };

  // --- XỬ LÝ ĐỔI BIỆT DANH ---
  const openNicknameModal = (member: ChatMember) => {
    setTargetNicknameUser(member);
    setNicknameInput(member.nickname || member.fullName);
    setShowNicknameModal(true);
  };

  const handleSaveNickname = () => {
    if (currentChat && targetNicknameUser) {
      const finalName = nicknameInput.trim();
      onChangeNickname?.(currentChat.id, targetNicknameUser.userId, finalName === '' ? null : finalName);
      setShowNicknameModal(false);
    }
  };

  const handleRemoveNickname = () => {
    if (currentChat && targetNicknameUser) {
      onChangeNickname?.(currentChat.id, targetNicknameUser.userId, null);
      setShowNicknameModal(false);
    }
  };

  // --- XỬ LÝ THÊM THÀNH VIÊN ---
  const toggleUserSelection = (userId: number) => {
    if (selectedUsersToAdd.includes(userId)) {
      setSelectedUsersToAdd(selectedUsersToAdd.filter(id => id !== userId));
    } else {
      setSelectedUsersToAdd([...selectedUsersToAdd, userId]);
    }
  };

  const handleInviteConfirm = () => {
    if (currentChat && selectedUsersToAdd.length > 0) {
      onInviteMembers?.(currentChat.id, selectedUsersToAdd);
      closeAddModal();
    }
  };

  const closeAddModal = () => {
    setShowAddModal(false);
    setSearchQuery('');
    setSearchResults([]);
    setSelectedUsersToAdd([]);
  };

  // --- XỬ LÝ QUẢN TRỊ NHÓM ---
  const handleTransferConfirm = () => {
    if (selectedNewOwner && currentChat) {
      onTransferOwner?.(currentChat.id, selectedNewOwner);
      setSelectedNewOwner(null); 
    }
  };

  const handleLeaveGroupConfirm = () => {
    if (currentChat && window.confirm("Bạn có chắc chắn muốn rời nhóm này?")) {
      onLeaveGroup?.(currentChat.id);
    }
  };

  const openGroupTitleModal = () => {
    if (currentChat) {
        setGroupTitleInput(currentChat.name);
        setShowGroupTitleModal(true);
    }
  };

  const handleSaveGroupTitle = () => {
    if (currentChat && groupTitleInput.trim()) {
        onChangeGroupTitle?.(currentChat.id, groupTitleInput.trim());
        setShowGroupTitleModal(false);
    }
  };

  // Tính năng chưa hoàn thiện
  const alertWIP = () => alert("Tính năng này đang được phát triển!");

  // --- RENDER HELPERS ---
  const renderTimeDivider = (currentMsg: MessageItem, prevMsg?: MessageItem) => {
    if (!prevMsg) {
       return (
        <div className="flex justify-center my-6">
          <span className="text-gray-500 text-[11px] font-semibold">
            {currentMsg.rawDate.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })} {currentMsg.rawDate.toLocaleDateString('vi-VN')}
          </span>
        </div>
      );
    }
    const diffMs = currentMsg.rawDate.getTime() - prevMsg.rawDate.getTime();
    const diffHours = diffMs / (1000 * 60 * 60);

    if (diffHours >= 2) {
      return (
        <div className="flex justify-center my-6">
          <span className="text-gray-500 text-[11px] font-semibold">
            {currentMsg.rawDate.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })} {diffHours >= 24 ? currentMsg.rawDate.toLocaleDateString('vi-VN') : ''}
          </span>
        </div>
      );
    }
    return null;
  };

  const getBubbleRadius = (isMe: boolean, isGroupedWithPrev: boolean, isGroupedWithNext: boolean) => {
    if (isMe) {
      if (!isGroupedWithPrev && !isGroupedWithNext) return 'rounded-2xl';
      if (!isGroupedWithPrev && isGroupedWithNext) return 'rounded-2xl rounded-tr-sm';
      if (isGroupedWithPrev && isGroupedWithNext) return 'rounded-2xl rounded-r-sm';
      if (isGroupedWithPrev && !isGroupedWithNext) return 'rounded-2xl rounded-br-sm';
    } else {
      if (!isGroupedWithPrev && !isGroupedWithNext) return 'rounded-2xl';
      if (!isGroupedWithPrev && isGroupedWithNext) return 'rounded-2xl rounded-tl-sm';
      if (isGroupedWithPrev && isGroupedWithNext) return 'rounded-2xl rounded-l-sm';
      if (isGroupedWithPrev && !isGroupedWithNext) return 'rounded-2xl rounded-bl-sm';
    }
    return 'rounded-2xl';
  };

  return (
    <div className="flex h-full w-full bg-white overflow-hidden relative">
      {/* ================= KHU VỰC CHAT CHÍNH ================= */}
      <div className="flex-1 flex flex-col h-full min-w-0 border-r border-gray-200">
        
        {/* HEADER */}
        <div className="p-3 px-4 border-b border-gray-100 flex items-center justify-between bg-white shadow-sm z-10">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-gray-100 text-gray-700 font-bold rounded-full flex items-center justify-center text-sm">
              {currentChat?.avatar}
            </div>
            <div>
              <h3 className="font-bold text-[15px] text-gray-900 leading-tight">{currentChat?.name}</h3>
              <span className="text-[12px] text-gray-500 font-medium">Đang hoạt động</span>
            </div>
          </div>

          <button 
            onClick={() => setShowInfoPanel(!showInfoPanel)}
            className={`p-2 rounded-full transition ${showInfoPanel ? 'bg-blue-50 text-blue-600' : 'text-gray-500 hover:bg-gray-100'}`}
          >
            <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24">
              <circle cx="12" cy="5" r="2"/><circle cx="12" cy="12" r="2"/><circle cx="12" cy="19" r="2"/>
            </svg>
          </button>
        </div>

        {/* MESSAGE FEED */}
        <div className="flex-1 overflow-y-auto p-4 bg-white flex flex-col">
          {messages.map((msg, index) => {
            const isMe = msg.sender === 'me';
            const prevMsg = index > 0 ? messages[index - 1] : undefined;
            const nextMsg = index < messages.length - 1 ? messages[index + 1] : undefined;

            const isGroupedWithPrev = prevMsg && prevMsg.senderName === msg.senderName && (msg.rawDate.getTime() - prevMsg.rawDate.getTime()) < 300000;
            const isGroupedWithNext = nextMsg && nextMsg.senderName === msg.senderName && (nextMsg.rawDate.getTime() - msg.rawDate.getTime()) < 300000;

            const bubbleRadius = getBubbleRadius(isMe, !!isGroupedWithPrev, !!isGroupedWithNext);
            const marginTop = isGroupedWithPrev ? 'mt-0.5' : 'mt-4';

            return (
              <React.Fragment key={msg.id}>
                {renderTimeDivider(msg, prevMsg)}

                <div className={`flex w-full ${marginTop} ${isMe ? 'justify-end' : 'justify-start'}`}>
                  <div className={`flex items-end gap-2 max-w-[75%] ${isMe ? 'flex-row-reverse' : 'flex-row'}`}>
                    
                    {!isMe && (
                      <div className="flex-shrink-0 flex flex-col justify-end h-full">
                        {!isGroupedWithNext ? (
                          <div className="w-7 h-7 bg-gray-200 text-gray-700 font-bold rounded-full flex items-center justify-center text-[10px]" title={msg.senderName}>
                            {msg.senderAvatar || 'U'}
                          </div>
                        ) : (
                          <div className="w-7 h-7" />
                        )}
                      </div>
                    )}

                    <div className={`flex flex-col ${isMe ? 'items-end' : 'items-start'}`}>
                      {!isMe && isGroup && !isGroupedWithPrev && msg.senderName && (
                        <span className="text-[12px] text-gray-500 font-medium mb-1 ml-1">{msg.senderName}</span>
                      )}

                      <div className={`px-4 py-2 text-[15px] leading-relaxed ${
                        isMe ? 'bg-[#0084ff] text-white' : 'bg-[#e4e6eb] text-black'
                      } ${bubbleRadius}`}>
                        {msg.text}
                      </div>

                      {!isGroupedWithNext && (
                        <div className="flex items-center gap-1 mt-1">
                          {isMe && msg.seenBy && msg.seenBy.length > 0 && (
                            <div className="flex -space-x-1.5 mt-0.5">
                              {msg.seenBy.map((user) => (
                                <div key={user.userId} className="w-3.5 h-3.5 bg-gray-300 border border-white rounded-full flex items-center justify-center text-[6px] text-gray-700 font-bold" title={user.fullName}>
                                  {user.avatar}
                                </div>
                              ))}
                            </div>
                          )}
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </React.Fragment>
            );
          })}

          {/* TYPING INDICATOR BUBBLE */}
          {typingUserIds.length > 0 && (
            <div className="flex items-center gap-2 pt-1 animate-in fade-in duration-200">
              <div className="flex items-center gap-1.5 bg-[#e4e6eb] px-3.5 py-2.5 rounded-2xl rounded-bl-sm shadow-sm">
                <span className="w-1.5 h-1.5 bg-gray-600 rounded-full animate-bounce [animation-delay:-0.3s]"></span>
                <span className="w-1.5 h-1.5 bg-gray-600 rounded-full animate-bounce [animation-delay:-0.15s]"></span>
                <span className="w-1.5 h-1.5 bg-gray-600 rounded-full animate-bounce"></span>
              </div>
              <span className="text-[12px] text-gray-500 italic font-medium">
                {getTypingText()}
              </span>
            </div>
          )}

          <div ref={messagesEndRef} />
        </div>

        {/* INPUT AREA */}
        <form onSubmit={handleFormSubmit} className="p-3 border-t border-gray-200 flex items-center gap-2 bg-white">
          <input
            type="text"
            value={inputMessage}
            onChange={handleInputChange}
            placeholder="Aa"
            className="flex-1 bg-[#f0f2f5] text-gray-900 rounded-full px-4 py-2 text-[15px] focus:outline-none"
          />
          <button 
            type="submit" 
            disabled={!inputMessage.trim()}
            className="text-[#0084ff] disabled:text-[#0084ff]/40 p-2 transition-colors"
          >
            <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24"><path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z"/></svg>
          </button>
        </form>
      </div>

      {/* ================= THÔNG TIN ĐOẠN CHAT (Bên phải) ================= */}
      {showInfoPanel && (
        <div className="w-[340px] bg-white flex flex-col h-full overflow-y-auto flex-shrink-0 border-l border-gray-200 animate-in slide-in-from-right-10 duration-200">
          
          <div className="flex flex-col items-center pt-8 pb-4">
            <div className="w-20 h-20 bg-gray-100 text-gray-700 font-bold rounded-full flex items-center justify-center text-3xl shadow-sm mb-3">
              {currentChat?.avatar}
            </div>
            <h2 className="font-bold text-[17px] text-gray-900 px-4 text-center">{currentChat?.name}</h2>
          </div>

          <div className="flex justify-center gap-6 px-4 mb-6">
              {isGroup ? (
                <>
                  <div className="flex flex-col items-center gap-1 cursor-pointer group" onClick={() => setShowAddModal(true)}>
                    <div className="w-9 h-9 bg-gray-100 group-hover:bg-gray-200 rounded-full flex items-center justify-center text-gray-800 transition">
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4"></path></svg>
                    </div>
                    <span className="text-[12px] text-gray-600 font-medium">Thêm</span>
                  </div>
                  
                  {/* NÚT MỚI: ĐỔI TÊN NHÓM */}
                  <div className="flex flex-col items-center gap-1 cursor-pointer group" onClick={openGroupTitleModal}>
                    <div className="w-9 h-9 bg-gray-100 group-hover:bg-gray-200 rounded-full flex items-center justify-center text-gray-800 transition">
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"></path></svg>
                    </div>
                    <span className="text-[12px] text-gray-600 font-medium">Đổi tên</span>
                  </div>

                  <div className="flex flex-col items-center gap-1 cursor-pointer group" onClick={handleLeaveGroupConfirm}>
                    <div className="w-9 h-9 bg-gray-100 group-hover:bg-gray-200 rounded-full flex items-center justify-center text-gray-800 transition">
                      <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path></svg>
                    </div>
                    <span className="text-[12px] text-gray-600 font-medium">Rời nhóm</span>
                  </div>
                </>
              ) : (
                <>
              {/* CHAT PRIVATE VẪN LÀ NÚT BIỆT DANH */}
                <div className="flex flex-col items-center gap-1 cursor-pointer group" onClick={() => setIsMembersOpen(true)}>
                  <div className="w-9 h-9 bg-gray-100 group-hover:bg-gray-200 rounded-full flex items-center justify-center text-gray-800 transition">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"></path></svg>
                  </div>
                  <span className="text-[12px] text-gray-600 font-medium">Biệt danh</span>
                </div>
                {/* Nút Chặn (giữ nguyên với alertWIP) */}
                <div className="flex flex-col items-center gap-1 cursor-pointer group" onClick={alertWIP}>
                  <div className="w-9 h-9 bg-gray-100 group-hover:bg-gray-200 rounded-full flex items-center justify-center text-gray-800 transition">
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636"></path></svg>
                  </div>
                  <span className="text-[12px] text-gray-600 font-medium">Chặn</span>
                </div>
              </>
            )}
          </div>

          {/* MENU DANH SÁCH THÀNH VIÊN (Hiện cho cả Nhóm và Chat Riêng) */}
          <div className="flex flex-col border-t border-gray-100 pt-2">
            <button 
              onClick={() => setIsMembersOpen(!isMembersOpen)}
              className="w-full flex items-center justify-between px-4 py-3 hover:bg-gray-50 transition"
            >
              <span className="font-semibold text-[15px] text-gray-800">{isGroup ? 'Thành viên trong đoạn chat' : 'Tùy chỉnh đoạn chat'}</span>
              <svg className={`w-5 h-5 text-gray-500 transition-transform duration-200 ${isMembersOpen ? 'rotate-180' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 9l-7 7-7-7"></path></svg>
            </button>

            {isMembersOpen && (
              <div className="px-2 pb-4">
                {members.map(member => {
                  const displayName = member.nickname || member.fullName;
                  const isUserMe = member.userId === currentUserId;

                  return (
                    <div key={member.userId} className="flex items-center justify-between p-2 hover:bg-gray-100 rounded-lg transition group">
                      <div className="flex items-center gap-3">
                        <div className="w-9 h-9 bg-gray-200 text-gray-700 rounded-full flex justify-center items-center text-xs font-bold">
                          {displayName.charAt(0).toUpperCase()}
                        </div>
                        <div className="flex flex-col justify-center">
                          <p className="text-[14px] font-semibold text-gray-900 leading-none">
                            {displayName} {isUserMe && <span className="text-gray-500 font-normal">(Bạn)</span>}
                          </p>
                          <p className="text-[12px] text-gray-500 mt-1.5 leading-none">
                            {member.nickname ? `Tên thật: ${member.fullName}` : (member.role === 'Owner' ? 'Quản trị viên' : 'Thành viên')}
                          </p>
                        </div>
                      </div>
                      
                      {/* KHU VỰC NÚT HÀNH ĐỘNG CỦA THÀNH VIÊN */}
                      <div className="flex items-center gap-2">
                        {/* NÚT ĐỔI BIỆT DANH (Cây bút) */}
                        <button 
                          onClick={() => openNicknameModal(member)}
                          className="w-7 h-7 rounded-full bg-gray-100 hover:bg-gray-200 text-gray-600 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity"
                          title="Đổi biệt danh"
                        >
                          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"></path></svg>
                        </button>

                        {/* Nút Toggle chuyển quyền (chỉ dành cho Owner trong Group) */}
                        {isGroup && isCurrentUserOwner && member.role.toUpperCase() !== 'OWNER' && (
                          <div 
                            onClick={() => setSelectedNewOwner(selectedNewOwner === member.userId ? null : member.userId)}
                            className={`w-5 h-5 rounded-full border-2 flex items-center justify-center cursor-pointer transition-colors ${
                              selectedNewOwner === member.userId 
                                ? 'border-[#0084ff] bg-[#0084ff]' 
                                : 'border-gray-300 hover:border-gray-400'
                            }`}
                            title={selectedNewOwner === member.userId ? 'Bỏ chọn' : 'Chọn làm trưởng nhóm'}
                          >
                            {selectedNewOwner === member.userId && (
                              <div className="w-2 h-2 bg-white rounded-full"></div>
                            )}
                          </div>
                        )}
                      </div>
                    </div>
                  );
                })}

                {/* Nút Submit chuyển quyền (Chỉ hiện khi có người được chọn) */}
                {isGroup && isCurrentUserOwner && selectedNewOwner && (
                  <div className="px-2 mt-3 animate-in fade-in zoom-in duration-200">
                    <button 
                      onClick={handleTransferConfirm}
                      className="w-full bg-[#0084ff] hover:bg-blue-600 text-white py-2 rounded-lg text-[14px] font-semibold transition"
                    >
                      Chuyển quyền Quản trị
                    </button>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      )}

      {/* ================= MODAL THÊM THÀNH VIÊN ================= */}
      {showAddModal && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-[2px] flex justify-center items-center z-50">
          <div className="bg-white rounded-2xl shadow-2xl w-[500px] max-h-[85vh] h-[550px] flex flex-col overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            <div className="p-4 border-b border-gray-100 flex items-center justify-between">
              <h3 className="font-bold text-[18px] text-gray-900">Thêm người</h3>
              <button onClick={closeAddModal} className="w-8 h-8 flex items-center justify-center rounded-full bg-gray-100 hover:bg-gray-200 text-gray-600 transition">✕</button>
            </div>

            <div className="p-4 border-b border-gray-100">
              <div className="flex items-center bg-[#f0f2f5] rounded-full px-4 py-2.5 text-[15px]">
                <span className="text-gray-500 mr-2 font-medium">Đến:</span>
                <input 
                  type="text" 
                  value={searchQuery}
                  onChange={handleSearchChange}
                  placeholder="Tìm kiếm theo tên hoặc email..."
                  className="bg-transparent border-none outline-none flex-1 text-gray-900 placeholder-gray-500"
                  autoFocus
                />
              </div>
            </div>

            <div className="flex-1 overflow-y-auto p-2 bg-white">
              {isSearching ? (
                <div className="flex justify-center items-center h-full">
                  <p className="text-sm text-gray-500 font-medium">Đang tải danh sách...</p>
                </div>
              ) : searchResults.length > 0 ? (
                <div className="space-y-1 p-1">
                  {searchResults.map(user => (
                    <div 
                      key={user.id} 
                      onClick={() => toggleUserSelection(user.id)}
                      className="flex items-center justify-between p-2.5 hover:bg-gray-50 rounded-xl cursor-pointer transition"
                    >
                      <div className="flex items-center gap-3">
                        <div className="w-11 h-11 bg-blue-50 text-blue-600 rounded-full flex justify-center items-center text-sm font-bold border border-blue-100">
                          {user.fullName.charAt(0).toUpperCase()}
                        </div>
                        <div className="flex flex-col">
                          <span className="font-semibold text-[15px] text-gray-900 leading-tight">{user.fullName}</span>
                          <span className="text-[13px] text-gray-500 mt-0.5">{user.email}</span>
                        </div>
                      </div>

                      <div className={`w-5 h-5 rounded-full border-2 flex items-center justify-center transition-colors ${
                        selectedUsersToAdd.includes(user.id) 
                          ? 'bg-[#0084ff] border-[#0084ff]' 
                          : 'border-gray-300'
                      }`}>
                        {selectedUsersToAdd.includes(user.id) && (
                          <svg className="w-3.5 h-3.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="3">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7"></path>
                          </svg>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="flex justify-center items-center h-full">
                  <p className="text-sm text-gray-500">Không có người dùng nào để thêm.</p>
                </div>
              )}
            </div>

            {selectedUsersToAdd.length > 0 && (
              <div className="p-4 border-t border-gray-100 bg-white">
                <button 
                  onClick={handleInviteConfirm}
                  className="w-full bg-[#0084ff] hover:bg-blue-600 text-white py-2.5 rounded-xl text-[15px] font-semibold transition shadow-sm"
                >
                  Thêm người ({selectedUsersToAdd.length})
                </button>
              </div>
            )}
          </div>
        </div>
      )}

      {/* ================= MODAL ĐỔI BIỆT DANH ================= */}
      {showNicknameModal && targetNicknameUser && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-[2px] flex justify-center items-center z-50">
          <div className="bg-white rounded-2xl shadow-2xl w-[400px] flex flex-col overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            
            <div className="p-4 border-b border-gray-100 flex items-center justify-between">
              <h3 className="font-bold text-[18px] text-gray-900">Chỉnh sửa biệt danh</h3>
              <button onClick={() => setShowNicknameModal(false)} className="w-8 h-8 flex items-center justify-center rounded-full bg-gray-100 hover:bg-gray-200 text-gray-600 transition">✕</button>
            </div>

            <div className="p-4 flex flex-col gap-4">
              <p className="text-sm text-gray-600">
                Mọi người trong đoạn chat sẽ thấy khi bạn thay đổi biệt danh của ai đó.
              </p>
              
              <div className="flex flex-col gap-1">
                <label className="text-xs font-semibold text-gray-500 uppercase">Biệt danh hiển thị</label>
                <input 
                  type="text" 
                  value={nicknameInput}
                  onChange={(e) => setNicknameInput(e.target.value)}
                  placeholder={targetNicknameUser.fullName}
                  className="w-full bg-[#f0f2f5] text-gray-900 rounded-xl px-4 py-3 text-[15px] focus:outline-none focus:ring-2 focus:ring-blue-100 border border-transparent focus:border-blue-500 transition"
                  autoFocus
                />
              </div>
            </div>

            <div className="p-4 border-t border-gray-100 bg-gray-50 flex justify-end gap-2">
              <button 
                onClick={handleRemoveNickname}
                className="px-4 py-2 rounded-lg text-[14px] font-semibold text-red-600 hover:bg-red-50 transition"
              >
                Gỡ
              </button>
              <button 
                onClick={() => setShowNicknameModal(false)}
                className="px-4 py-2 rounded-lg text-[14px] font-semibold text-gray-700 hover:bg-gray-200 transition"
              >
                Hủy
              </button>
              <button 
                onClick={handleSaveNickname}
                disabled={nicknameInput === (targetNicknameUser.nickname || targetNicknameUser.fullName)}
                className="px-5 py-2 rounded-lg text-[14px] font-semibold text-white bg-[#0084ff] hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition shadow-sm"
              >
                Lưu
              </button>
            </div>
            
          </div>
        </div>
      )}

      {/* ================= MODAL ĐỔI TÊN NHÓM ================= */}
      {showGroupTitleModal && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-[2px] flex justify-center items-center z-50">
          <div className="bg-white rounded-2xl shadow-2xl w-[400px] flex flex-col overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            
            <div className="p-4 border-b border-gray-100 flex items-center justify-between">
              <h3 className="font-bold text-[18px] text-gray-900">Đổi tên nhóm</h3>
              <button onClick={() => setShowGroupTitleModal(false)} className="w-8 h-8 flex items-center justify-center rounded-full bg-gray-100 hover:bg-gray-200 text-gray-600 transition">✕</button>
            </div>

            <div className="p-4 flex flex-col gap-4">
              <p className="text-sm text-gray-600">
                Mọi người trong nhóm đều có thể nhìn thấy tên này.
              </p>
              
              <div className="flex flex-col gap-1">
                <label className="text-xs font-semibold text-gray-500 uppercase">Tên nhóm</label>
                <input 
                  type="text" 
                  value={groupTitleInput}
                  onChange={(e) => setGroupTitleInput(e.target.value)}
                  placeholder="Nhập tên nhóm..."
                  className="w-full bg-[#f0f2f5] text-gray-900 rounded-xl px-4 py-3 text-[15px] focus:outline-none focus:ring-2 focus:ring-blue-100 border border-transparent focus:border-blue-500 transition"
                  autoFocus
                />
              </div>
            </div>

            <div className="p-4 border-t border-gray-100 bg-gray-50 flex justify-end gap-2">
              <button 
                onClick={() => setShowGroupTitleModal(false)}
                className="px-4 py-2 rounded-lg text-[14px] font-semibold text-gray-700 hover:bg-gray-200 transition"
              >
                Hủy
              </button>
              <button 
                onClick={handleSaveGroupTitle}
                disabled={!groupTitleInput.trim() || groupTitleInput.trim() === currentChat?.name}
                className="px-5 py-2 rounded-lg text-[14px] font-semibold text-white bg-[#0084ff] hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition shadow-sm"
              >
                Lưu
              </button>
            </div>
            
          </div>
        </div>
      )}

    </div>
  );
};
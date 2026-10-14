import React, { useState, useEffect } from 'react';
import axiosClient from '../../api/axiosClient';

export interface ConversationItem {
  id: number;
  name: string;
  lastMessage: string;
  time: string;
  avatar: string;
  unreadCount: number;
  type: 'PRIVATE' | 'GROUP' | string;
}

// Tái sử dụng DTO tìm kiếm User
export interface UserSearchDto {
  id: number;
  email: string;
  fullName: string;
}

interface ChatSidebarProps {
  activeNav: 'chats' | 'contacts' | 'settings';
  conversations: ConversationItem[];
  selectedChatId: number;
  onSelectChat: (id: number) => void;
  activeContactTab: 'list' | 'requests' | 'search';
  setActiveContactTab: (tab: 'list' | 'requests' | 'search') => void;
  userEmail?: string;
  currentUserId?: number; // <--- BỔ SUNG ĐỂ LỌC USER
  onLeaveGroup?: (id: number) => void;
  onCreateGroup?: (title: string, memberIds: number[]) => Promise<void>; // <--- HÀM TẠO NHÓM
}

export const ChatSidebar: React.FC<ChatSidebarProps> = ({
  activeNav,
  conversations,
  selectedChatId,
  onSelectChat,
  activeContactTab,
  setActiveContactTab,
  userEmail,
  currentUserId,
  onLeaveGroup,
  onCreateGroup
}) => {
  const [openMenuId, setOpenMenuId] = useState<number | null>(null);

  // --- STATE CHO MODAL TẠO NHÓM ---
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [groupName, setGroupName] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<UserSearchDto[]>([]);
  const [selectedUsers, setSelectedUsers] = useState<number[]>([]);
  const [isSearching, setIsSearching] = useState(false);

  const handleToggleMenu = (e: React.MouseEvent, id: number) => {
    e.stopPropagation(); 
    setOpenMenuId(openMenuId === id ? null : id);
  };

  const handleLeaveGroup = (e: React.MouseEvent, id: number) => {
    e.stopPropagation();
    if (window.confirm("Bạn có chắc chắn muốn rời nhóm này?")) {
      onLeaveGroup?.(id);
      setOpenMenuId(null);
    }
  };

  // --- EFFECT TÌM KIẾM NGƯỜI DÙNG KHI MỞ MODAL TẠO NHÓM ---
  useEffect(() => {
    if (!showCreateModal) return;

    const fetchUsers = async () => {
      setIsSearching(true);
      try {
        const res = await axiosClient.get<UserSearchDto[]>(`/Users/search?keyword=${encodeURIComponent(searchQuery)}`);
        // Lọc bỏ chính mình khỏi danh sách
        const filteredUsers = res.data.filter((u: UserSearchDto) => u.id !== currentUserId);
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
  }, [searchQuery, showCreateModal, currentUserId]);

  const toggleUserSelection = (userId: number) => {
    if (selectedUsers.includes(userId)) {
      setSelectedUsers(selectedUsers.filter(id => id !== userId));
    } else {
      setSelectedUsers([...selectedUsers, userId]);
    }
  };

  const closeCreateModal = () => {
    setShowCreateModal(false);
    setGroupName('');
    setSearchQuery('');
    setSelectedUsers([]);
  };

  const handleConfirmCreate = async () => {
    if (selectedUsers.length > 0) {
      await onCreateGroup?.(groupName, selectedUsers);
      closeCreateModal();
    }
  };

  return (
    <div className="w-72 bg-gray-50/50 border-r border-gray-200 flex flex-col relative" onClick={() => setOpenMenuId(null)}>
      {activeNav === 'chats' && (
        <>
          {/* CẬP NHẬT HEADER CÓ NÚT TẠO NHÓM (+) */}
          <div className="p-4 border-b border-gray-200/60 flex items-center gap-2">
            <input
              type="text"
              placeholder="Search chats..."
              className="flex-1 bg-white border border-gray-200 text-gray-700 rounded-xl px-3 py-2 text-sm focus:outline-none focus:border-blue-500 transition shadow-sm min-w-0"
            />
            <button 
              onClick={() => setShowCreateModal(true)}
              className="w-9 h-9 flex-shrink-0 flex items-center justify-center bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-xl transition"
              title="Tạo nhóm chat mới"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4"></path>
              </svg>
            </button>
          </div>
          
          <div className="flex-1 overflow-y-auto p-2 space-y-1">
            {conversations.map((chat) => {
              const isActive = selectedChatId === chat.id;
              const isGroupChat = chat.type?.toUpperCase() === 'GROUP'; 

              return (
                <div
                  key={chat.id}
                  onClick={() => onSelectChat(chat.id)}
                  className={`relative flex items-center gap-3 p-3 rounded-xl cursor-pointer transition ${
                    isActive ? 'bg-blue-50/90 text-gray-900 shadow-sm' : 'hover:bg-gray-100/80 text-gray-700'
                  }`}
                >
                  <div className="w-10 h-10 bg-gray-200 text-gray-700 font-bold rounded-full flex items-center justify-center text-xs flex-shrink-0 relative">
                    {chat.avatar}
                    {chat.unreadCount > 0 && (
                      <span className="absolute -top-1 -right-1 bg-rose-500 text-white text-[9px] font-bold w-4 h-4 rounded-full flex items-center justify-center">
                        {chat.unreadCount}
                      </span>
                    )}
                  </div>

                  <div className="flex-1 min-w-0 pr-4">
                    <div className="flex justify-between items-baseline mb-0.5">
                      <h4 className={`font-bold text-xs truncate ${chat.unreadCount > 0 ? 'text-gray-900 font-extrabold' : 'text-gray-800'}`}>
                        {chat.name}
                      </h4>
                      <span className="text-[10px] text-gray-400 font-normal">{chat.time}</span>
                    </div>
                    <p className={`text-xs truncate ${chat.unreadCount > 0 ? 'text-blue-600 font-semibold' : 'text-gray-500'}`}>
                      {chat.lastMessage}
                    </p>
                  </div>

                  {/* 3-Dot Menu */}
                  {isGroupChat && (
                    <button 
                      onClick={(e) => handleToggleMenu(e, chat.id)}
                      className="absolute right-2 top-1/2 -translate-y-1/2 p-1.5 text-gray-400 hover:text-gray-600 rounded-full hover:bg-gray-200"
                    >
                      <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24"><path d="M12 8c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm0 2c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm0 6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z" /></svg>
                    </button>
                  )}

                  {openMenuId === chat.id && (
                    <div className="absolute right-8 top-8 w-32 bg-white border border-gray-200 shadow-lg rounded-lg py-1 z-50">
                      <button 
                        onClick={(e) => handleLeaveGroup(e, chat.id)}
                        className="w-full text-left px-4 py-2 text-xs text-rose-600 hover:bg-rose-50 font-medium"
                      >
                        🚪 Rời nhóm
                      </button>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </>
      )}

      {/* CÁC TAB CONTACT VÀ SETTINGS GIỮ NGUYÊN */}
      {activeNav === 'contacts' && (
        <>
          <div className="p-4 border-b border-gray-200/60">
            <h1 className="text-lg font-bold text-gray-800">Contacts</h1>
            <p className="text-xs text-gray-500 mt-0.5">Quản lý mạng lưới bạn bè</p>
          </div>
          <div className="p-3 space-y-1.5">
            <button onClick={() => setActiveContactTab('list')} className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-xs font-semibold transition ${activeContactTab === 'list' ? 'bg-blue-600 text-white shadow-sm' : 'text-gray-700 hover:bg-gray-100'}`}>👥 Danh sách bạn bè</button>
            <button onClick={() => setActiveContactTab('requests')} className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-xs font-semibold transition ${activeContactTab === 'requests' ? 'bg-blue-600 text-white shadow-sm' : 'text-gray-700 hover:bg-gray-100'}`}>🔔 Lời mời kết bạn</button>
            <button onClick={() => setActiveContactTab('search')} className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-xs font-semibold transition ${activeContactTab === 'search' ? 'bg-blue-600 text-white shadow-sm' : 'text-gray-700 hover:bg-gray-100'}`}>🔍 Tìm kiếm người dùng</button>
          </div>
        </>
      )}

      {activeNav === 'settings' && (
        <div className="p-4">
          <h1 className="text-lg font-bold text-gray-800 mb-3">Settings</h1>
          <div className="bg-white p-3 rounded-xl border border-gray-200 text-xs">
            <p className="text-gray-500">Tài khoản hiện tại:</p>
            <p className="font-semibold text-gray-800 mt-1 truncate">{userEmail}</p>
          </div>
        </div>
      )}

      {/* ================= MODAL TẠO NHÓM MỚI ================= */}
      {showCreateModal && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-[2px] flex justify-center items-center z-50">
          <div className="bg-white rounded-2xl shadow-2xl w-[500px] max-h-[85vh] h-[600px] flex flex-col overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            
            <div className="p-4 border-b border-gray-100 flex items-center justify-between">
              <h3 className="font-bold text-[18px] text-gray-900">Tạo nhóm mới</h3>
              <button onClick={closeCreateModal} className="w-8 h-8 flex items-center justify-center rounded-full bg-gray-100 hover:bg-gray-200 text-gray-600 transition">✕</button>
            </div>

            {/* Ô NHẬP TÊN NHÓM & THANH TÌM KIẾM */}
            <div className="p-4 border-b border-gray-100 flex flex-col gap-3 bg-gray-50/50">
              <div className="flex items-center bg-white border border-gray-200 rounded-xl px-4 py-2 text-[15px] shadow-sm">
                <span className="text-gray-500 mr-2 font-medium">Tên nhóm:</span>
                <input 
                  type="text" 
                  value={groupName}
                  onChange={(e) => setGroupName(e.target.value)}
                  placeholder="Nhóm chat"
                  className="bg-transparent border-none outline-none flex-1 text-gray-900 placeholder-gray-400"
                  autoFocus
                />
              </div>

              <div className="flex items-center bg-white border border-gray-200 rounded-xl px-4 py-2 text-[15px] shadow-sm">
                <span className="text-gray-500 mr-2 font-medium">Thêm:</span>
                <input 
                  type="text" 
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="Tìm kiếm bạn bè..."
                  className="bg-transparent border-none outline-none flex-1 text-gray-900 placeholder-gray-400"
                />
              </div>
            </div>

            {/* DANH SÁCH NGƯỜI DÙNG */}
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
                        selectedUsers.includes(user.id) 
                          ? 'bg-[#0084ff] border-[#0084ff]' 
                          : 'border-gray-300'
                      }`}>
                        {selectedUsers.includes(user.id) && (
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
                  <p className="text-sm text-gray-500">Không tìm thấy người dùng nào.</p>
                </div>
              )}
            </div>

            {/* FOOTER XÁC NHẬN */}
            <div className="p-4 border-t border-gray-100 bg-white">
              <button 
                onClick={handleConfirmCreate}
                disabled={selectedUsers.length === 0}
                className="w-full bg-[#0084ff] hover:bg-blue-600 disabled:bg-blue-300 text-white py-2.5 rounded-xl text-[15px] font-semibold transition shadow-sm"
              >
                Tạo nhóm {selectedUsers.length > 0 ? `(${selectedUsers.length})` : ''}
              </button>
            </div>

          </div>
        </div>
      )}

    </div>
  );
};
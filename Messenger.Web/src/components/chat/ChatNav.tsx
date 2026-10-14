import React from 'react';

interface ChatNavProps {
  activeNav: 'chats' | 'contacts' | 'settings';
  setActiveNav: (nav: 'chats' | 'contacts' | 'settings') => void;
  userEmail?: string;
  onLogout: () => void;
}

export const ChatNav: React.FC<ChatNavProps> = ({ activeNav, setActiveNav, userEmail, onLogout }) => {
  return (
    <div className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6 z-10 shadow-sm flex-shrink-0">
      {/* Logo và các Tab điều hướng ngang */}
      <div className="flex items-center gap-8">
        <div className="flex items-center gap-2.5 font-bold text-blue-600 text-base">
          <span className="w-8 h-8 bg-blue-600 text-white rounded-xl flex items-center justify-center text-sm shadow-sm">M</span>
          Messenger
        </div>

        <div className="flex items-center gap-1 bg-gray-50 p-1 rounded-xl border border-gray-200/60">
          <button
            onClick={() => setActiveNav('chats')}
            className={`flex items-center gap-2 px-4 py-1.5 rounded-lg font-medium text-xs transition-all ${
              activeNav === 'chats' ? 'bg-white text-blue-600 shadow-sm font-semibold' : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            💬 Chats
          </button>

          <button
            onClick={() => setActiveNav('contacts')}
            className={`flex items-center gap-2 px-4 py-1.5 rounded-lg font-medium text-xs transition-all ${
              activeNav === 'contacts' ? 'bg-white text-blue-600 shadow-sm font-semibold' : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            👥 Contacts
          </button>

          <button
            onClick={() => setActiveNav('settings')}
            className={`flex items-center gap-2 px-4 py-1.5 rounded-lg font-medium text-xs transition-all ${
              activeNav === 'settings' ? 'bg-white text-blue-600 shadow-sm font-semibold' : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            ⚙️ Settings
          </button>
        </div>
      </div>

      {/* Trạng thái Server & Thông tin User / Đăng xuất ở góc phải */}
      <div className="flex items-center gap-4">
        <div className="flex items-center gap-2 text-xs font-semibold text-gray-600 bg-gray-50 px-3 py-1.5 rounded-full border border-gray-200/60">
          <span className="w-2.5 h-2.5 rounded-full bg-emerald-500 animate-pulse"></span>
          Server Connected
        </div>

        <button
          onClick={onLogout}
          className="flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-medium text-rose-600 hover:bg-rose-50 transition border border-rose-100"
        >
          🚪 Đăng xuất ({userEmail?.split('@')[0]})
        </button>
      </div>
    </div>
  );
};
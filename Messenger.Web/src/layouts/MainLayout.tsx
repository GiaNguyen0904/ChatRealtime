import React from 'react';

interface MainLayoutProps {
  children: React.ReactNode;
  topNav: React.ReactNode;       // Thanh điều hướng ngang ở trên
  subSidebar: React.ReactNode;   // Cột bên trái phía dưới (Danh sách chat / Menu contacts)
}

export const MainLayout: React.FC<MainLayoutProps> = ({ children, topNav, subSidebar }) => {
  return (
    <div className="flex flex-col h-screen bg-gray-100 text-gray-800 font-sans antialiased overflow-hidden select-none">
      {/* 1. THANH NGANG PHÍA TRÊN */}
      {topNav}

      {/* 2. KHUNG PHÍA DƯỚI CHIA LÀM 2 CỘT */}
      <div className="flex flex-1 overflow-hidden">
        {/* Cột 1 phía dưới: Sub-sidebar (Danh sách Chats / Menu Contacts) */}
        {subSidebar}

        {/* Cột 2 phía dưới: Main Content Area (Khung Chat hoặc Contacts) */}
        <div className="flex-1 bg-white flex flex-col overflow-hidden shadow-inner">
          {children}
        </div>
      </div>
    </div>
  );
};
# REALTIME MESSAGING SYSTEM — ROADMAP & DEVELOPMENT PLAN

## 1. Mục tiêu dự án

Xây dựng hệ thống nhắn tin realtime chuẩn chỉnh phục vụ ứng tuyển vị trí Backend Developer Intern.

- **Core Backend:** Chat cá nhân, chat nhóm, quản lý bạn bè và truyền tải tin nhắn thời gian thực (Realtime Messaging).
- **Frontend (Messenger.Web):** Đóng vai trò là Client phục vụ sử dụng, kiểm thử API và demo toàn bộ luồng Backend; không đầu tư sâu vào UI/UX cầu kỳ hay thiết kế Figma.

## 2. Tech Stack

| Hạng mục | Lựa chọn | Ghi chú / Chi tiết |
|---|---|---|
| Backend | ASP.NET Core Web API (.NET) | Core framework |
| BE Architecture | Modular Monolith | Học và áp dụng Clean Architecture nâng cao dần |
| Realtime | SignalR | Truyền tải tin nhắn thời gian thực |
| Database | PostgreSQL | CSDL chính, tối ưu Index & Constraints |
| Cache | Redis | Cấu hình sau MVP khi có use case phù hợp |
| Message Queue | RabbitMQ | Tích hợp sau MVP |
| Frontend | React 18 + TypeScript + Vite | Khai báo kiểu dữ liệu chặt chẽ |
| FE State Management | Zustand | Quản lý Global State tinh gọn, thay thế Redux |
| FE HTTP Client | Axios Client | Cấu hình Interceptor tự động xử lý Token |
| API Testing | Postman + Swagger | Kiểm thử và tài liệu hóa API |
| Container | Docker / Docker Compose | Đóng gói môi trường |
| Version Control | Git / GitHub | Quản lý mã nguồn |

## 3. Nguyên tắc phát triển

- **Backend là trọng tâm:** FE làm ở mức vừa đủ để test/demo end-to-end.
- **Phát triển song song theo Feature:** BE và FE đi cùng nhau từng tính năng, không làm toàn bộ BE rồi mới làm FE.
- **Quy trình chuẩn từng Feature:** Database → Backend → Frontend → Integration → Test.
- **Pattern FE đồng nhất:** API Service → Zustand Store → Minimal UI Component.
- **Tối giản UI:** Không cần Figma/pixel-perfect UI; sử dụng các thành phần HTML/CSS cơ bản.

## 4. Phase 0 — Analysis & Design (~1 tuần)

**Trạng thái:** Hoàn thiện sơ đồ Use Case, ERD và phân tích CSDL.

### Hạng mục đã thực hiện

- **User Flow:** Phân tích chi tiết luồng User và Admin.
- **User Stories:** Phân chia cụ thể tính năng MVP, Nice-to-have và Future Enhancement.
- **Use Case Diagram & System Flow Diagram.**
- **ERD & Database Analysis:** Định nghĩa bảng, PK/FK, Partial Unique Index, CHECK Constraints và Data Rules.
- **FE Screen Description:** Mô tả các màn hình cần thiết.

**Definition of Done:** Đủ tài liệu tối thiểu để bắt đầu code.

## 5. Timeline Tổng Quan đến MVP

| Phase | Thời lượng | Backend Scope | Frontend Scope (Zustand Pattern) | Kết quả đầu ra (Deliverable) |
|---|---|---|---|---|
| 0 | ~2 tuần | Analysis / ERD / DB Analysis | FE Screen Description & Design Ready | Flow |
| 1 | ~2 tuần | Foundation, Web API, EF Core, PostgreSQL | Vite + React + TS, Axios Client, Layout Foundation | Foundation |
| 2 | ~2 tuần | Register / Login / JWT / Refresh Token | useAuthStore, Auth UI, Protected Route | Auth End-to-End |
| 3 | ~2 tuần | User Profile / Search / Friend Service | useFriendStore, Friend List UI, Search Modal | Friend End-to-End |
| 4 | ~2 tuần | Conversation / Group / History API | useChatStore, Chat Window, Group UI | Chat via API |
| 5 | ~2 tuần | SignalR Hub / Connection / Realtime | SignalR Client Listener, Realtime Push | Realtime MVP |
| 6 | ~2 tuần | Validation / Logging / Docker / Optimize | Error / Loading States / Polish | MVP Complete |

## 6. Chi Tiết Lộ Trình Triển Khai

### Phase 1 — Foundation

#### Backend

- Dựng khung dự án ASP.NET Core Web API (.NET 8) theo mô hình Modular Monolith.
- Cấu hình EF Core, PostgreSQL Migration, Dependency Injection (DI), Middleware xử lý lỗi tập trung (Global Exception Handling), Logging và Swagger UI.

#### Frontend

- Cấu hình React 18 + TypeScript + Vite.
- Thiết lập `axiosClient.ts`: Cấu hình baseURL, timeout, headers.
- Dựng cấu hình Router cơ bản với React Router (`/login`, `/register`, `/chat`).
- Khai báo bộ UI cơ bản (`Button`, `InputGroup`, `AuthLayout`).

**Milestone:** FE gọi thành công API Test/Health-check từ BE và BE kết nối ổn định PostgreSQL.

### Phase 2 — Authentication

#### Backend

- Thực hiện các API: Register, Login, Password Hashing (BCrypt), tạo JWT Access Token và Refresh Token.
- Xử lý lưu trữ Refresh Token mã hóa vào CSDL, có kiểm tra thời gian hết hạn (`ExpiresAt`) và trạng thái thu hồi (`RevokedAt`).

#### Frontend

- Viết `authService.ts` gọi các endpoint Authentication.
- Xây dựng Zustand store `useAuthStore.ts` quản lý state `user`, `accessToken`, `isAuthenticated`.
- Cấu hình Axios Interceptors: Tự động đính kèm Bearer Token vào Header và bắt lỗi 401 để tự động gọi API Refresh Token.
- Xây dựng `ProtectedRoute.tsx` để chặn truy cập khi chưa đăng nhập.

**Milestone:** Hoàn thiện luồng Register → Login → Nhận JWT → Chuyển hướng Trang Chat → Logout.

### Phase 3 — User & Friend

#### Backend

- Thực hiện các API:
  - User Profile
  - Tìm kiếm người dùng (`SearchUsersAsync`)
  - Gửi lời mời kết bạn (`SendRequestAsync`)
  - Chấp nhận/Từ chối lời mời (`RespondRequestAsync`)
  - Xóa bạn (`DeleteFriendAsync`)
  - Lấy danh sách bạn bè (`GetFriendsAsync`)
- Áp dụng logic `minId/maxId` khớp với ràng buộc `CHECK(user_id < friend_id)` trên PostgreSQL.

#### Frontend

- Viết `friendService.ts` và Zustand store `useFriendStore.ts`.
- Dựng UI Component:
  - Ô tìm kiếm User
  - Modal hiển thị danh sách lời mời chờ xử lý
  - Danh sách bạn bè ở Sidebar

**Milestone:** Tìm kiếm người dùng → Gửi lời mời → Tài khoản bên kia nhận & Accept → Cập nhật danh sách bạn bè end-to-end.

### Phase 4 — Conversation & Group

#### Backend

- Thực hiện các API:
  - Tạo cuộc trò chuyện cá nhân (`PRIVATE`) và nhóm (`GROUP`)
  - Lấy danh sách hội thoại
  - Load lịch sử tin nhắn (có phân trang)
  - Tạo nhóm chat
  - Thêm/Xóa thành viên
  - Rời nhóm
  - Chuyển quyền Trưởng nhóm (Transfer Ownership)
- Áp dụng logic `direct_key` để chống trùng lặp cuộc trò chuyện 1-1.

#### Frontend

- Viết `chatService.ts` và Zustand store `useChatStore.ts`.
- Dựng UI Component:
  - Danh sách cuộc trò chuyện Sidebar
  - Khung chat hiển thị tin nhắn (Chat Window)
  - Modal tạo nhóm chat

**Milestone:** Chọn hội thoại → Load lịch sử tin nhắn từ CSDL thành công → Gửi tin nhắn dạng REST API.

### Phase 5 — Realtime Messaging

#### Backend

- Dựng SignalR Hub, quản lý vòng đời kết nối (Connection Lifecycle).
- Thực hiện các sự kiện Realtime:
  - Tham gia/Rời nhóm chat
  - Gửi/Nhận tin nhắn thời gian thực
  - Phát tin nhắn đến tất cả thành viên trong nhóm

#### Frontend

- Viết `signalRService.ts` khởi tạo và quản lý `HubConnection` (Tự động kết nối/kết nối lại).
- Đăng ký các Listener lắng nghe sự kiện từ SignalR Hub và đẩy thẳng dữ liệu mới vào `useChatStore` để cập nhật giao diện mà không cần tải lại trang.

**Milestone:** Mở 2 trình duyệt riêng biệt, chat qua lại nhận tin nhắn tức thì (Realtime MVP).

### Phase 6 — MVP Polish & Optimization

#### Backend

- Kiểm tra Validation, xử lý Error Handling toàn diện, bổ sung Seed Data mẫu.
- Tối ưu hóa truy vấn CSDL, chỉ định Index chuẩn.
- Đóng gói dự án bằng Docker / Docker Compose, hoàn thiện tài liệu Swagger và file `README.md`.

#### Frontend

- Bổ sung hiển thị trạng thái Loading (Spinner), Toast Notification báo lỗi/thành công.
- Xử lý các trường hợp biên (Edge Cases):
  - Hiển thị thông báo khi mất kết nối SignalR
  - Tự động đẩy về trang Login khi Refresh Token hết hạn

**Milestone:** Sẵn sàng Demo hoàn chỉnh toàn bộ hệ thống cả Backend và Frontend.

## 7. Sau MVP (Future Enhancements)

- **Caching & Message Queue:** Áp dụng Redis Cache cho danh sách trạng thái Online/Offline và tích hợp RabbitMQ xử lý tác vụ bất đồng bộ.
- **Refactoring:** Áp dụng Clean Architecture sâu hơn, viết thêm Unit Test & Integration Test.
- **Tính năng bổ sung (Nice-to-have):**
  - Chặn người dùng (Block User), Báo cáo (Report User).
  - Thu hồi tin nhắn (Recall Message), Xóa tin nhắn ở phía tôi (Soft Delete).
  - Hiển thị trạng thái Đã xem (Seen Status), Đang nhập (Typing Indicator).
- **Deployment:** Triển khai hoàn chỉnh hệ thống lên môi trường Server Cloud qua Docker Compose.

## 8. Scope Chi Tiết Dự Án MVP

### Nằm trong MVP (Bắt buộc hoàn thành)

1. Đăng ký / Đăng nhập / Đăng xuất / Refresh Access Token.
2. Authentication & Authorization dựa trên JWT.
3. Tìm kiếm người dùng theo tên hoặc Email.
4. Gửi lời mời kết bạn / Chấp nhận / Từ chối / Xóa bạn bè.
5. Quản lý hội thoại cá nhân (Private Chat) & Nhóm chat (Group Chat).
6. Gửi tin nhắn Text, Emoji, Hình ảnh cơ bản.
7. Xem lịch sử tin nhắn cuộc trò chuyện.
8. Truyền tải tin nhắn thời gian thực qua SignalR.
9. Tài khoản Admin: Đăng nhập, xem thống kê cơ bản, vô hiệu hóa tài khoản User.

### Ngoài MVP (Future Enhancements)

1. Chặn người dùng (Block User) / Báo cáo tài khoản (Report User).
2. Thu hồi tin nhắn (Recall Message).
3. Thông báo đẩy nâng cao (Push Notification).
4. Gọi thoại / Gọi Video (Voice & Video Call).

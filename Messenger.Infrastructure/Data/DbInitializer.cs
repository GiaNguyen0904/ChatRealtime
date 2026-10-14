using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Apply các Migrations còn thiếu nếu DB chưa cập nhật đầy đủ
            if ((await context.Database.GetPendingMigrationsAsync()).Any())
            {
                await context.Database.MigrateAsync();
            }

            // Seed dữ liệu mẫu nếu cần thiết
            if (!await context.Roles.AnyAsync())
            {
                var roles = new List<Role>
                {
                    new Role
                {
                    Name = "ADMIN",
                    Description = "Quản trị viên hệ thống",
                    CreatedAt = DateTime.UtcNow
                },
                new Role
                {
                    Name = "USER",
                    Description = "Người dùng tiêu chuẩn",
                    CreatedAt = DateTime.UtcNow
                }
                };
                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }

            string adminEmail = "admin@chatrealtime.com";
            // Kiểm tra xem người dùng admin đã tồn tại chưa
            if (!await context.Users.AnyAsync(u => u.Email == adminEmail))
            {
                // Tạo người dùng admin mặc định
                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "ADMIN");
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword("Admin@123"); // Mật khẩu mặc định cho admin

                // Tạo người dùng admin
                var adminUser = new User
                {
                    Email = adminEmail,
                    PasswordHash = hashedPassword,
                    RoleId = adminRole!.Id,
                    FullName = "Admin User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await context.Users.AddAsync(adminUser); // Thêm người dùng admin vào DbContext
                await context.SaveChangesAsync(); // Lưu thay đổi vào cơ sở dữ liệu
            }
        }
            
    }
}

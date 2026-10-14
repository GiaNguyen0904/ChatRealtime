using Messenger.Application.Auth.Interfaces;
using Messenger.Infrastructure.Data;
using Messenger.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Messenger.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Messenger.Infrastructure
{
    public static class DependencyInjection
    {
        //method này dùng để thêm các dịch vụ của tầng Infrastructure vào DI container của ứng dụng. 
        public static IServiceCollection AddInfrastructure( this IServiceCollection services, IConfiguration configuration)
        {
            // Đăng ký ApplicationDbContext với DI container, sử dụng PostgreSQL làm cơ sở dữ liệu
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection")));
            // Đăng ký IApplicationDbContext để sử dụng ApplicationDbContext
            services.Configure<JwtSettings>(
            configuration.GetSection("JwtSettings")); // Đăng ký JwtSettings để sử dụng trong ứng dụng
            services.AddScoped<IApplicationDbContext, ApplicationDbContext>(); // Đăng ký IApplicationDbContext để sử dụng ApplicationDbContext
            services.AddScoped<IPasswordHasher, PasswordHasher>(); // Đăng ký IPasswordHasher để sử dụng PasswordHasher
            services.AddScoped<ITokenService, TokenService>(); // Đăng ký ITokenService để sử dụng TokenService



            return services;

        }
    }
}

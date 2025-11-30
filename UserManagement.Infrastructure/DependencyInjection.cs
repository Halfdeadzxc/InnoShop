using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Application.Interfaces;
using UserManagement.Infrastructure.Data;
using UserManagement.Infrastructure.Data.Repositories;
using UserManagement.Infrastructure.Services;
using UserManagement.Infrastructure.Security;
using UserManagement.Infrastructure.ExternalServices;

namespace UserManagement.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<UserDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(UserDbContext).Assembly.FullName)));

            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<ITokenService, JwtService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IProductCommunicationService, ProductCommunicationService>();

            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

            services.AddHttpClient<IProductCommunicationService, ProductCommunicationService>(client =>
            {
                client.BaseAddress = new Uri(configuration["ProductService:BaseUrl"] ?? "http://product-service:5001/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return services;
        }
    }
}
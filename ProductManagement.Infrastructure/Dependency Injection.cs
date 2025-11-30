using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Interfaces;
using ProductManagement.Infrastructure.Data;
using ProductManagement.Infrastructure.Data.Repositories;
using ProductManagement.Infrastructure.Services;

namespace ProductManagement.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Database
            services.AddDbContext<ProductDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ProductDbContext).Assembly.FullName)));

            // Repositories
            services.AddScoped<IProductRepository, ProductRepository>();

            // External Services
            services.AddScoped<IUserCommunicationService, UserCommunicationService>();

            // Configuration
            services.Configure<UserServiceSettings>(configuration.GetSection("UserService"));

            // HTTP Client for User Service Communication
            services.AddHttpClient<IUserCommunicationService, UserCommunicationService>((serviceProvider, client) =>
            {
                var settings = serviceProvider.GetRequiredService<IOptions<UserServiceSettings>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            });

            return services;
        }

    }
}
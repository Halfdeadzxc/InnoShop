using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Application.DTOs
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }

        public AuthResponse(string accessToken, string refreshToken, UserDto user)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            User = user;
            ExpiresAt = DateTime.UtcNow.AddHours(24);
        }
    }

}

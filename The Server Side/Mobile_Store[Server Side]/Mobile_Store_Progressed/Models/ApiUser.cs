namespace Mobile_Store_Progressed.Models
{
    public class ApiUser
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; }

        public string Role { get; set; }

        public string? RefreshTokenHash { get; set; }

        public DateTime? RefreshTokenExpiresAt { get; set; }

        public DateTime? RefreshTokenRevokedAt { get; set; }

        public DateTime? CreatedAt { get; set; }



    }
}

namespace SecureCrudAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } // 'Admin' or 'User'
        public string Status { get; set; } = "Pending"; // Default to 'Pending'
        public string VerificationToken { get; set; } // Auto-generated
        public DateTime? TokenExpiry { get; set; }
        public string? ResetToken { get; set; } // Optional
        public DateTime? ResetTokenExpiry { get; set; }

        //public string RefreshToken { get; set; }
    }

}

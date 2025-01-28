using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using SecureCrudAPI.Models;
using System.IdentityModel.Tokens.Jwt;
 
using System.Security.Claims;
using System.Text;

namespace SecureCrudAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(UserDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }



        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto userDto)
        {
            // Create a new User object
            var user = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                VerificationToken = Guid.NewGuid().ToString(),
                Role = userDto.Role,
                Status = "Pending" // Set status as pending initially
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send the verification email (Implement SendVerificationEmail() method)
            await SendVerificationEmail(user);

            return Ok(new { Message = "User registered successfully! Please check your email to verify your account." });
        }



        //[HttpPost("register")]
        //public async Task<IActionResult> Register([FromBody] User user)
        //{
        //    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        //    _context.Users.Add(user);
        //    await _context.SaveChangesAsync();
        //    return Ok(new { Message = "User registered successfully!" });
        //}

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel user)
        {
            var dbUser = await _context.Users.SingleOrDefaultAsync(u => u.Username == user.Username);
            if (dbUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, dbUser.PasswordHash))
            {
                return Unauthorized(new { Message = "Invalid username or password" });
            }

            var token = GenerateJwtToken(dbUser);
            return Ok(new { Token = token });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Name, user.Username)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        private async Task SendVerificationEmail(User user)
        {
            var verificationLink = $"{_configuration["AppSettings:BaseUrl"]}/api/user/verify?token={user.VerificationToken}";
            var subject = "Verify Your Email";
            var body = $"Please click the link below to verify your email address:\n{verificationLink}";

            // Send the email using your email service (like SendGrid or SMTP)
            await SendEmailAsync(user.Email, subject, body);
        }



        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            // Create the email message
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Admin", emailSettings["SenderEmail"]));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body // Use HTML body for better formatting
            };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            // Send the email
            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true; // Ignore SSL validation

                await client.ConnectAsync(emailSettings["SMTPServer"], int.Parse(emailSettings["SMTPPort"]), SecureSocketOptions.Auto);
                client.CheckCertificateRevocation = false; // Skip revocation check
                await client.AuthenticateAsync(emailSettings["SenderEmail"], emailSettings["SenderPassword"]);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }

        }




        [HttpGet("verify")]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.VerificationToken == token);
            if (user == null || user.TokenExpiry < DateTime.Now)
            {
                return BadRequest(new { Message = "Invalid or expired token" });
            }

            // Set user status to Active
            user.Status = "Active";
            user.VerificationToken = "VERIFIED"; // Clear the token with a placeholder value

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Email verified successfully!" });
        }



        //[HttpGet("verify")]
        //public async Task<IActionResult> VerifyEmail(string token)
        //{
        //    var user = await _context.Users.SingleOrDefaultAsync(u => u.VerificationToken == token);
        //    if (user == null || user.TokenExpiry < DateTime.Now)
        //    {
        //        return BadRequest(new { Message = "Invalid or expired token" });
        //    }

        //    // Set user status to Active
        //    user.Status = "Active";
        //    user.VerificationToken = null; // Clear the token after verification

        //    await _context.SaveChangesAsync();

        //    return Ok(new { Message = "Email verified successfully!" });
        //}


        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.ResetToken == model.Token && u.ResetTokenExpiry > DateTime.Now);
            if (user == null)
            {
                return BadRequest(new { Message = "Invalid or expired reset token" });
            }

            // Hash the new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.ResetToken = null; // Clear the reset token after successful password reset
            user.ResetTokenExpiry = null; // Clear expiry

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Password reset successfully!" });
        }

        [HttpPost("refresh-token")]
        public IActionResult RefreshToken([FromBody] string refreshToken)
        {
            var user = _context.Users.SingleOrDefault(u => u.RefreshToken == refreshToken);
            if (user == null || user.RefreshTokenExpiry <= DateTime.Now)
            {
                return Unauthorized(new { Message = "Invalid or expired refresh token" });
            }

            var newJwtToken = GenerateJwtToken(user);
            return Ok(new { Token = newJwtToken });
        }


        //////////
        ///

        //[HttpPost("request-password-reset")]
        //public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetModel model)
        //{
        //    // Validate the input model
        //    if (model == null || string.IsNullOrEmpty(model.Email))
        //    {
        //        return BadRequest(new { Message = "Email is required" });
        //    }

        //    var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == model.Email);
        //    if (user == null)
        //    {
        //        return BadRequest(new { Message = "User not found" });
        //    }

        //    // Generate Reset Token
        //    user.ResetToken = Guid.NewGuid().ToString(); // Generate a unique token
        //    user.ResetTokenExpiry = DateTime.Now.AddHours(1); // Set token expiry time

        //    await _context.SaveChangesAsync();

        //    // Send the password reset email with the token
        //    var resetLink = $"{_configuration["AppSettings:BaseUrl"]}/api/user/reset-password?token={user.ResetToken}";
        //    var subject = "Reset Your Password";
        //    var body = $"Please click the link below to reset your password:\n{resetLink}";

        //    await SendEmailAsync(user.Email, subject, body);

        //    return Ok(new { Message = "Password reset link sent successfully!" });
        //}



        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetModel model)
        {
            // Validate the input model
            if (model == null || string.IsNullOrEmpty(model.Email))
            {
                return BadRequest(new { Message = "Email is required" });
            }

            var users = await _context.Users.Where(u => u.Email == model.Email).ToListAsync();

            if (!users.Any())
            {
                return BadRequest(new { Message = "User not found" });
            }

            if (users.Count > 1)
            {
                return BadRequest(new { Message = "Multiple users found with the same email. Please contact support." });
            }

            var user = users.First();

            // Generate Reset Token
            user.ResetToken = Guid.NewGuid().ToString(); // Generate a unique token
            user.ResetTokenExpiry = DateTime.Now.AddHours(1); // Set token expiry time

            await _context.SaveChangesAsync();

            // Send the password reset email with the token
            var resetLink = $"{_configuration["AppSettings:BaseUrl"]}/api/user/reset-password?token={user.ResetToken}";
            var subject = "Reset Your Password";
            var body = $"Please click the link below to reset your password:\n{resetLink}";

            await SendEmailAsync(user.Email, subject, body);

            return Ok(new { Message = "Password reset link sent successfully!" });
        }




    }
}

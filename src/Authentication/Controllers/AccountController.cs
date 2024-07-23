using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Authentication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController(ILogger<AccountController> logger, IConfiguration configuration) : ControllerBase
    {
        private static ConcurrentDictionary<string, string> Users = new ConcurrentDictionary<string, string>();

        //api/account/login/{email}/{password}
        [HttpGet("login/{email}/{password}")]
        public async Task<IActionResult> Login(string email, string password)
        {
            var dbEmail = Users.Keys.Where(x => x.Equals(email)).FirstOrDefault();

            if (!string.IsNullOrEmpty(dbEmail))
            {
                Users.TryGetValue(email, out string? dbPassword);

                if (!Equals(dbPassword, password))
                {
                    logger.Log(LogLevel.Warning, "Invalid credentials.");
                    return BadRequest("Invalid credentials.");
                }

                string jwtToken = GenerateJwtToken(dbEmail);
                return Ok(jwtToken);
            }

            return NotFound("Email not found.");
        }

        [HttpPost("register/{email}/{password}")]
        public async Task<IActionResult> Register(string email, string password)
        {
            var dbEmail = Users.Keys.Where(x => x.Equals(email)).FirstOrDefault();

            if (!string.IsNullOrEmpty(dbEmail)) 
            {
                return BadRequest("User already exist.");
            }

            Users[email]= password;
            return Ok("User create successfully.");
        }

        private string GenerateJwtToken(string dbEmail)
        {
            var key = Encoding.UTF8.GetBytes(configuration["Authentication:Key"]!);
            var securityKey = new SymmetricSecurityKey(key);
            var credential = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[] { new Claim(ClaimTypes.Email, dbEmail) };
            var token = new JwtSecurityToken(
                issuer: configuration["Authentication:Issuer"],
                audience: configuration["Authentication:Audience"],
                claims: claims,
                expires: null,
                signingCredentials: credential);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

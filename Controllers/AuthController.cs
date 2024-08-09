using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StudiaPraca.Contexts;
using StudiaPraca.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StudiaPraca.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] UserLogin login)
        {
            var user = Authenticate(login);

            if (user != null)
            {
                var token = Generate(user);
                return Ok(new { token });
            }

            return NotFound("User not found");
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] UserRegister register)
        {
            if (_context.Users.Any(u => u.Username == register.Username || u.Email == register.Email))
            {
                return BadRequest(new { message = "User with the same username or email already exists" });
            }

            var user = new User
            {
                Username = register.Username,
                Email = register.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(register.Password)
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { message = "User registered successfully" });
        }


        private User Authenticate(UserLogin login)
        {
            var user = _context.Users.SingleOrDefault(u => u.Username == login.Username);

            if (user != null && BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
            {
                return user;
            }

            return null;
        }
        private string Generate(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Use user ID instead of username
                new Claim(ClaimTypes.Name, user.Username)
              };

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
              _config["Jwt:Issuer"],
              claims,
              expires: DateTime.Now.AddMinutes(120),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

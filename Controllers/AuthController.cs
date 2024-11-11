// Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using contacts.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;


namespace contacts.Controllers
{

    [ApiController]
    [Route("api/[controller]")]

    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User loginUser)
        {
            // busquem usuari amb nom i password (password oberta!)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginUser.Username);

            // si usuari no existeix tornem Unathorized
            if (user == null)
                return Unauthorized("Invalid credentials");


            // Verificar la contrasenya codificada
            var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, loginUser.Password);
            if (passwordVerificationResult != PasswordVerificationResult.Success)
                return Unauthorized("Invalid credentials");

            /*
            Els "claims" són informació que s’inclou en el token JWT, com el nom d’usuari, l’ID i el rol d’administrador.
            Això permet que el front-end conegui l'identitat i els permisos de l’usuari.
            */
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("UserId", user.Id.ToString()),
                new Claim("IsAdmin", user.IsAdmin.ToString())
            };
            // clau a appsettings.json
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: creds);

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
    }

}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ProjectLibrary.Entities.Concrete;
using ProjectLibrary.Entities.Dtos;


namespace LibraryManagement.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            var user = new User
            {
                //PasswordHash = model.Password,
                
                UserName = model.Email.Split('@')[0],
                Email = model.Email,
                FullName = model.FullName,
                Role = "User"
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);
            //if (!result.Succeeded)
            //{
            //    foreach (var error in result.Errors)
            //    {
            //        Console.WriteLine($"Error: {error.Code} - {error.Description}");
            //    }
            //    return BadRequest(result.Errors);
            //}

            return Ok(new { message = "User registered successfully!" });
        }

        //[HttpPost("login")]
        //public async Task<IActionResult> Login([FromBody] LoginDto model)
        //{
        //    var user = await _userManager.FindByEmailAsync(model.Email);
        //    if (user == null)
        //        return Unauthorized("Invalid email or password");

        //    var result = await _signInManager.PasswordSignInAsync(user.Email, model.Password, false, false);
        //    if (!result.Succeeded)
        //        return Unauthorized("Invalid email or password");

        //    var token = GenerateJwtToken(user);
        //    return Ok(new { token });
        //}

        //[HttpPost("login")]
        //public async Task<IActionResult> Login([FromBody] LoginDto model)
        //{
        //    var user = await _userManager.FindByEmailAsync(model.Email);
        //    if (user == null)
        //        return Unauthorized("Invalid credentials");

        //    var isValidPassword = await _userManager.CheckPasswordAsync(user, model.Password);
        //    if (!isValidPassword)
        //        return Unauthorized("Invalid credentials");

        //    var token = GenerateJwtToken(user);
        //    return Ok(new { token });
        //}

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            var isValidPassword = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isValidPassword)
                return Unauthorized(new { message = "Invalid credentials" });

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Role
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
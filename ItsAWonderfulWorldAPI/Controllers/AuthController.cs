using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ItsAWonderfulWorldAPI.Services;
using ItsAWonderfulWorldAPI.Models;

namespace ItsAWonderfulWorldAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public ActionResult<User> Register([FromBody] RegisterModel model)
        {
            var user = _authService.Register(model.Username, model.Password);
            if (user == null)
            {
                return BadRequest("Username already exists");
            }
            return Ok(user);
        }

        [HttpPost("login")]
        public ActionResult<string> Login([FromBody] LoginModel model)
        {
            var token = _authService.Login(model.Username, model.Password);
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }
            return Ok(new { token });
        }
    }

    public class RegisterModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}

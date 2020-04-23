using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cw3.DTOs.Requests;
using Cw3.Models;
using Cw3.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Cw3.Controllers
{
    [Route("api/enrollments")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private IStudentDbService _service;
        private IConfiguration Configuration;

        public EnrollmentsController(IStudentDbService service, IConfiguration configuration)
        {
            _service = service;
            Configuration = configuration;
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public IActionResult EnrollStudent(EnrollStudentRequest request)
        {
            try {
            return Ok(_service.EnrollStudent(request));

            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("promotions")]
        [Authorize(Roles = "Employee")]
        public IActionResult PromoteStudent(PromoteStudentRequest request)
        {
            try
            {
                //Mamy zwrócić 201, ale metoda za to odpowiedzialna wymaga adresu url, nie wiem jaki adres podać więc daję adres strony głównej uczelni
                return Created("https://www.pja.edu.pl/",_service.PromoteStudents(request.Semester, request.Studies));
            }
            catch (ArgumentException ex) {
                return NotFound("Nie znaleziono danego wpisu");
            }
        }

        [HttpGet("login")]
        public IActionResult Login(LoginRequestDTO loginRequest)
        {
            if (!_service.CheckPassword(loginRequest))
                return Forbid("Bearer");

            var claims = _service.GetClaims(loginRequest.Login);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "Gakko",
                audience: "Students",
                claims: claims,
                expires: DateTime.Now.AddMinutes(5),
                signingCredentials: creds 
                ) ;
            var refreshToken = Guid.NewGuid();
            _service.SetRefreshToken(refreshToken.ToString(), loginRequest.Login);
            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), refreshToken });
        }

        [HttpPost("refresh-token/{token}")]
        public IActionResult RefreshToken(string token)
        {
            var user = _service.CheckRefreshToken(token);
            if (user == null)
                return Forbid("Bearer");

            var claims = _service.GetClaims(user);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var newToken = new JwtSecurityToken(
                issuer: "Gakko",
                audience: "Students",
                claims: claims,
                expires: DateTime.Now.AddMinutes(5),
                signingCredentials: creds
                );
            var refreshToken = Guid.NewGuid();
            _service.SetRefreshToken(refreshToken.ToString(), user);
            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(newToken), refreshToken });

        }

        [HttpPost("change-password")]
        [Authorize]
        public IActionResult ChangePassword(ChangePasswordRequest request)
        {
            // _service.SetPassword(newPassword,User.Claims.)
            var index = User.Claims.ToList()[0].ToString().Split(": ")[1]; //Czy da się jakoś prościej uzyskać konkretny Claim?
            _service.SetPassword(request.NewPassword, index);
            return Ok("Your password has been changed");
        }
    }

  
}
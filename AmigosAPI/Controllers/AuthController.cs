using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AmigosAPI.Data;
using AmigosAPI.Dtos;
using AmigosAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AmigosAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;

        public AuthController(IAuthRepository repo, IConfiguration config)
        {
            _repo = repo;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register (UserForRegisterDto userForRegisterDto)
        {
            userForRegisterDto.Username = userForRegisterDto.Username.ToLower();
            if (await _repo.UserExists(userForRegisterDto.Username))
                return BadRequest("Username already exists");
            var userToCreate = new User
            {
                Username = userForRegisterDto.Username
            };

            var createdUser = await _repo.Register(userToCreate, userForRegisterDto.Password);
            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            var userFromRepo = await _repo.Login(userForLoginDto.Username,
                userForLoginDto.Password);
            if (userFromRepo == null)
                return Unauthorized();

            //user credentials.
            var claims = new[]
            {
                new Claim (ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                new Claim (ClaimTypes.Name, userFromRepo.Username)
            };

            //generate security key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes
                (_config.GetSection("AppSettings:Token").Value));

            //use key as part of sign in credentials and encrypt
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);


            //create token and pass in claims, add expiry date, pass in sign in credentials
            var tokenDesciptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            //create jwt from token
            var token = tokenHandler.CreateToken(tokenDesciptor);

            return Ok(new
            {
                token = tokenHandler.WriteToken(token)
            });


        }


    }
}
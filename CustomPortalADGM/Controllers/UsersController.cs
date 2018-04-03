using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CustomPortalADGM.Dtos;
using CustomPortalADGM.Entities;
using CustomPortalADGM.Helpers;
using CustomPortalADGM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CustomPortalADGM.Controllers
{
  [Authorize, Route("api/[controller]")]
  public class UsersController : Controller
  {
    private IUserService _userService;
    private IMapper _mapper;
    private readonly IConfiguration _configuration;
    public UsersController(
        IUserService userService,
        IMapper mapper,
        IConfiguration Configuration)
    {
      _userService = userService;
      _mapper = mapper;
      _configuration = Configuration;
    }

    [AllowAnonymous, HttpPost("authenticate")]
    public async Task<IActionResult> AuthenticateAsync([FromBody]UserDto userDto)
    {
      var user = await _userService.AuthenticateAsync(userDto.Username, userDto.Password);

      if (user == null)
        return Unauthorized();
      return Ok(new
      {
        user.Id,
        user.Username,
        user.FirstName,
        user.LastName,
        token = await GenerateToken(userDto)
      });
    }
    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Register([FromBody]UserDto userDto)
    {
      // map dto to entity
      var user = _mapper.Map<User>(userDto);

      try
      {
        // save 
        await _userService.CreateAsync(user, userDto.Password);
        return Ok();
      }
      catch (AppException ex)
      {
        // return error message if there was an exception
        return BadRequest(ex.Message);
      }
    }

    [HttpGet]
    public IActionResult GetAll()
    {
      var users = _userService.GetAll();
      var userDtos = _mapper.Map<IList<UserDto>>(users);
      return Ok(userDtos);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
      var user = _userService.GetById(id);
      var userDto = _mapper.Map<UserDto>(user);
      return Ok(userDto);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, [FromBody]UserDto userDto)
    {
      // map dto to entity and set id
      var user = _mapper.Map<User>(userDto);
      user.Id = id;

      try
      {
        // save 
        _userService.Update(user, userDto.Password);
        return Ok();
      }
      catch (AppException ex)
      {
        // return error message if there was an exception
        return BadRequest(ex.Message);
      }
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
      _userService.Delete(id);
      return Ok();
    }
    private Task<string> GenerateToken(UserDto userDto)
    {
      return Task.Factory.StartNew(() =>
      {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, userDto.Username)
        };

        var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
          _configuration["Jwt:Issuer"],
          claims,
          expires: DateTime.Now.AddDays(7),
          signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
      });
    }
  }
}

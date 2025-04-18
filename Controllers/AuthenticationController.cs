﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RopeyDVDSystem.Models;
using RopeyDVDSystem.Models.Identity;
using RopeyDVDSystem.Models.ViewModels;

namespace CourseWorkSampleAuth.Controllers;

public class AuthenticationController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthenticationController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        return RedirectToAction("Index", "Home");
    }


    public IActionResult Login()
    {
        return View(new UserLoginModel());
    }

    // to check the login credentials

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(UserLoginModel loginModel)
    {
        if (!ModelState.IsValid) return View(loginModel);

        var user = await _userManager.FindByNameAsync(loginModel.Username);

        if (user != null && await _userManager.CheckPasswordAsync(user, loginModel.Password))
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new(ClaimTypes.Name, user.UserName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var userRole in userRoles) authClaims.Add(new Claim(ClaimTypes.Role, userRole));

            var token = GetToken(authClaims);

            var userDetails = new UserDetailsViewModel
            {
                UserName = user.UserName,

                Email = user.Email,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo
            };

            Response.Cookies.Append("X-Access-Token",
                userDetails.Token,
                new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict
                });


            return RedirectToAction("Index", "Admin");
        }

        TempData["Error"] = "Invalid credentials. Try again!";
        return View(loginModel);
    }


    // to logout of the session
    public IActionResult Logout()
    {
        Response.Cookies.Delete("X-Access-Token");
        return RedirectToAction("Index");
    }


    // to register user
    public IActionResult RegisterUser()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterUser(UserRegisterModel model)
    {
        var userExists = await _userManager.FindByNameAsync(model.Username);
        if (userExists != null)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new Response {Status = "Error", Message = "User already exists!"});

        ApplicationUser user = new()
        {
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = model.Username
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new Response
                    {Status = "Error", Message = "User creation failed! Please check user details and try again."});
        if (await _roleManager.RoleExistsAsync(UserRoles.Assistant))
            await _userManager.AddToRoleAsync(user, UserRoles.Assistant);
        return RedirectToAction("Index", "Home");
    }


    // to registe admin
    public IActionResult RegisterAdmin()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterAdmin(UserRegisterModel model)
    {
        var userExists = await _userManager.FindByNameAsync(model.Username);
        if (userExists != null)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new Response {Status = "Error", Message = "User already exists!"});

        ApplicationUser user = new()
        {
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = model.Username
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new Response
                    {Status = "Error", Message = "User creation failed! Please check user details and try again."});


        if (await _roleManager.RoleExistsAsync(UserRoles.Manager))
            await _userManager.AddToRoleAsync(user, UserRoles.Manager);

        return RedirectToAction("Index", "Home");
    }

    public IActionResult UnauthorizedAccess()
    {
        return View();
    }


    [HttpGet]
    [Authorize]
    public ActionResult ChangePassword()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    [ValidateAntiForgeryToken]
    public ActionResult ChangePassword1(ChangePassword changePassword)
    {
        if (!ModelState.IsValid) return View(ChangePassword);

        return View();
    }


    private JwtSecurityToken GetToken(List<Claim> authClaims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

        var token = new JwtSecurityToken(
            _configuration["JWT:ValidIssuer"],
            _configuration["JWT:ValidAudience"],
            expires: DateTime.Now.AddHours(3),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return token;
    }
}
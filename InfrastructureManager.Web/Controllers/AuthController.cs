using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace InfrastructureManager.Web.Controllers;

public class AuthController : Controller
{
    private readonly CustomSignInManager          _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(
        CustomSignInManager          signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager   = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(vm);

        var result = await _signInManager.PasswordSignInAsync(
            vm.Email,
            vm.Password,
            isPersistent:     vm.RememberMe,
            lockoutOnFailure: true);  // lockoutOnFailure: true geeft al basisbeveiliging

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty,
                "Account locked due to multiple failed attempts. Try again in 15 minutes.");
        }
        else if (result == IdentitySignInResult.NotAllowed)
        {
            ModelState.AddModelError(string.Empty,
                "This account has been deactivated. Contact your administrator.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
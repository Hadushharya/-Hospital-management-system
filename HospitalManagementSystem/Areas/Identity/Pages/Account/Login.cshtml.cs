using System.ComponentModel.DataAnnotations;
using HospitalManagementSystem.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HospitalManagementSystem.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(
                Input.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account is locked out. Contact an administrator.");
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        return Page();
    }
}
















//using System.ComponentModel.DataAnnotations;
//using HospitalManagementSystem.Data;
//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;

//namespace HospitalManagementSystem.Areas.Identity.Pages.Account;

//[AllowAnonymous]
//public class LoginModel : PageModel
//{
//    private readonly SignInManager<ApplicationUser> _signInManager;

//    public LoginModel(SignInManager<ApplicationUser> signInManager)
//    {
//        _signInManager = signInManager;
//    }

//    [BindProperty]
//    public InputModel Input { get; set; } = new();

//    public string? ReturnUrl { get; set; }

//    [TempData]
//    public string? ErrorMessage { get; set; }

//    public class InputModel
//    {
//        [Required, EmailAddress]
//        public string Email { get; set; } = string.Empty;

//        [Required, DataType(DataType.Password)]
//        public string Password { get; set; } = string.Empty;

//        public bool RememberMe { get; set; }
//    }

//    public async Task OnGetAsync(string? returnUrl = null)
//    {
//        if (!string.IsNullOrEmpty(ErrorMessage))
//        {
//            ModelState.AddModelError(string.Empty, ErrorMessage);
//        }

//        returnUrl ??= Url.Content("~/");
//        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
//        ReturnUrl = returnUrl;
//    }

//    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
//    {
//        returnUrl ??= Url.Content("~/");

//        if (ModelState.IsValid)
//        {
//            var result = await _signInManager.PasswordSignInAsync(
//                Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

//            if (result.Succeeded)
//            {
//                return LocalRedirect(returnUrl);
//            }
//            if (result.IsLockedOut)
//            {
//                ModelState.AddModelError(string.Empty, "This account is locked out. Contact an administrator.");
//                return Page();
//            }

//            ModelState.AddModelError(string.Empty, "Invalid email or password.");
//            return Page();
//        }

//        return Page();
//    }
//}
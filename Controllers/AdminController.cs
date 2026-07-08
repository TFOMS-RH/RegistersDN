using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RegistrDN.Models.Entities;
using RegistrDN.Models.ViewModels;

namespace RegistrDN.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    // Список пользователей
    public async Task<IActionResult> Users()
    {
        var users = _userManager.Users.ToList();
        var userViewModels = new List<UserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new UserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                HospitalCode = user.HospitalCode ?? string.Empty,
                Roles = string.Join(", ", roles),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt ?? DateTime.Now
            });
        }

        return View(userViewModels);
    }

    // Страница добавления пользователя
    [HttpGet]
    public IActionResult Register()
    {
        ViewBag.Roles = _roleManager.Roles.ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(AdminRegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = _roleManager.Roles.ToList();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            HospitalCode = model.HospitalCode,
            RegionCode = model.RegionCode,
            CreatedAt = DateTime.Now,
            IsActive = true  
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Назначаем выбранную роль
            if (!string.IsNullOrEmpty(model.Role))
            {
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            _logger.LogInformation($"Администратор создал пользователя: {model.Email}");
            TempData["Success"] = $"Пользователь {model.Email} успешно создан!";
            return RedirectToAction(nameof(Users));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        ViewBag.Roles = _roleManager.Roles.ToList();
        return View(model);
    }

    // Блокировка/разблокировка пользователя
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = $"Статус пользователя {user.Email} изменен";
        return RedirectToAction(nameof(Users));
    }

    // Удаление пользователя
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        // Нельзя удалить самого себя
        if (user.Email == User.Identity?.Name)
        {
            TempData["Error"] = "Нельзя удалить самого себя";
            return RedirectToAction(nameof(Users));
        }

        await _userManager.DeleteAsync(user);
        TempData["Success"] = $"Пользователь {user.Email} удален";
        return RedirectToAction(nameof(Users));
    }
}
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos_Yo.Data;
using Pedidos_Yo.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Pedidos_Yo.Controllers
{
    public class AccountController : Controller
    {
        private readonly Pedidos_YoDBContext _context;

        public AccountController(Pedidos_YoDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            System.Diagnostics.Debug.WriteLine("Accediendo a la vista Login");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            System.Diagnostics.Debug.WriteLine($"Intento de login con Email: '{model.Email}', Password: '{model.Password}'");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                System.Diagnostics.Debug.WriteLine("Errores de validación: " + string.Join(", ", errors));
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine($"Usuario no encontrado para Email: '{model.Email}'");
                ModelState.AddModelError("", "Correo o contraseña inválidos.");
                return View(model);
            }

            System.Diagnostics.Debug.WriteLine($"Login exitoso para {user.Nombre}, Rol: {user.Rol}");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Nombre),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Rol)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            System.Diagnostics.Debug.WriteLine("Autenticación completada, redirigiendo...");

            switch (user.Rol)
            {
                case "Admin":
                    return RedirectToAction("Index", "Admin");
                case "Cliente":
                    return RedirectToAction("Index", "Product");
                case "Empleado":
                    return RedirectToAction("Index", "Employee");
                default:
                    return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
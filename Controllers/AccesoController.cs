using Microsoft.AspNetCore.Mvc;
using Banco.Data;
using Banco.Models;
using Microsoft.EntityFrameworkCore;
using Banco.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace Banco.Controllers
{
    public class AccesoController : Controller
    {
        private readonly DBContext _dbContext;
        public AccesoController(DBContext dbContext)
        {
           _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registro(UsuarioVM modelo)
        {
            if (modelo.Clave != modelo.ConfirmarClave)
            {
                ViewData["Mensaje"] = "Las claves no coinciden :(";
                return View();
            }

            Usuario usuario = new Usuario()
            {
                Nombre = modelo.Nombre,
                Correo = modelo.Correo,
                Identificacion = modelo.Identificacion,
                Clave = modelo.Clave,
            };

            _dbContext.Usuarios.Add(usuario);
            await _dbContext.SaveChangesAsync(); // Guardar los cambios en la base de datos

            if (usuario.IdUsuario != 0)
            {
                return RedirectToAction("Login", "Acceso");
            }
            else { ViewData["Mensaje"] = "No se pudo crear el usuario. Terrible"; }

            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if(User.Identity.IsAuthenticated)return RedirectToAction("Index", "Home ");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginVM modelo)
        {
            Usuario? usuario_encontrado = await _dbContext.Usuarios.Where(u =>
                u.Correo == modelo.Correo && u.Clave == modelo.Clave
            ).FirstOrDefaultAsync();

            if (usuario_encontrado == null)
            {
                ViewData["Mensaje"] = "No se encontraron coincidencias.Asegúrese que lo ingresado sea correcto";
                return View();
            }

            List<Claim> claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name,usuario_encontrado.Nombre)
            };

            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            AuthenticationProperties properties = new AuthenticationProperties()
            {
                AllowRefresh = true,
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                properties);
            

            return RedirectToAction("Index", "Home");

        }
    }
}

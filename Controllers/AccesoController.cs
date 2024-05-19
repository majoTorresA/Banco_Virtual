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
        
        private const int MaxIntentosFallidos = 3;
        private const int BloqueoHoras = 24;

        private readonly DBContext _dbContext;
        public AccesoController(DBContext dbContext)
        {
           _dbContext = dbContext;
        }

        public async Task CrearCuentaParaUsuario(Usuario nuevoUsuario)
        {
            var nuevaCuenta = new Cuenta
            {
                IdUsuario = nuevoUsuario.IdUsuario, 
                Saldo = 0 
            };

            _dbContext.Cuentas.Add(nuevaCuenta);
            await _dbContext.SaveChangesAsync();
        }

        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registro(UsuarioVM modelo)
        {
            // Verificar si el correo ya está registrado
            if (await _dbContext.Usuarios.AnyAsync(u => u.Correo == modelo.Correo))
            {
                ViewData["Info"] = "Ya existe ese correo. Ingrese otro e intente nuevamente";
                return View();
            }

            if (modelo.Clave != modelo.ConfirmarClave)
            {
                ViewData["Error"] = "Las claves no coinciden :(";
                return View();
            }

            Usuario usuario = new Usuario()
            {
                Nombre = modelo.Nombre,
                Correo = modelo.Correo,
                Identificacion = modelo.Identificacion,
                Clave = modelo.Clave,
                IntentosFallidos = 0,
                BloqueadoHasta = null
            };

            _dbContext.Usuarios.Add(usuario);
            await _dbContext.SaveChangesAsync(); 

            // Verificar si el usuario no tiene una cuenta asociada
            if (!_dbContext.Cuentas.Any(c => c.IdUsuario == usuario.IdUsuario))
            {
                await CrearCuentaParaUsuario(usuario);
            }


            if (usuario.IdUsuario != 0)
            {
                await CrearCuentaParaUsuario(usuario);

                return RedirectToAction("Login", "Acceso");
            }
            else
            {
                ViewData["Error"] = "Error: No se pudo crear el usuario. ";
            }

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
            Usuario? usuario_encontrado = await _dbContext.Usuarios
            .Include(u => u.Cuenta)
            .Where(u => u.Correo == modelo.Correo)
            .FirstOrDefaultAsync();

            if (usuario_encontrado == null)
            {
                ViewData["Info"] = "Correo no encontrado. Reviselo e intente nuevamente";
                return View();
            }

            // Verificar si la cuenta está bloqueada
            if (usuario_encontrado.BloqueadoHasta.HasValue && usuario_encontrado.BloqueadoHasta.Value > DateTime.Now)
            {
                ViewData["Error"] = "Cuenta bloqueada por 24 horas, comunícate con tu banco";
                return View();
            }

            // Verificar la contraseña
            if (usuario_encontrado.Clave != modelo.Clave)
            {
                usuario_encontrado.IntentosFallidos++;

                if (usuario_encontrado.IntentosFallidos >= MaxIntentosFallidos)
                {
                    usuario_encontrado.BloqueadoHasta = DateTime.Now.AddHours(BloqueoHoras);
                    await _dbContext.SaveChangesAsync();
                    ViewData["Error"] = "Cuenta bloqueada por 24 horas, comunícate con tu banco";
                    return View();
                }

                await _dbContext.SaveChangesAsync();
                ViewData["Advertencia"] = $"Contraseña incorrecta. Te quedan {MaxIntentosFallidos - usuario_encontrado.IntentosFallidos} intentos";
                return View();
            }

       
            usuario_encontrado.IntentosFallidos = 0;
            usuario_encontrado.BloqueadoHasta = null; 
            await _dbContext.SaveChangesAsync();


            if (usuario_encontrado.Cuenta == null)
            {
                await CrearCuentaParaUsuario(usuario_encontrado);
            }

            List<Claim> claims = new List<Claim>()
    {
        new Claim(ClaimTypes.NameIdentifier, usuario_encontrado.IdUsuario.ToString()), 
        new Claim(ClaimTypes.Name, usuario_encontrado.Nombre)
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

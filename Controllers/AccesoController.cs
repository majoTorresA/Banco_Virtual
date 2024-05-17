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
            // Crear una nueva cuenta para el usuario
            var nuevaCuenta = new Cuenta
            {
                IdUsuario = nuevoUsuario.IdUsuario, // Asignar el ID del nuevo usuario a la cuenta
                Saldo = 0 // Establecer el saldo inicial en 0
            };

            // Agregar la nueva cuenta a la base de datos
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
                ViewData["Mensaje"] = "Ya existe ese correo. Ingrese otro e intente nuevamente";
                return View();
            }

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
                IntentosFallidos = 0,
                BloqueadoHasta = null
            };

            _dbContext.Usuarios.Add(usuario);
            await _dbContext.SaveChangesAsync(); // Guardar los cambios en la base de datos

            // Verificar si el usuario no tiene una cuenta asociada
            if (!_dbContext.Cuentas.Any(c => c.IdUsuario == usuario.IdUsuario))
            {
                // Crear una cuenta para el nuevo usuario
                await CrearCuentaParaUsuario(usuario);
            }


            if (usuario.IdUsuario != 0)
            {
                // Crear una cuenta para el nuevo usuario
                await CrearCuentaParaUsuario(usuario);

                return RedirectToAction("Login", "Acceso");
            }
            else
            {
                ViewData["Mensaje"] = "No se pudo crear el usuario. Terrible";
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
                ViewData["Mensaje"] = "No se encontraron coincidencias. Asegúrese que lo ingresado sea correcto";
                return View();
            }

            if (usuario_encontrado.Clave != modelo.Clave)
            {
                usuario_encontrado.IntentosFallidos++;

                if (usuario_encontrado.IntentosFallidos >= MaxIntentosFallidos)
                {
                    usuario_encontrado.BloqueadoHasta = DateTime.Now.AddHours(BloqueoHoras);
                    if (usuario_encontrado.BloqueadoHasta.HasValue && usuario_encontrado.BloqueadoHasta.Value > DateTime.Now)
                    {
                        ViewData["Mensaje"] = "Cuenta bloqueada por 24 horas, comunícate con tu banco";
                        return View();
                    }
                    else
                    {
                        usuario_encontrado.IntentosFallidos = 0; // Resetear el contador de intentos fallidos después del bloqueo
                    }
                }
                await _dbContext.SaveChangesAsync();

                ViewData["Mensaje"] = $"Contraseña incorrecta. Intentos restantes: {MaxIntentosFallidos - usuario_encontrado.IntentosFallidos}";
                return View();
            }

            usuario_encontrado.IntentosFallidos = 0; // Resetear los intentos fallidos en caso de inicio de sesión exitoso
            await _dbContext.SaveChangesAsync();

            // Verificar si el usuario no tiene una cuenta asociada
            if (usuario_encontrado.Cuenta == null)
            {
                // Crear una cuenta para el usuario
                await CrearCuentaParaUsuario(usuario_encontrado);
            }

            List<Claim> claims = new List<Claim>()
    {
        new Claim(ClaimTypes.NameIdentifier, usuario_encontrado.IdUsuario.ToString()), // Store IdUsuario in NameIdentifier claim
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

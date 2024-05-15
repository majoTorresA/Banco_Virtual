using Banco.Models;
using Banco.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Banco.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;


namespace Banco.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DBContext _dbContext;

        public HomeController(ILogger<HomeController> logger, DBContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Retiro()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Retiro(string clave, decimal cantidad)
        {
            // Obtener el id del usuario autenticado desde los claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                ViewData["MensajeFallo"] = "Error al obtener el ID del usuario";
                return View();
            }

            if (!int.TryParse(userIdClaim.Value, out int idUsuario))
            {
                ViewData["MensajeFallo"] = "Error al convertir el ID del usuario";
                return View();
            }

            // Obtener el usuario y verificar la clave, incluyendo la propiedad Cuenta
            var usuario = await _dbContext.Usuarios
                                          .Include(u => u.Cuenta)
                                          .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario && u.Clave == clave);
            if (usuario == null)
            {
                ViewData["MensajeFallo"] = "Usuario o clave incorrectos";
                return View();
            }

            // Verificar que el usuario tenga una cuenta asociada
            if (usuario.Cuenta == null)
            {
                ViewData["MensajeFallo"] = "El usuario no tiene una cuenta asociada";
                return View();
            }

            // Verificar que haya suficiente saldo para el retiro
            decimal saldo = usuario.Cuenta.Saldo;
            if (cantidad > saldo)
            {
                ViewData["MensajeFallo"] = "Saldo insuficiente";
                return View();
            }

            // Obtener el tipo de movimiento "Retirar" de la base de datos
            var tipoMovimientoRetirar = _dbContext.TiposMovimiento.FirstOrDefault(t => t.Nombre == "Retirar");
            if (tipoMovimientoRetirar == null)
            {
                ViewData["MensajeFallo"] = "Tipo de movimiento 'Retirar' no encontrado";
                return View();
            }

            // Crear el movimiento de retiro
            var movimiento = new Movimientos
            {
                IdUsuario = idUsuario,
                IdTipoMovimiento = tipoMovimientoRetirar.IdTipoMovimiento,
                Cantidad = cantidad,
                Fecha = DateTime.Now
            };

            // Actualizar el saldo de la cuenta
            usuario.Cuenta.Saldo -= cantidad;

            // Guardar los cambios en la base de datos de movimientos
            _dbContext.Movimientos.Add(movimiento);
            await _dbContext.SaveChangesAsync();

            ViewData["MensajeExito"] = "Retiro realizado con éxito";
            return View();
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Salir()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login","Acceso");
        }
    }
}
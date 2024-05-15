using Banco.Models;
using Banco.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Banco.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
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


       [HttpPost("Retiro")]
    public async Task<IActionResult> Retiro(int idUsuario, decimal cantidad)
    {
        // Obtener el usuario
        var usuario = await _dbContext.Usuarios.FindAsync(idUsuario);
        if (usuario == null)
            return NotFound("Usuario no encontrado");

        // Verificar que haya suficiente saldo para el retiro
        decimal saldo = usuario.Cuenta.Saldo;
        if (cantidad > saldo)
            return BadRequest("Saldo insuficiente");

        // Obtener el tipo de movimiento "Retirar" de la base de datos
        var tipoMovimientoRetirar = _dbContext.TiposMovimiento.FirstOrDefault(t => t.Nombre == "Retirar");
        if (tipoMovimientoRetirar == null)
            return NotFound("Tipo de movimiento 'Retirar' no encontrado");

        // Crear el movimiento de retiro
        var movimiento = new Movimientos
        {
            IdUsuario = idUsuario,
            TipoMovimiento = tipoMovimientoRetirar,
            Cantidad = cantidad,
            Fecha = DateTime.Now
        };

        // Actualizar el saldo de la cuenta
        usuario.Cuenta.Saldo -= cantidad;

        // Guardar los cambios en la base de datos
        _dbContext.Movimientos.Add(movimiento);
        await _dbContext.SaveChangesAsync();

        return Ok("Retiro realizado con éxito");
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
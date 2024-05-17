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

        //movimiento de Consigna
        [HttpGet]
        public IActionResult Consigna()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Consigna(ConsignaViewModel modelo)
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

            // Validar que la cantidad sea positiva
            if (modelo.Cantidad <= 0)
            {
                ViewData["MensajeFallo"] = "La cantidad debe ser mayor a cero";
                return View();
            }

            // Obtener el usuario que consigna
            var usuarioConsignador = await _dbContext.Usuarios.Include(u => u.Cuenta).FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
            if (usuarioConsignador == null || usuarioConsignador.Cuenta == null)
            {
                ViewData["MensajeFallo"] = "Usuario consignador no encontrado o no tiene una cuenta asociada";
                return View();
            }

            // Obtener el destinatario
            var usuarioDestinatario = await _dbContext.Usuarios.Include(u => u.Cuenta).FirstOrDefaultAsync(u => u.Correo == modelo.CorreoDestinatario);
            if (usuarioDestinatario == null || usuarioDestinatario.Cuenta == null)
            {
                ViewData["MensajeFallo"] = "Destinatario no encontrado o no tiene una cuenta asociada";
                return View();
            }

            decimal saldo = usuarioConsignador.Cuenta.Saldo;
            if (modelo.Cantidad > saldo)
            {
                ViewData["MensajeFallo"] = "Saldo insuficiente";
                return View();
            }

            // Crear el movimiento de consignación para el consignador
            var tipoMovimientoConsignar = await _dbContext.TiposMovimiento.FirstOrDefaultAsync(t => t.Nombre == "Consignar");
            if (tipoMovimientoConsignar == null)
            {
                ViewData["MensajeFallo"] = "Tipo de movimiento 'Consignar' no encontrado";
                return View();
            }

            var movimientoConsignador = new Movimientos
            {
                IdUsuario = idUsuario,
                IdTipoMovimiento = tipoMovimientoConsignar.IdTipoMovimiento,
                Cantidad = modelo.Cantidad,
                Fecha = DateTime.Now
            };

            // Crear el movimiento de recepción para el destinatario
            var movimientoDestinatario = new Movimientos
            {
                IdUsuario = usuarioDestinatario.IdUsuario,
                IdTipoMovimiento = tipoMovimientoConsignar.IdTipoMovimiento,
                Cantidad = modelo.Cantidad,
                Fecha = DateTime.Now
            };

            // Actualizar el saldo de la cuenta del consignador y del destinatario
            usuarioConsignador.Cuenta.Saldo -= modelo.Cantidad;
            usuarioDestinatario.Cuenta.Saldo += modelo.Cantidad;

            // Guardar los cambios en la base de datos
            _dbContext.Movimientos.Add(movimientoConsignador);
            _dbContext.Movimientos.Add(movimientoDestinatario);
            await _dbContext.SaveChangesAsync();

            ViewData["MensajeExito"] = "Consignación realizada con éxito";
            return View();
        }


        //Consultar Saldo

        [HttpGet]
        public async Task<IActionResult> ConsultarSaldo()
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

            // Obtener el usuario y verificar la cuenta
            var usuario = await _dbContext.Usuarios
                                          .Include(u => u.Cuenta)
                                          .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
            if (usuario == null)
            {
                ViewData["MensajeFallo"] = "Usuario no encontrado";
                return View();
            }

            // Verificar que el usuario tenga una cuenta asociada
            if (usuario.Cuenta == null)
            {
                ViewData["MensajeFallo"] = "El usuario no tiene una cuenta asociada";
                return View();
            }

            // Retornar el saldo a la vista
            ViewData["Saldo"] = usuario.Cuenta.Saldo;
            return View();
        }

        //Movimiento Depositar
        [HttpGet]
        public IActionResult Depositar()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Depositar(string clave, decimal cantidad)
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
                ViewData["MensajeFallo"] = "Clave incorrecta. Intente de nuevo";
                return View();
            }

            // Verificar que el usuario tenga una cuenta asociada
            if (usuario.Cuenta == null)
            {
                ViewData["MensajeFallo"] = "El usuario no tiene una cuenta asociada";
                return View();
            }

            // Obtener el tipo de movimiento "Depositar" de la base de datos
            var tipoMovimientoDepositar = _dbContext.TiposMovimiento.FirstOrDefault(t => t.Nombre == "Depositar");
            if (tipoMovimientoDepositar == null)
            {
                ViewData["MensajeFallo"] = "Tipo de movimiento 'Depositar' no encontrado";
                return View();
            }

            // Crear el movimiento de deposito
            var movimiento = new Movimientos
            {
                IdUsuario = idUsuario,
                IdTipoMovimiento = tipoMovimientoDepositar.IdTipoMovimiento,
                Cantidad = cantidad,
                Fecha = DateTime.Now
            };

            // Actualizar el saldo de la cuenta
            usuario.Cuenta.Saldo += cantidad;

            // Guardar los cambios en la base de datos de movimientos
            _dbContext.Movimientos.Add(movimiento);
            await _dbContext.SaveChangesAsync();

            ViewData["MensajeExito"] = "Deposito realizado con éxito";
            return View();
        }

        //Consultar Movimientos
        [HttpGet]
        public async Task<IActionResult> ConsultarMovimientos()
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

            // Obtener los movimientos del usuario que sean de tipo "Retirar", "Consignar" o depositar
            var movimientos = await _dbContext.Movimientos
                .Include(m => m.TipoMovimiento)
                .Where(m => m.IdUsuario == idUsuario && (m.TipoMovimiento.Nombre == "Retirar" || m.TipoMovimiento.Nombre == "Consignar" || m.TipoMovimiento.Nombre == "Depositar"))
                .ToListAsync();

            return View(movimientos);
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
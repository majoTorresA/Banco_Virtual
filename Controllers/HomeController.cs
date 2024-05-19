using Banco.Models;
using Banco.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Banco.ViewModels;
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
        private async Task<int?> GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int idUsuario))
            {
                return null;
            }
            return idUsuario;
        }

        private async Task<Usuario> GetAuthenticatedUserWithAccount(int idUsuario)
        {
            return await _dbContext.Usuarios.Include(u => u.Cuenta).FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
        }

        private async Task<TipoMovimiento> GetTipoMovimientoByName(string nombre)
        {
            return await _dbContext.TiposMovimiento.FirstOrDefaultAsync(t => t.Nombre == nombre);
        }


        [HttpGet]
        public IActionResult Retiro()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Retiro(string clave, decimal cantidad)
        {
            var idUsuario = await GetAuthenticatedUserId();
            if (idUsuario == null)
            {
                ViewData["Error"] = "Error al obtener el ID del usuario";
                return View();
            }

            var usuario = await GetAuthenticatedUserWithAccount(idUsuario.Value);
            if (usuario == null || usuario.Clave != clave)
            {
                ViewData["Error"] = "Usuario o clave incorrectos";
                return View();
            }

            if (cantidad > usuario.Cuenta.Saldo)
            {
                ViewData["Error"] = "No tiene saldo suficiente";
                return View();
            }

            var tipoMovimientoRetirar = await GetTipoMovimientoByName("Retirar");
            if (tipoMovimientoRetirar == null)
            {
                ViewData["Error"] = "Tipo de movimiento 'Retirar' no encontrado";
                return View();
            }

            var movimiento = new Movimientos
            {
                IdUsuario = idUsuario.Value,
                IdTipoMovimiento = tipoMovimientoRetirar.IdTipoMovimiento,
                Cantidad = cantidad,
                Fecha = DateTime.Now
            };

            usuario.Cuenta.Saldo -= cantidad;
            _dbContext.Movimientos.Add(movimiento);
            await _dbContext.SaveChangesAsync();

            ViewData["Exito"] = "Retiro realizado con éxito";
            ViewData["VerSaldo"] = usuario.Cuenta.Saldo;
            return View();
        }


        [HttpGet]
        public IActionResult Consigna()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Consigna(ConsignaViewModel modelo)
        {
            var idUsuario = await GetAuthenticatedUserId();
            if (idUsuario == null)
            {
                ViewData["Error"] = "Error al obtener el ID del usuario";
                return View();
            }

            if (modelo.Cantidad <= 0)
            {
                ViewData["Error"] = "La cantidad debe ser mayor a cero";
                return View();
            }

            var transfiere = await GetAuthenticatedUserWithAccount(idUsuario.Value);
            if (transfiere == null)
            {
                ViewData["Error"] = "Usuario No encontrado";
                return View();
            }

            var recibe = await _dbContext.Usuarios.Include(u => u.Cuenta).FirstOrDefaultAsync(u => u.Correo == modelo.CorreoDestinatario);
            if (recibe == null)
            {
                ViewData["Error"] = "Destinatario no encontrado.Revise si ha escrito correctamente";
                return View();
            }

            if (modelo.Cantidad > transfiere.Cuenta.Saldo)
            {
                ViewData["Error"] = "Saldo insuficiente para hacer ese movimiento";
                return View();
            }

            var tipoMovimientoConsignar = await GetTipoMovimientoByName("Consignar");
            if (tipoMovimientoConsignar == null)
            {
                ViewData["Error"] = "Tipo de movimiento 'Consignar' no encontrado";
                return View();
            }

            var movimientoConsigna = new Movimientos
            {
                IdUsuario = idUsuario.Value,
                IdTipoMovimiento = tipoMovimientoConsignar.IdTipoMovimiento,
                Cantidad = modelo.Cantidad,
                Fecha = DateTime.Now
            };

            var movimientoDestinatario = new Movimientos
            {
                IdUsuario = recibe.IdUsuario,
                IdTipoMovimiento = tipoMovimientoConsignar.IdTipoMovimiento,
                Cantidad = modelo.Cantidad,
                Fecha = DateTime.Now
            };

            transfiere.Cuenta.Saldo -= modelo.Cantidad;
            recibe.Cuenta.Saldo += modelo.Cantidad;

            _dbContext.Movimientos.Add(movimientoConsigna);
            _dbContext.Movimientos.Add(movimientoDestinatario);
            await _dbContext.SaveChangesAsync();

            ViewData["Exito"] = "Consignación realizada con éxito";
            ViewData["VerSaldo"] = transfiere.Cuenta.Saldo;
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> ConsultarSaldo()
        {
            var idUsuario = await GetAuthenticatedUserId();
            if (idUsuario == null)
            {
                ViewData["Error"] = "Error al obtener el ID del usuario";
                return View();
            }

            var usuario = await GetAuthenticatedUserWithAccount(idUsuario.Value);
            if (usuario == null)
            {
                ViewData["Error"] = "Usuario no encontrado";
                return View();
            }

            ViewData["VerSaldo"] = usuario.Cuenta.Saldo;
            return View();
        }


        [HttpGet]
        public IActionResult Depositar()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Depositar(string clave, decimal cantidad)
        {
            var idUsuario = await GetAuthenticatedUserId();
            if (idUsuario == null)
            {
                ViewData["Error"] = "Error al obtener el ID del usuario";
                return View();
            }

            var usuario = await GetAuthenticatedUserWithAccount(idUsuario.Value);
            if (usuario == null || usuario.Clave != clave)
            {
                ViewData["Error"] = "Clave incorrecta. Intente de nuevo";
                return View();
            }

            var tipoMovimientoDepositar = await GetTipoMovimientoByName("Depositar");
            if (tipoMovimientoDepositar == null)
            {
                ViewData["Error"] = "Tipo de movimiento 'Depositar' no encontrado";
                return View();
            }

            var movimiento = new Movimientos
            {
                IdUsuario = idUsuario.Value,
                IdTipoMovimiento = tipoMovimientoDepositar.IdTipoMovimiento,
                Cantidad = cantidad,
                Fecha = DateTime.Now
            };

            usuario.Cuenta.Saldo += cantidad;
            _dbContext.Movimientos.Add(movimiento);
            await _dbContext.SaveChangesAsync();

            ViewData["Exito"] = "Depósito realizado con éxito";
            ViewData["VerSaldo"] = usuario.Cuenta.Saldo;
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> ConsultarMovimientos()
        {
            var idUsuario = await GetAuthenticatedUserId();
            if (idUsuario == null)
            {
                ViewData["Error"] = "Error al obtener el ID del usuario";
                return View();
            }

            var usuario = await GetAuthenticatedUserWithAccount(idUsuario.Value);
            if (usuario == null)
            {
                ViewData["Error"] = "Usuario o cuenta no encontrados";
                return View();
            }

            var movimientos = await _dbContext.Movimientos
                .Include(m => m.TipoMovimiento)
                .Include(m => m.Usuario)
                .Where(m => m.IdUsuario == idUsuario || (m.TipoMovimiento.Nombre == "Consignar" && m.Usuario.Correo == usuario.Correo))
                .OrderBy(m => m.Fecha)
                .ToListAsync();

            var movimientosViewModel = movimientos.Select(movimiento => new ConsultaMovimientoVM
            {
                Fecha = movimiento.Fecha,
                TipoMovimiento = movimiento.TipoMovimiento.Nombre,
                Cantidad = movimiento.Cantidad
            }).ToList();

            movimientosViewModel.Reverse();

            return View(movimientosViewModel);
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
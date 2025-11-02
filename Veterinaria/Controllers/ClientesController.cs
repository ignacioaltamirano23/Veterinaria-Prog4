using LogicaDeNegocio.Context;
using LogicaDeNegocio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Veterinariat.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class ClientesController : Controller
    {
        private readonly AppDbContext _context;

        public ClientesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Cliente/MisMascotas 
        public async Task<IActionResult> MisMascotas()
        {
            try
            {               
                var clienteId = await ObtenerClienteId();
                if (clienteId == null)
                {
                    TempData["MensajeError"] = "No se encontró información del cliente.";
                    return RedirectToAction("AccessDenied", "Login");
                }

                var mascotas = await GetMascotasClienteQuery(clienteId)
                    .OrderBy(m => m.Nombre)
                    .ToListAsync();               

                return View(mascotas);
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al cargar las mascotas.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Cliente/MisTurnos 
        public async Task<IActionResult> MisTurnos()
        {
            try
            {
                var clienteId = await ObtenerClienteId();
                if (clienteId == null)
                {
                    return RedirectToAction("AccessDenied", "Login");
                }

                var turnos = await GetTurnosClienteQuery(clienteId)
                    .OrderByDescending(t => t.FechaHora)
                    .ToListAsync();

                return View(turnos);
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al cargar los turnos.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Cliente/CancelarTurno/5 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarTurno(int id)
        {
            var clienteId = await ObtenerClienteId();
            if (clienteId == null)
            {
                return RedirectToAction("AccessDenied", "Login");
            }

            var turno = await _context.Turnos
                .Include(t => t.Mascota)
                .FirstOrDefaultAsync(t => t.Id == id && t.Mascota.ClienteId == clienteId);

            if (turno == null)
            {
                TempData["MensajeError"] = "Turno no encontrado.";
                return RedirectToAction(nameof(MisTurnos));
            }

            if (turno.EstadoTurno == EstadoTurno.Pendiente || turno.EstadoTurno == EstadoTurno.Confirmado)
            {
                turno.EstadoTurno = EstadoTurno.Cancelado;
                _context.Update(turno);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = "Turno cancelado correctamente.";
            }
            else
            {
                TempData["MensajeError"] = "No se puede cancelar un turno que ya fue cancelado.";
            }

            return RedirectToAction(nameof(MisTurnos));
        }

        private async Task<string?> ObtenerClienteId()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var usuario = await _context.Usuarios
                .Include(u => u.Cliente)
                .FirstOrDefaultAsync(u => u.Id.Equals(usuarioId));

            return usuario?.Cliente?.UsuarioId;
        }

        private IQueryable<Mascota> GetMascotasClienteQuery(string clienteId)
        {
            return _context.Mascotas
                .Where(m => m.ClienteId == clienteId)
                .Include(m => m.Turnos)
                .ThenInclude(t => t.Veterinario)
                .ThenInclude(v => v.Usuario);
        }

        private IQueryable<Turno> GetTurnosClienteQuery(string clienteId)
        {
            return _context.Turnos
                .Include(t => t.Mascota)
                .Include(t => t.Veterinario)
                .ThenInclude(v => v.Usuario)
                .Where(t => t.Mascota.ClienteId == clienteId);
        }
    }
}

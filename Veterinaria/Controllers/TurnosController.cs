using LogicaDeNegocio.Context;
using LogicaDeNegocio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace VetTest.Controllers
{
    [Authorize(Roles = "Administrador,Veterinario")]
    public class TurnosController : Controller
    {
        private readonly AppDbContext _context;
                
        private string IdUsuario => User.FindFirstValue(ClaimTypes.NameIdentifier);        
        private bool EsAdmin => User.IsInRole("Administrador");
        private bool EsVeterinario => User.IsInRole("Veterinario");

        public TurnosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Turnos
        public async Task<IActionResult> Index()
        {
            IQueryable<Turno> turnosQuery = _context.Turnos
                .Include(t => t.Mascota)
                .ThenInclude(m => m.Cliente)
                .ThenInclude(c => c.Usuario)
                .Include(t => t.Veterinario)
                .ThenInclude(v => v.Usuario);

            // Si es veterinario, solo ver sus turnos
            if (EsVeterinario)
            {
                turnosQuery = turnosQuery.Where(t => t.Veterinario.UsuarioId == IdUsuario);
            }                

            var turnos = await turnosQuery
                .OrderByDescending(t => t.FechaHora)
                .ToListAsync();

            return View(turnos);
        }

        // GET: Turnos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var turno = await _context.Turnos
                .Include(t => t.Mascota)
                .ThenInclude(m => m.Cliente)
                .ThenInclude(c => c.Usuario)
                .Include(t => t.Veterinario)
                .ThenInclude(v => v.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (turno == null) return NotFound();            

            // Validar acceso de veterinario
            if (EsVeterinario && turno.Veterinario.UsuarioId != IdUsuario)
            {
                return RedirectToAction("AccessDenied", "Login");
            }                

            return View(turno);
        }

        // GET: Turnos/Create
        public async Task<IActionResult> Create()
        {
            await CargarListasDesplegables(IdUsuario);

            var ahora = DateTime.Now;
            var proximaHora = ahora.AddHours(1);

            var turno = new Turno
            {
                FechaHora = new DateTime(proximaHora.Year, proximaHora.Month, proximaHora.Day, proximaHora.Hour, 0, 0),
                EstadoTurno = EstadoTurno.Confirmado,
                VeterinarioId = EsVeterinario ? IdUsuario : null
            };

            return View(turno);
        }

        // POST: Turnos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FechaHora,MascotaId,VeterinarioId,EstadoTurno")] Turno turno)
        {
            turno.EstadoTurno = EstadoTurno.Confirmado;

            if (EsAdmin && string.IsNullOrEmpty(turno.VeterinarioId))
            {
                ViewBag.MensajeError = "Debes seleccionar un veterinario para crear el turno.";
                await CargarListasDesplegables(IdUsuario);
                return View(turno);
            }

            if (ModelState.IsValid)
            {
                _context.Add(turno);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = "Turno creado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            await CargarListasDesplegables(IdUsuario);
            return View(turno);
        }

        // GET: Turnos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
                        
            var turno = await _context.Turnos
                .Include(t => t.Veterinario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (turno == null) return NotFound();            

            if (EsVeterinario && turno.Veterinario.UsuarioId != IdUsuario)
            {
                return RedirectToAction("AccessDenied", "Login");
            }                

            await CargarListasDesplegables(IdUsuario, turno.VeterinarioId);
            return View(turno);
        }

        // POST: Turnos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, 
            [Bind("Id,FechaHora,MascotaId,VeterinarioId,EstadoTurno")] Turno turno)
        {
            if (id != turno.Id) return NotFound();

            if (EsVeterinario && turno.VeterinarioId != IdUsuario)
            {
                return RedirectToAction("AccessDenied", "Login");
            }                

            if (EsAdmin && string.IsNullOrEmpty(turno.VeterinarioId))
            {
                TempData["MensajeError"] = "Debes seleccionar un veterinario para actualizar el turno.";
                await CargarListasDesplegables(IdUsuario, turno.VeterinarioId);
                return View(turno);
            }

            if (ModelState.IsValid)
            {
                _context.Update(turno);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = "Turno actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            await CargarListasDesplegables(IdUsuario, turno.VeterinarioId);
            return View(turno);
        }

        [Authorize(Roles = "Administrador")]
        // GET: Turnos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var turno = await _context.Turnos
                .Include(t => t.Mascota)
                .ThenInclude(m => m.Cliente)
                .ThenInclude(c => c.Usuario)
                .Include(t => t.Veterinario)
                .ThenInclude(v => v.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (turno == null) return NotFound();

            return View(turno);
        }

        // POST: Turnos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var turno = await _context.Turnos.FindAsync(id);

            if (turno != null)
            {
                _context.Turnos.Remove(turno);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = "Turno eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarListasDesplegables(string idUsuario, string veterinarioIdSeleccionado = null)
        {
            var mascotas = await _context.Mascotas
                .Include(m => m.Cliente)
                .ThenInclude(c => c.Usuario)
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            ViewData["MascotaId"] = mascotas.Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = $"{m.Nombre} ({m.Cliente.Usuario.Nombre})"
            }).ToList();

            if (EsAdmin)
            {
                var veterinarios = await _context.Veterinarios
                    .Include(v => v.Usuario)
                    .OrderBy(v => v.Usuario.Nombre)
                    .ToListAsync();

                ViewData["VeterinarioId"] = veterinarios.Select(v => new SelectListItem
                {
                    Value = v.UsuarioId.ToString(),
                    Text = v.Usuario.Nombre
                }).ToList();
            }
            else
            {
                var veterinarioActual = await _context.Veterinarios
                    .Include(v => v.Usuario)
                    .FirstOrDefaultAsync(v => v.UsuarioId == idUsuario);

                if (veterinarioActual != null)
                {
                    ViewData["VeterinarioId"] = new List<SelectListItem>
                    {
                        new SelectListItem
                        {
                            Value = veterinarioActual.UsuarioId.ToString(),
                            Text = veterinarioActual.Usuario.Nombre,
                            Selected = true
                        }
                    };
                }
            }

            ViewData["EstadosTurno"] = new SelectList(Enum.GetValues(typeof(EstadoTurno)));
        }
    }
}

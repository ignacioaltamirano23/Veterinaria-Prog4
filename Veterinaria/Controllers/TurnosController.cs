using LogicaDeNegocio.Context;
using LogicaDeNegocio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Veterinaria.Controllers
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
            var turnos = await GetTurnosQuery().ToListAsync();
            return View(turnos);
        }
                       
        // GET: Turnos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var turno = await GetTurnosBaseQuery()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (turno == null) return NotFound();  
            
            if(!await TieneAccesoTurno(turno.Id))
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

            // Validaciones
            if (EsAdmin && string.IsNullOrEmpty(turno.VeterinarioId))
            {
                ModelState.AddModelError("VeterinarioId", "Debe seleccionar un veterinario.");
            }

            // Validar que la mascota existe
            if (!await _context.Mascotas.AnyAsync(m => m.Id == turno.MascotaId))
            {
                ModelState.AddModelError("MascotaId", "La mascota seleccionada no existe.");
            }

            // Validar que el veterinario existe (si es admin)
            if (EsAdmin && !string.IsNullOrEmpty(turno.VeterinarioId) &&
                !await _context.Veterinarios.AnyAsync(v => v.UsuarioId == turno.VeterinarioId))
            {
                ModelState.AddModelError("VeterinarioId", "El veterinario seleccionado no existe.");
            }

            // Validar que no haya turnos solapados para el veterinario
            if (await ExisteTurnoSolapado(turno))
            {
                ModelState.AddModelError("FechaHora", "Ya existe un turno para este veterinario en la fecha y hora seleccionada.");
            }

            if (!ModelState.IsValid)
            {
                await CargarListasDesplegables(IdUsuario, turno.VeterinarioId);
                return View(turno);
            }

            try
            {
                _context.Add(turno);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = "Turno creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.MensajeError = "Error al crear el turno. Intente nuevamente.";
                await CargarListasDesplegables(IdUsuario, turno.VeterinarioId);
                return View(turno);
            }
        }

        // GET: Turnos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            // Validaciones
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

            if (!await TieneAccesoTurno(id))
            {
                return RedirectToAction("AccessDenied", "Login");
            }

            if (!ModelState.IsValid)
            {
                await CargarListasDesplegables(IdUsuario, turno.VeterinarioId);
                return View(turno);
            }

            // Validaciones
            if (EsAdmin && string.IsNullOrEmpty(turno.VeterinarioId))
            {
                ModelState.AddModelError("VeterinarioId", "Debe seleccionar un veterinario.");
            }

            if (!await _context.Mascotas.AnyAsync(m => m.Id == turno.MascotaId))
            {
                ModelState.AddModelError("MascotaId", "La mascota seleccionada no existe.");
            }
                       
            if (EsAdmin && !string.IsNullOrEmpty(turno.VeterinarioId) &&
                !await _context.Veterinarios.AnyAsync(v => v.UsuarioId == turno.VeterinarioId))
            {
                ModelState.AddModelError("VeterinarioId", "El veterinario seleccionado no existe.");
            }

            if (await ExisteTurnoSolapado(turno))
            {
                ModelState.AddModelError("FechaHora", "Ya existe un turno para este veterinario en la fecha y hora seleccionada.");
            }

            try
            {
                _context.Update(turno);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = "Turno actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.MensajeError = "Error al actualizar el turno.";
                await CargarListasDesplegables(IdUsuario, turno.VeterinarioId);
                return View(turno);
            }            
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
            // Validaciones
            try
            {
                var turno = await _context.Turnos.FindAsync(id);                

                if (turno == null)
                {
                    TempData["MensajeError"] = "Turno no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Turnos.Remove(turno);
                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = "Turno eliminado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al eliminar el turno.";
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

        private IQueryable<Turno> GetTurnosBaseQuery()
        {
            return _context.Turnos
                .Include(t => t.Mascota)
                .ThenInclude(m => m.Cliente)
                .ThenInclude(c => c.Usuario)
                .Include(t => t.Veterinario)
                .ThenInclude(v => v.Usuario);
        }

        private IQueryable<Turno> GetTurnosQuery()
        {
            var query = GetTurnosBaseQuery();

            if (EsVeterinario)
            {
                query = query.Where(t => t.Veterinario.UsuarioId == IdUsuario);
            }

            return query.OrderByDescending(t => t.FechaHora);
        }  
        
        private async Task<bool> TieneAccesoTurno(int turnoId)
        {
            if (EsAdmin) return true;

            if (EsVeterinario)
            {
                return await _context.Turnos
                    .AnyAsync(t => t.Id == turnoId && t.Veterinario.UsuarioId == IdUsuario);
            }

            return false;
        }

        private async Task<bool> ExisteTurnoSolapado(Turno turno)
        {
            if (string.IsNullOrEmpty(turno.VeterinarioId))
                return false;

            return await _context.Turnos
                .AnyAsync(t => t.VeterinarioId == turno.VeterinarioId
                    && t.Id != turno.Id
                    && t.FechaHora == turno.FechaHora
                    && t.EstadoTurno != EstadoTurno.Cancelado);
        }
    }
}

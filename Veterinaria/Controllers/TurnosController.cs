using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LogicaDeNegocio.Context;
using LogicaDeNegocio.Models;


namespace VeterinariaTest.Controllers
{
    public class TurnosController : Controller
    {
        private readonly AppDbContext _context;

        public TurnosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Turnos
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Turnos.Include(t => t.Mascota);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Turnos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var turno = await _context.Turnos
                .Include(t => t.Mascota)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (turno == null)
            {
                return NotFound();
            }

            return View(turno);
        }

        // GET: Turnos/Create
        public IActionResult Create()
        {
            ViewData["MascotaId"] = new SelectList(
                _context.Mascotas
                .Select(m => new
                {
                    m.Id,
                    Display = m.Nombre + " (" + m.Raza + ")"
                }).ToList(),
                "Id",
                "Display"
             );

            ViewData["Estados"] = new SelectList(Enum.GetValues(typeof(EstadoTurno))
                .Cast<EstadoTurno>()
                .Select(e => new { Value = e, Text = e.ToString() }),
            "Value", "Text");

            return View();
        }

        // POST: Turnos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FechaHora,EstadoTurno,MascotaId")] Turno turno)
        {
            if (ModelState.IsValid)
            {
                _context.Add(turno);
                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = "El turno fue registrado correctamente.";

                return RedirectToAction(nameof(Index));
            }            

            ViewData["MascotaId"] = new SelectList(
                _context.Mascotas
                    .Select(m => new
                    {
                        m.Id,
                        Display = m.Nombre + " (" + m.Raza + ")"
                    }).ToList(),
                "Id",
                "Display",
                turno.MascotaId
            );

            ViewData["Estados"] = new SelectList(
                Enum.GetValues(typeof(EstadoTurno))
                    .Cast<EstadoTurno>()
                    .Select(e => new { Value = e, Text = e.ToString() }),
                "Value",
                "Text",
                turno.EstadoTurno
            );

            return View(turno);
        }

        // GET: Turnos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var turno = await _context.Turnos.FindAsync(id);
            if (turno == null)
            {
                return NotFound();
            }

            ViewData["MascotaId"] = new SelectList(
              _context.Mascotas
                  .Select(m => new
                  {
                      m.Id,
                      Display = m.Nombre + " (" + m.Raza + ")"
                  }).ToList(),
              "Id",
              "Display",
              turno.MascotaId
          );

            ViewData["Estados"] = new SelectList(
                Enum.GetValues(typeof(EstadoTurno))
                    .Cast<EstadoTurno>()
                    .Select(e => new { Value = e, Text = e.ToString() }),
                "Value",
                "Text",
                turno.EstadoTurno
            );
            return View(turno);
        }

        // POST: Turnos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FechaHora,EstadoTurno,MascotaId")] Turno turno)
        {
            if (id != turno.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(turno);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TurnoExists(turno.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["MensajeExito"] = "El turno fue actualizado correctamente.";

                return RedirectToAction(nameof(Index));
            }
            

            ViewData["MascotaId"] = new SelectList(
                _context.Mascotas
                    .Select(m => new
                    {
                        m.Id,
                        Display = m.Nombre + " (" + m.Raza + ")"
                    }).ToList(),
                "Id",
                "Display",
                turno.MascotaId
            );

            ViewData["Estados"] = new SelectList(
                Enum.GetValues(typeof(EstadoTurno))
                    .Cast<EstadoTurno>()
                    .Select(e => new { Value = e, Text = e.ToString() }),
                "Value",
                "Text",
                turno.EstadoTurno
            );
            return View(turno);
        }

        // GET: Turnos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var turno = await _context.Turnos
                .Include(t => t.Mascota)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (turno == null)
            {
                return NotFound();
            }

            return View(turno);
        }

        // POST: Turnos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var turno = await _context.Turnos.FindAsync(id);
            if (turno != null)
            {
                _context.Turnos.Remove(turno);
                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = "El turno fue eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TurnoExists(int id)
        {
            return _context.Turnos.Any(e => e.Id == id);
        }
    }
}

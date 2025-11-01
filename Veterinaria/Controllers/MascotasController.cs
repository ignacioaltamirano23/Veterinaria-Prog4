using LogicaDeNegocio.Context;
using LogicaDeNegocio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Veterinaria.Controllers
{
    [Authorize(Roles = "Administrador,Veterinario")]
    public class MascotasController : Controller
    {
        private readonly AppDbContext _context;

        public MascotasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Mascotas 
        public async Task<IActionResult> Index()
        {
            var mascotas = await _context.Mascotas
                .Include(m => m.Cliente)
                .ThenInclude(c => c.Usuario)
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            return View(mascotas);
        }

        // GET: Mascotas/Details/5 
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mascota = await _context.Mascotas
                .Include(m => m.Cliente)
                .ThenInclude(c => c.Usuario)
                .Include(m => m.Turnos)
                .ThenInclude(t => t.Veterinario)
                .ThenInclude(v => v.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mascota == null)
            {
                return NotFound();
            }

            return View(mascota);
        }

        // GET: Mascotas/Create 
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create()
        {
            await CargarClientes();
            return View();
        }

        // POST: Mascotas/Create 
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([Bind("Nombre,Especie,Raza,FechaNacimiento,ClienteId")] Mascota mascota)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mascota);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"Mascota {mascota.Nombre} registrada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            await CargarClientes();
            return View(mascota);
        }

        // GET: Mascotas/Edit/5 
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mascota = await _context.Mascotas.FindAsync(id);
            if (mascota == null)
            {
                return NotFound();
            }

            await CargarClientes();
            return View(mascota);
        }

        // POST: Mascotas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit
            (int id,
            [Bind("Id,Nombre,Especie,Raza,FechaNacimiento,ClienteId")] Mascota mascota)
        {
            if (id != mascota.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(mascota);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"Mascota {mascota.Nombre} actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            await CargarClientes();
            return View(mascota);
        }

        // GET: Mascotas/Delete/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mascota = await _context.Mascotas
                .Include(m => m.Cliente)
                .ThenInclude(c => c.Usuario)
                .Include(m => m.Turnos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mascota == null)
            {
                return NotFound();
            }

            return View(mascota);
        }

        // POST: Mascotas/Delete/5 
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mascota = await _context.Mascotas
                .Include(m => m.Turnos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mascota != null)
            {
                // Eliminar turnos asociados primero
                if (mascota.Turnos.Any())
                {
                    _context.Turnos.RemoveRange(mascota.Turnos);
                }

                _context.Mascotas.Remove(mascota);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"Mascota {mascota.Nombre} eliminada correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarClientes()
        {
            var clientes = await _context.Clientes
                .Include(c => c.Usuario)
                .OrderBy(c => c.Usuario.Nombre)
                .ToListAsync();

            ViewData["ClienteId"] = clientes.Select(c => new SelectListItem
            {
                Value = c.UsuarioId.ToString(),
                Text = $"{c.Usuario.Nombre} ({c.Usuario.Email})"
            }).ToList();
        }
    }
}

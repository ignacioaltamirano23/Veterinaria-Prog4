using LogicaDeNegocio.Context;
using LogicaDeNegocio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace VetTest.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _context;

        private string IdUsuario => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Cliente)
                .Include(u => u.Veterinario)
                .OrderBy(u => u.Email)
                .ToListAsync();

            return View(usuarios);
        }

        // GET: Usuarios/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Cliente)
                .Include(u => u.Veterinario)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // GET: Usuarios/Create
        public async Task<IActionResult> Create()
        {
            ViewData["RolId"] = new SelectList(await _context.Roles.ToListAsync(), "Id", "Nombre");
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Email,Telefono,Direccion,RolId")] Usuario usuario)
        {
            // Quitar validación del rol
            ModelState.Remove("Rol");

            if (!ModelState.IsValid)
            {
                ViewData["RolId"] = new SelectList(await _context.Roles.ToListAsync(), "Id", "Nombre", usuario.RolId);
                return View(usuario);
            }

            // Crear Id y GoogleId
            if (string.IsNullOrEmpty(usuario.Id))
            {
                usuario.Id = Guid.NewGuid().ToString();
            }

            usuario.GoogleId = $"{Guid.NewGuid()}";

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var rol = await _context.Roles.FindAsync(usuario.RolId);

            if (rol != null)
            {
                if (rol.Nombre == "Veterinario")
                {
                    _context.Veterinarios.Add(new Veterinario { UsuarioId = usuario.Id });
                }
                else if (rol.Nombre == "Cliente")
                {
                    _context.Clientes.Add(new Cliente { UsuarioId = usuario.Id });
                }

                await _context.SaveChangesAsync();
            }

            TempData["MensajeExito"] = $"Usuario {usuario.Nombre} registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }


        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Cliente)
                .Include(u => u.Veterinario)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null) return NotFound();

            return View(usuario); 
        }


        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id, 
            [Bind("Id,Nombre,Email,Telefono,Direccion,RolId,GoogleId")] Usuario usuarioEditado)
        {
            if (id != usuarioEditado.Id) return NotFound();

            var usuarioOriginal = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuarioOriginal == null) return NotFound();

            // Limpiamos error de Rol (porque no lo enviamos en el form)
            ModelState.Remove("Rol");

            if (ModelState.IsValid)
            {
                // Solo actualizamos los datos personales
                usuarioOriginal.Nombre = usuarioEditado.Nombre;
                usuarioOriginal.Email = usuarioEditado.Email;
                usuarioOriginal.Telefono = usuarioEditado.Telefono;
                usuarioOriginal.Direccion = usuarioEditado.Direccion;

                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = $"Usuario {usuarioOriginal.Nombre} actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            return View(usuarioEditado);
        }

        // GET: Usuarios/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Cliente)
                .Include(u => u.Veterinario)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Cliente)
                .Include(u => u.Veterinario)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                TempData["MensajeError"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Evitar que un administrador se elimine a sí mismo
            if (usuario.Id == IdUsuario)
            {
                TempData["MensajeError"] = "No podés eliminar tu propio usuario.";
                return RedirectToAction(nameof(Index));
            }

            // Si es veterinario, manejamos sus turnos
            if (usuario.Veterinario != null)
            {
                await ManejarEliminacionVeterinario(usuario.Veterinario.UsuarioId);
            }

            // Eliminamos sus entidades relacionadas
            if (usuario.Cliente != null)
                _context.Clientes.Remove(usuario.Cliente);

            if (usuario.Veterinario != null)
                _context.Veterinarios.Remove(usuario.Veterinario);

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = $"Usuario {usuario.Email} eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }


        // GET: Usuarios/AsignarRol/5
        [HttpGet]
        public async Task<IActionResult> AsignarRol(string? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Cliente)
                .Include(u => u.Veterinario)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // POST: Usuarios/AsignarRol/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarRol(string id, string nuevoRol)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Cliente)
                .Include(u => u.Veterinario)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null) return NotFound();

            // Evitar que el usuario se cambie su propio rol
            if (usuario.Id == IdUsuario)
            {
                TempData["MensajeError"] = "No podés cambiar tu propio rol.";
                return RedirectToAction(nameof(Index));
            }

            var rolEntidad = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == nuevoRol);
            if (rolEntidad == null)
            {
                TempData["MensajeError"] = "Rol inválido.";
                return RedirectToAction(nameof(AsignarRol), new { id });
            }

            await AsignarRolUsuario(usuario, rolEntidad);

            TempData["MensajeExito"] = $"Rol {rolEntidad.Nombre} asignado correctamente a {usuario.Email}";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> AsignarRolUsuario(Usuario usuario, Rol nuevoRol)
        {
            var rolAnterior = usuario.Rol;
            usuario.RolId = nuevoRol.Id;
            usuario.Rol = nuevoRol;

            
            if (nuevoRol.Nombre == "Veterinario")
            {
                if (usuario.Veterinario == null) usuario.Veterinario = new Veterinario();
                if (rolAnterior?.Nombre == "Cliente" && usuario.Cliente != null)
                {
                    _context.Clientes.Remove(usuario.Cliente);
                    usuario.Cliente = null;
                }
            }
            else if (nuevoRol.Nombre == "Cliente")
            {
                if (usuario.Cliente == null) usuario.Cliente = new Cliente();
                if (rolAnterior?.Nombre == "Veterinario" && usuario.Veterinario != null)
                {
                    await ManejarEliminacionVeterinario(usuario.Veterinario.UsuarioId);
                    usuario.Veterinario = null;
                }
            }
            else if (nuevoRol.Nombre == "Administrador")
            {
                if (usuario.Cliente != null)
                {
                    _context.Clientes.Remove(usuario.Cliente);
                    usuario.Cliente = null;
                }
                if (usuario.Veterinario != null)
                {
                    await ManejarEliminacionVeterinario(usuario.Veterinario.UsuarioId);
                    usuario.Veterinario = null;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }


        private async Task ManejarEliminacionVeterinario(string veterinarioId)
        {
            var turnos = await _context.Turnos
                .Where(t => t.VeterinarioId == veterinarioId)
                .ToListAsync();

            // Buscar otro veterinario
            var otroVeterinarioId = await _context.Veterinarios
            .Where(v => v.UsuarioId != veterinarioId && v.Usuario.Rol.Nombre == "Veterinario")
            .Select(v => v.UsuarioId)
            .FirstOrDefaultAsync();

            // Si encuentra otro veterinario, asignarle los turnos
            // Si no desvincula a cualquier veterinario del turno y lo cambia a cancelado
            foreach (var turno in turnos)
            {
                if (!string.IsNullOrEmpty(otroVeterinarioId))
                {
                    turno.VeterinarioId = otroVeterinarioId;
                }
                else
                {
                    turno.VeterinarioId = null;
                    turno.EstadoTurno = EstadoTurno.Cancelado;
                }
            }

            var veterinario = await _context.Veterinarios.FindAsync(veterinarioId);
            if (veterinario != null) _context.Veterinarios.Remove(veterinario);

            await _context.SaveChangesAsync();
        }
    }
}

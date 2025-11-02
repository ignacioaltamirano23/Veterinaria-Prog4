using LogicaDeNegocio.Context;
using LogicaDeNegocio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Veterinaria.Controllers
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
            var usuarios = await GetUsuariosQuery().ToListAsync();

            return View(usuarios);
        }

        // GET: Usuarios/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null) return NotFound();

            var usuario = await GetUsuariosQuery().FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // GET: Usuarios/Create
        public async Task<IActionResult> Create()
        {
            ViewData["RolId"] = await GetRolesSelectList();
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Email,Telefono,Direccion,RolId")] Usuario usuario)
        {
            // Quitar validación del rol
            ModelState.Remove("Rol");

            // Validar email único
            if(await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
            {
                ModelState.AddModelError("Email", "Ya existe un usuario con este mail");
            }

            if (!ModelState.IsValid)
            {
                ViewData["RolId"] = await GetRolesSelectList(usuario.RolId);
                return View(usuario);
            }

            try
            {
                // Configurar usuario
                usuario.Id = Guid.NewGuid().ToString();
                usuario.GoogleId = Guid.NewGuid().ToString();

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // Crear entidades según rol
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
            catch (Exception ex)
            {
                ViewBag.MensajeError = "Error al crear el usuario.";
                ViewData["RolId"] = await GetRolesSelectList(usuario.RolId);
                return View(usuario);
            }
        }


        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null) return NotFound();

            var usuario = await GetUsuariosQuery().FirstOrDefaultAsync(u => u.Id == id);

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

            // Validar email único (excluyendo el usuario actual)
            if (await _context.Usuarios.AnyAsync(u => u.Email == usuarioEditado.Email && u.Id != id))
            {
                ModelState.AddModelError("Email", "Ya existe otro usuario con este email.");
            }

            ModelState.Remove("Rol");

            if (!ModelState.IsValid)
            {
                return View(usuarioEditado);
            }

            try
            {
                // Actualizar propiedades
                usuarioOriginal.Nombre = usuarioEditado.Nombre;
                usuarioOriginal.Email = usuarioEditado.Email;
                usuarioOriginal.Telefono = usuarioEditado.Telefono;
                usuarioOriginal.Direccion = usuarioEditado.Direccion;

                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = $"Usuario {usuarioOriginal.Nombre} actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.MensajeError = "Error al actualizar el usuario.";
                return View(usuarioEditado);
            }
        }

        // GET: Usuarios/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null) return NotFound();

            var usuario = await GetUsuariosQuery().FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
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

                // Si es veterinario, manejamos sus turnos
                if (usuario.Veterinario != null)
                {
                    await ManejarEliminacionVeterinario(usuario.Veterinario.UsuarioId);
                }

                // Eliminar entidades relacionadas
                if (usuario.Cliente != null)
                    _context.Clientes.Remove(usuario.Cliente);

                if (usuario.Veterinario != null)
                    _context.Veterinarios.Remove(usuario.Veterinario);

                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = $"Usuario {usuario.Email} eliminado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al eliminar el usuario.";
            }

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
            if (id == IdUsuario)
            {
                TempData["MensajeError"] = "No podés cambiar tu propio rol.";
                return RedirectToAction(nameof(Index));
            }

            try
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

                var rolEntidad = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == nuevoRol);
                if (rolEntidad == null)
                {
                    TempData["MensajeError"] = "Rol inválido.";
                    return RedirectToAction(nameof(AsignarRol), new { id });
                }

                await AsignarRolUsuario(usuario, rolEntidad);

                TempData["MensajeExito"] = $"Rol {rolEntidad.Nombre} asignado correctamente a {usuario.Email}";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al asignar el rol.";
            }

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
            // Solo manejar turnos futuros
            var turnos = await _context.Turnos
                .Where(t => t.VeterinarioId == veterinarioId && t.FechaHora > DateTime.Now)
                .ToListAsync();

            var otroVeterinarioId = await _context.Veterinarios
                .Where(v => v.UsuarioId != veterinarioId && v.Usuario.Rol.Nombre == "Veterinario")
                .Select(v => v.UsuarioId)
                .FirstOrDefaultAsync();

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
            await _context.SaveChangesAsync();
        }

        private IQueryable<Usuario> GetUsuariosQuery()
        {
            return _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Cliente)
                .Include(u => u.Veterinario)
                .OrderBy(u => u.Email);
        }

        private async Task<SelectList> GetRolesSelectList(object selectedValue = null)
        {
            var roles = await _context.Roles
                .OrderBy(r => r.Nombre)
                .ToListAsync();
            return new SelectList(roles, "Id", "Nombre", selectedValue);
        }
    }
}

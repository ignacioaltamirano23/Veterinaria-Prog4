using LogicaDeNegocio.Context;
using LogicaDeNegocio.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Login/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration.GetSection("GoogleKeys:ClientId").Value;
    options.ClientSecret = builder.Configuration.GetSection("GoogleKeys:ClientSecret").Value;

    options.Scope.Add("profile");
    options.ClaimActions.MapJsonKey("picture", "picture");

    options.Events.OnCreatingTicket = async context =>
    {
        var email = context.Identity?.FindFirst(ClaimTypes.Email)?.Value;
        var googleId = context.Identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
            return;

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

        var usuario = await dbContext.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.Cliente)
            .Include(u => u.Veterinario)
            .FirstOrDefaultAsync(u => u.GoogleId == googleId);

        if (usuario == null)
        {
            var clienteRol = await dbContext.Roles.FirstOrDefaultAsync(r => r.Nombre == "Cliente");

            if (clienteRol == null)
            {
                clienteRol = new Rol { Nombre = "Cliente" };
                dbContext.Roles.Add(clienteRol);
                await dbContext.SaveChangesAsync();
            }

            var nombrePorDefecto = email.Split('@')[0];

            usuario = new Usuario
            {
                Id = googleId,
                GoogleId = googleId,
                Email = email,
                Nombre = nombrePorDefecto,
                Telefono = "N/A",
                Direccion = "N/A",
                RolId = clienteRol.Id,
                Cliente = new Cliente()
            };

            dbContext.Usuarios.Add(usuario);
            await dbContext.SaveChangesAsync();
        }

        // Claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol.Nombre)
        };

        if (usuario.Cliente != null)
            claims.Add(new Claim("ClientePerfilId", usuario.Cliente.UsuarioId));

        if (usuario.Veterinario != null)
            claims.Add(new Claim("VeterinarioPerfilId", usuario.Veterinario.UsuarioId));

        var appIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        context.Principal = new ClaimsPrincipal(appIdentity);
    };
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogicaDeNegocio.Context;
using LogicaDeNegocio.Models;
using Microsoft.EntityFrameworkCore;

namespace LogicaDeNegocio.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {            
            context.Database.EnsureCreated();

            // Evitar duplicados
            if (context.Clientes.Any() || context.Mascotas.Any() || context.Turnos.Any())
                return;

            var random = new Random();

            // ------------------- Clientes -------------------
            var nombres = new[]
            {
                "Juan Pérez", "María López", "Carlos González", "Ana Torres", "José Fernández",
                "Laura Martínez", "Pedro Ramírez", "Lucía Sánchez", "Diego Herrera", "Carmen Díaz",
                "Martín Castro", "Sofía Romero", "Andrés Vega", "Valentina Morales", "Felipe Navarro"
            };

            var clientes = nombres.Select((nombre, i) => new Cliente
            {
                Nombre = nombre,
                Telefono = $"123-456-{i + 1:000}",
                Direccion = $"Calle {(i + 1) * 10}",
                Mascotas = new HashSet<Mascota>()
            }).ToList();

            context.Clientes.AddRange(clientes);
            context.SaveChanges();

            // ------------------- Mascotas -------------------
            var especies = new[] { "Perro", "Gato", "Ave", "Conejo", "Pez" };
            var razas = new[] { "Mestizo", "Labrador", "Persa", "Canario", "Angora" };

            var mascotas = new List<Mascota>();

            for (int i = 1; i <= 15; i++)
            {
                var cliente = clientes[random.Next(clientes.Count)];

                var mascota = new Mascota
                {
                    Nombre = $"Mascota{i}",
                    Especie = especies[random.Next(especies.Length)],
                    Raza = razas[random.Next(razas.Length)],
                    FechaNacimiento = DateTime.Now.AddYears(-random.Next(1, 10)),
                    ClienteId = cliente.Id,
                    Turnos = new HashSet<Turno>()
                };

                mascotas.Add(mascota);
                cliente.Mascotas?.Add(mascota); 
            }

            context.Mascotas.AddRange(mascotas);
            context.SaveChanges();

            // ------------------- Turnos -------------------
            var estados = Enum.GetValues(typeof(EstadoTurno)).Cast<EstadoTurno>().ToArray();
            var turnos = new List<Turno>();

            foreach (var mascota in mascotas)
            {
                int cantidadTurnos = random.Next(2, 4); // 2 o 3 turnos por mascota

                for (int j = 0; j < cantidadTurnos; j++)
                {
                    var turno = new Turno
                    {
                        FechaHora = DateTime.Now.AddDays(random.Next(1, 30))
                                               .AddHours(random.Next(8, 20)),
                        EstadoTurno = estados[random.Next(estados.Length)],
                        MascotaId = mascota.Id
                    };

                    turnos.Add(turno);
                    mascota.Turnos?.Add(turno); 
                }
            }

            context.Turnos.AddRange(turnos);
            context.SaveChanges();
        }
    }
}

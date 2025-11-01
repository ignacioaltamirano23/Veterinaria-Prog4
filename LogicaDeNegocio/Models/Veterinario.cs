using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicaDeNegocio.Models
{
    public class Veterinario
    {
        [Key]
        public string UsuarioId { get; set; } = string.Empty;
        public Usuario Usuario { get; set; } = default!;

        public ICollection<Turno>? Turnos { get; set; } = new List<Turno>();  
    }
}

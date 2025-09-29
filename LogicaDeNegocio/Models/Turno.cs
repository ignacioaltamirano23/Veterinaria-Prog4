using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicaDeNegocio.Models
{
    public class Turno
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "La fecha es obligatoria")]
        [Display(Name = "Fecha")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime FechaHora { get; set; }

        [Display(Name = "Estado")]
        public EstadoTurno EstadoTurno { get; set; } = EstadoTurno.Pendiente;

        [Required(ErrorMessage = "Debe seleccionar una mascota")]
        [Display(Name = "Mascota")]        
        public int? MascotaId { get; set; }

        [ForeignKey("MascotaId")]
        public Mascota? Mascota { get; set; }
    }
}

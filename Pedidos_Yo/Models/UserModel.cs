using System.ComponentModel.DataAnnotations;

namespace Pedidos_Yo.Models
{
    public class UserModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        [StringLength(150, ErrorMessage = "El correo electrónico no puede exceder los 150 caracteres.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, ErrorMessage = "La contraseña no puede exceder los 100 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio.")]
        [RegularExpression("Admin|Cliente|Empleado", ErrorMessage = "El rol debe ser Admin, Cliente o Empleado.")]
        public string Rol { get; set; }

        // Relación: Un usuario tiene muchos pedidos
        public ICollection<OrderModel> Orders { get; set; }
    }
}

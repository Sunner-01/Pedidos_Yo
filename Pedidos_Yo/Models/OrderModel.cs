using System.ComponentModel.DataAnnotations;

namespace Pedidos_Yo.Models
{
    public class OrderModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El ID del cliente es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del cliente debe ser mayor a cero.")]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "La fecha del pedido es obligatoria.")]
        [DataType(DataType.DateTime, ErrorMessage = "La fecha debe ser válida.")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El estado es obligatorio.")]
        [StringLength(20, ErrorMessage = "El estado no puede exceder los 20 caracteres.")]
        [RegularExpression("Pendiente|Procesado|Enviado|Entregado", ErrorMessage = "El estado debe ser Pendiente, Procesado, Enviado o Entregado.")]
        public string Estado { get; set; } = "Pendiente";

        [Required(ErrorMessage = "El total es obligatorio.")]
        [Range(0.01, 9999999.99, ErrorMessage = "El total debe ser mayor a cero y razonable.")]
        public decimal Total { get; set; }

        // Relaciones
        public UserModel Cliente { get; set; }
        public ICollection<OrderItemModel> Items { get; set; } = new List<OrderItemModel>();
    }

}

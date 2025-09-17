using System.ComponentModel.DataAnnotations;

namespace Pedidos_Yo.Models
{
    public class OrderItemModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El ID del pedido es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del pedido debe ser mayor a cero.")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "El ID del producto es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del producto debe ser mayor a cero.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "El subtotal es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El subtotal debe ser mayor a cero.")]
        public decimal Subtotal { get; set; }

        // Relaciones
        public OrderModel Order { get; set; }
        public ProductModel Product { get; set; }
    }
}

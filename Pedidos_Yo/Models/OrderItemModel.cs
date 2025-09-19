using System.ComponentModel.DataAnnotations;

namespace Pedidos_Yo.Models
{
    public class OrderItemModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El ID del pedido es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del pedido debe ser válido.")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "El ID del producto es obligatorio.")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del producto debe ser válido.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre 1 y 1000.")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "El subtotal es obligatorio.")]
        [Range(0.01, 9999999.99, ErrorMessage = "El subtotal debe ser mayor a cero.")]
        public decimal Subtotal { get; set; }

        // Relaciones
        public OrderModel Order { get; set; }
        public ProductModel Product { get; set; }
    }

}

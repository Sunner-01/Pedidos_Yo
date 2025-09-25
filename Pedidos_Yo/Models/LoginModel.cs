using System.ComponentModel.DataAnnotations;

namespace Pedidos_Yo.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "El correo electr�nico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo electr�nico no tiene un formato v�lido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contrase�a es obligatoria.")]
        public string Password { get; set; }
    }
}
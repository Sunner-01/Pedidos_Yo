using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos_Yo.Data;
using System.Threading.Tasks;

namespace Pedidos_Yo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly Pedidos_YoDBContext _context;

        public AdminController(Pedidos_YoDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Conteos generales
            ViewBag.NumUsuarios = await _context.Users.CountAsync();
            ViewBag.NumProductos = await _context.Products.CountAsync();
            ViewBag.NumPedidos = await _context.Orders.CountAsync();

            // Pedidos por estado
            ViewBag.PedidosPendientes = await _context.Orders.Where(o => o.Estado == "Pendiente").CountAsync();
            ViewBag.PedidosProcesados = await _context.Orders.Where(o => o.Estado == "Procesado").CountAsync();
            ViewBag.PedidosEnviados = await _context.Orders.Where(o => o.Estado == "Enviado").CountAsync();
            ViewBag.PedidosEntregados = await _context.Orders.Where(o => o.Estado == "Entregado").CountAsync();

            // Productos con bajo stock (stock < 10)
            ViewBag.ProductosBajoStock = await _context.Products.Where(p => p.Stock < 10).CountAsync();

            return View();
        }

        // Acción para pedidos pendientes 
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PedidosPendientes()
        {
            var pedidos = await _context.Orders
                .Include(o => o.Cliente)
                .Where(o => o.Estado == "Pendiente")
                .ToListAsync();
            return View(pedidos);
        }
    }
}
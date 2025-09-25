using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos_Yo.Data;
using Pedidos_Yo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Pedidos_Yo.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class ClientController : Controller
    {
        private readonly Pedidos_YoDBContext _context;

        public ClientController(Pedidos_YoDBContext context)
        {
            _context = context;
        }

        // Index: Muestra productos con filtros y paginación
        public async Task<IActionResult> Index(string searchName = null, decimal? minPrice = null, decimal? maxPrice = null, int page = 1, int itemsPerPage = 10)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(p => p.Nombre.Contains(searchName));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Precio >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Precio <= maxPrice.Value);
            }

            var totalItems = await query.CountAsync();
            var products = await query
                .OrderBy(p => p.Nombre)
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)itemsPerPage);
            ViewBag.ItemsPerPage = itemsPerPage;
            ViewBag.SearchName = searchName;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(products);
        }

        // Orders: Lista pedidos del cliente con paginación
        public async Task<IActionResult> Orders(int page = 1, int itemsPerPage = 10)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var query = _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Where(o => o.ClienteId == userId);

            var totalItems = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.Fecha)
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)itemsPerPage);
            ViewBag.ItemsPerPage = itemsPerPage;

            return View(orders);
        }

        // CreateOrder: Formulario para nuevo pedido
        [HttpGet]
        public IActionResult CreateOrder()
        {
            ViewData["Productos"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Products, "Id", "Nombre");
            var model = new OrderModel { Items = new List<OrderItemModel> { new OrderItemModel() } };
            return View(model);
        }

        // CreateOrder POST: Crear pedido
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(OrderModel orderModel, int ItemProductId, int ItemCantidad)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            orderModel.ClienteId = userId;

            if (ItemProductId <= 0 || ItemCantidad <= 0)
            {
                ModelState.AddModelError("", "Selecciona un producto y cantidad válida.");
                ViewData["Productos"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Products, "Id", "Nombre");
                orderModel.Items = new List<OrderItemModel> { new OrderItemModel { ProductId = ItemProductId, Cantidad = ItemCantidad } };
                return View(orderModel);
            }

            var product = await _context.Products.FindAsync(ItemProductId);
            if (product == null)
            {
                ModelState.AddModelError("", "Producto no encontrado.");
                ViewData["Productos"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Products, "Id", "Nombre");
                orderModel.Items = new List<OrderItemModel> { new OrderItemModel { ProductId = ItemProductId, Cantidad = ItemCantidad } };
                return View(orderModel);
            }

            if (product.Stock < ItemCantidad)
            {
                ModelState.AddModelError("", $"Stock insuficiente para {product.Nombre}. Disponible: {product.Stock}");
                ViewData["Productos"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Products, "Id", "Nombre");
                orderModel.Items = new List<OrderItemModel> { new OrderItemModel { ProductId = ItemProductId, Cantidad = ItemCantidad } };
                return View(orderModel);
            }

            var item = new OrderItemModel
            {
                ProductId = ItemProductId,
                Cantidad = ItemCantidad,
                Subtotal = ItemCantidad * product.Precio
            };

            orderModel.Items = new List<OrderItemModel> { item };
            orderModel.Total = item.Subtotal;
            orderModel.Fecha = DateTime.Now;
            orderModel.Estado = "Pendiente";

            product.Stock -= ItemCantidad;

            _context.Orders.Add(orderModel);
            await _context.SaveChangesAsync();

            return RedirectToAction("Orders");
        }

        // EditOrder: Formulario para editar pedido (solo Pendiente)
        [HttpGet]
        public async Task<IActionResult> EditOrder(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.ClienteId == userId && o.Estado == "Pendiente");

            if (order == null)
            {
                return NotFound();
            }

            ViewData["Productos"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Products, "Id", "Nombre");
            return View(order);
        }

        // EditOrder POST: Actualizar pedido
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOrder(int id, OrderModel orderModel, int ItemProductId, int ItemCantidad)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var existingOrder = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.ClienteId == userId && o.Estado == "Pendiente");

            if (existingOrder == null)
            {
                return NotFound();
            }

            if (ItemProductId <= 0 || ItemCantidad <= 0)
            {
                ModelState.AddModelError("", "Selecciona un producto y cantidad válida.");
                ViewData["Productos"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Products, "Id", "Nombre");
                orderModel.Items = new List<OrderItemModel> { new OrderItemModel { ProductId = ItemProductId, Cantidad = ItemCantidad } };
                return View(orderModel);
            }

            var product = await _context.Products.FindAsync(ItemProductId);
            if (product == null)
            {
                ModelState.AddModelError("", "Producto no encontrado.");
                ViewData["Productos"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Products, "Id", "Nombre");
                orderModel.Items = new List<OrderItemModel> { new OrderItemModel { ProductId = ItemProductId, Cantidad = ItemCantidad } };
                return View(orderModel);
            }

            var existingItem = existingOrder.Items.FirstOrDefault();
            int originalCantidad = existingItem?.Cantidad ?? 0;
            var originalProductId = existingItem?.ProductId ?? 0;

            if ((product.Stock + originalCantidad) < ItemCantidad)
            {
                ModelState.AddModelError("", $"Stock insuficiente para {product.Nombre}. Disponible: {product.Stock + originalCantidad}");
                ViewData["Productos"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Products, "Id", "Nombre");
                orderModel.Items = new List<OrderItemModel> { new OrderItemModel { ProductId = ItemProductId, Cantidad = ItemCantidad } };
                return View(orderModel);
            }

            if (existingItem != null)
            {
                var originalProduct = await _context.Products.FindAsync(originalProductId);
                if (originalProduct != null)
                {
                    originalProduct.Stock += originalCantidad;
                }
                _context.OrderItems.Remove(existingItem);
            }

            var newItem = new OrderItemModel
            {
                OrderId = id,
                ProductId = ItemProductId,
                Cantidad = ItemCantidad,
                Subtotal = ItemCantidad * product.Precio
            };

            existingOrder.Items = new List<OrderItemModel> { newItem };
            existingOrder.Total = newItem.Subtotal;
            existingOrder.Fecha = DateTime.Now;

            product.Stock -= ItemCantidad;

            _context.Update(existingOrder);
            await _context.SaveChangesAsync();

            return RedirectToAction("Orders");
        }

        // CancelOrder: Eliminar pedido (solo Pendiente)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.ClienteId == userId && o.Estado == "Pendiente");

            if (order == null)
            {
                return NotFound();
            }

            foreach (var item in order.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock += item.Cantidad;
                }
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return RedirectToAction("Orders");
        }
    }
}
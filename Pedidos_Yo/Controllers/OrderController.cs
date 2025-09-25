using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pedidos_Yo.Data;
using Pedidos_Yo.Models;

namespace Pedidos_Yo.Controllers
{
    [Authorize(Roles = "Admin,Cliente,Empleado")]
    public class OrderController : Controller
    {
        private readonly Pedidos_YoDBContext _context;

        public OrderController(Pedidos_YoDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1, int itemsPerPage = 10)
        {
            var query = _context.Orders
                .Include(o => o.Cliente)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .AsQueryable();

            if (User.IsInRole("Cliente"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                query = query.Where(o => o.ClienteId == userId);
            }

            var totalItems = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.Fecha)
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)itemsPerPage);
            ViewBag.ItemsPerPage = itemsPerPage;

            if (User.IsInRole("Cliente"))
            {
                ViewData["Layout"] = "~/Views/Shared/_LayoutCliente.cshtml";
            }

            return View(orders);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderModel = await _context.Orders
                .Include(o => o.Cliente)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (orderModel == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Cliente"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (orderModel.ClienteId != userId)
                {
                    return NotFound();
                }
                ViewData["Layout"] = "~/Views/Shared/_LayoutCliente.cshtml";
            }

            return View(orderModel);
        }

        public IActionResult Create()
        {
            ViewData["Productos"] = new SelectList(_context.Products, "Id", "Nombre");
            if (User.IsInRole("Cliente"))
            {
                ViewData["Layout"] = "~/Views/Shared/_LayoutCliente.cshtml";
            }
            else
            {
                ViewData["ClienteId"] = new SelectList(_context.Users.Where(u => u.Rol == "Cliente"), "Id", "Email");
            }
            return View(new OrderModel { Items = new List<OrderItemModel> { new OrderItemModel() } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderModel orderModel, int ItemProductId, int ItemCantidad)
        {
            if (User.IsInRole("Cliente"))
            {
                orderModel.ClienteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            }

            try
            {
                if (!ModelState.IsValid || ItemProductId <= 0 || ItemCantidad <= 0)
                {
                    ModelState.AddModelError("", "Debe seleccionar un producto y especificar una cantidad válida.");
                    ViewData["Productos"] = new SelectList(_context.Products, "Id", "Nombre");
                    if (!User.IsInRole("Cliente"))
                    {
                        ViewData["ClienteId"] = new SelectList(_context.Users.Where(u => u.Rol == "Cliente"), "Id", "Email", orderModel.ClienteId);
                    }
                    orderModel.Items = new List<OrderItemModel> { new OrderItemModel { ProductId = ItemProductId, Cantidad = ItemCantidad } };
                    return View(orderModel);
                }

                var product = await _context.Products.FindAsync(ItemProductId);
                if (product == null)
                {
                    ModelState.AddModelError("", $"Producto con ID {ItemProductId} no encontrado.");
                    ViewData["Productos"] = new SelectList(_context.Products, "Id", "Nombre");
                    if (!User.IsInRole("Cliente"))
                    {
                        ViewData["ClienteId"] = new SelectList(_context.Users.Where(u => u.Rol == "Cliente"), "Id", "Email", orderModel.ClienteId);
                    }
                    orderModel.Items = new List<OrderItemModel> { new OrderItemModel { ProductId = ItemProductId, Cantidad = ItemCantidad } };
                    return View(orderModel);
                }
                if (product.Stock < ItemCantidad)
                {
                    ModelState.AddModelError("", $"Stock insuficiente para {product.Nombre}. Disponible: {product.Stock}");
                    ViewData["Productos"] = new SelectList(_context.Products, "Id", "Nombre");
                    if (!User.IsInRole("Cliente"))
                    {
                        ViewData["ClienteId"] = new SelectList(_context.Users.Where(u => u.Rol == "Cliente"), "Id", "Email", orderModel.ClienteId);
                    }
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
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al crear el pedido: " + ex.Message);
                ViewData["Productos"] = new SelectList(_context.Products, "Id", "Nombre");
                if (!User.IsInRole("Cliente"))
                {
                    ViewData["ClienteId"] = new SelectList(_context.Users.Where(u => u.Rol == "Cliente"), "Id", "Email", orderModel.ClienteId);
                }
                orderModel.Items = new List<OrderItemModel> { new OrderItemModel { ProductId = ItemProductId, Cantidad = ItemCantidad } };
                return View(orderModel);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderModel = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (orderModel == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Cliente"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (orderModel.ClienteId != userId || orderModel.Estado != "Pendiente")
                {
                    return NotFound();
                }
                ViewData["Layout"] = "~/Views/Shared/_LayoutCliente.cshtml";
            }

            ViewData["Productos"] = new SelectList(_context.Products, "Id", "Nombre", orderModel.Items.FirstOrDefault()?.ProductId);
            if (!User.IsInRole("Cliente"))
            {
                ViewData["ClienteId"] = new SelectList(_context.Users.Where(u => u.Rol == "Cliente"), "Id", "Email", orderModel.ClienteId);
            }
            return View(orderModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrderModel orderModel, int ItemProductId, int ItemCantidad)
        {
            if (id != orderModel.Id)
            {
                return NotFound();
            }

            if (User.IsInRole("Cliente"))
            {
                orderModel.ClienteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            }

            try
            {
                if (!ModelState.IsValid || ItemProductId <= 0 || ItemCantidad <= 0)
                {
                    ModelState.AddModelError("", "Debe seleccionar un producto y especificar una cantidad válida.");
                    ViewData["Productos"] = new SelectList(_context.Products, "Id", "Nombre");
                    if (!User.IsInRole("Cliente"))
                    {
                        ViewData["ClienteId"] = new SelectList(_context.Users.Where(u => u.Rol == "Cliente"), "Id", "Email", orderModel.ClienteId);
                    }
                    orderModel.Items = new List<OrderItemModel> { new OrderItemModel { ProductId = ItemProductId, Cantidad = ItemCantidad } };
                    return View(orderModel);
                }

                var product = await _context.Products.FindAsync(ItemProductId);
                if (product == null)
                {
                    ModelState.AddModelError("", $"Producto con ID {ItemProductId} no encontrado.");
                    ViewData["Productos"] = new SelectList(_context.Products, "Id", "Nombre");
                    if (!User.IsInRole("Cliente"))
                    {
                        ViewData["ClienteId"] = new SelectList(_context.Users.Where(u => u.Rol == "Cliente"), "Id", "Email", orderModel.ClienteId);
                    }
                    orderModel.Items = new List<OrderItemModel> { new OrderItemModel { ProductId = ItemProductId, Cantidad = ItemCantidad } };
                    return View(orderModel);
                }

                var existingOrder = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);
                if (existingOrder == null)
                {
                    return NotFound();
                }

                if (User.IsInRole("Cliente") && (existingOrder.ClienteId != orderModel.ClienteId || existingOrder.Estado != "Pendiente"))
                {
                    return NotFound();
                }

                var originalItem = existingOrder.Items.FirstOrDefault();
                int originalCantidad = originalItem?.Cantidad ?? 0;
                var originalProduct = originalItem != null ? await _context.Products.FindAsync(originalItem.ProductId) : null;
                if (originalProduct != null)
                {
                    originalProduct.Stock += originalCantidad;
                }

                if (product.Stock < ItemCantidad)
                {
                    ModelState.AddModelError("", $"Stock insuficiente para {product.Nombre}. Disponible: {product.Stock}");
                    ViewData["Productos"] = new SelectList(_context.Products, "Id", "Nombre");
                    if (!User.IsInRole("Cliente"))
                    {
                        ViewData["ClienteId"] = new SelectList(_context.Users.Where(u => u.Rol == "Cliente"), "Id", "Email", orderModel.ClienteId);
                    }
                    orderModel.Items = new List<OrderItemModel> { new OrderItemModel { ProductId = ItemProductId, Cantidad = ItemCantidad } };
                    return View(orderModel);
                }

                if (originalItem != null)
                {
                    _context.OrderItems.Remove(originalItem);
                }

                orderModel.Items = new List<OrderItemModel>
                {
                    new OrderItemModel
                    {
                        ProductId = ItemProductId,
                        Cantidad = ItemCantidad,
                        Subtotal = ItemCantidad * product.Precio
                    }
                };
                orderModel.Total = orderModel.Items.Sum(i => i.Subtotal);

                product.Stock -= ItemCantidad;

                _context.Update(orderModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al editar el pedido: " + ex.Message);
                ViewData["Productos"] = new SelectList(_context.Products, "Id", "Nombre");
                if (!User.IsInRole("Cliente"))
                {
                    ViewData["ClienteId"] = new SelectList(_context.Users.Where(u => u.Rol == "Cliente"), "Id", "Email", orderModel.ClienteId);
                }
                orderModel.Items = new List<OrderItemModel> { new OrderItemModel { ProductId = ItemProductId, Cantidad = ItemCantidad } };
                return View(orderModel);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderModel = await _context.Orders
                .Include(o => o.Cliente)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (orderModel == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Cliente"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (orderModel.ClienteId != userId || orderModel.Estado != "Pendiente")
                {
                    return NotFound();
                }
                ViewData["Layout"] = "~/Views/Shared/_LayoutCliente.cshtml";
            }

            return View(orderModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orderModel = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (orderModel == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Cliente"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (orderModel.ClienteId != userId || orderModel.Estado != "Pendiente")
                {
                    return NotFound();
                }
            }

            foreach (var item in orderModel.Items)
            {
                var product = item.Product;
                product.Stock += item.Cantidad;
            }

            _context.Orders.Remove(orderModel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pedidos_Yo.Data;
using Pedidos_Yo.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Pedidos_Yo.Controllers
{
    [Authorize(Roles = "Admin,Cliente,Empleado")]
    public class ProductController : Controller
    {
        private readonly Pedidos_YoDBContext _context;

        public ProductController(Pedidos_YoDBContext context)
        {
            _context = context;
        }

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

            if (User.IsInRole("Cliente"))
            {
                ViewData["Layout"] = "~/Views/Shared/_LayoutCliente.cshtml";
            }

            return View(products);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productModel = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (productModel == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Cliente"))
            {
                ViewData["Layout"] = "~/Views/Shared/_LayoutCliente.cshtml";
            }

            return View(productModel);
        }

        [Authorize(Roles = "Admin,Empleado")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin,Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Descripcion,Precio,Stock")] ProductModel productModel)
        {
            if (ModelState.IsValid)
            {
                _context.Add(productModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(productModel);
        }

        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productModel = await _context.Products.FindAsync(id);
            if (productModel == null)
            {
                return NotFound();
            }
            return View(productModel);
        }

        [Authorize(Roles = "Admin,Empleado")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,Precio,Stock")] ProductModel productModel)
        {
            if (id != productModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(productModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductModelExists(productModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(productModel);
        }

        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productModel = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (productModel == null)
            {
                return NotFound();
            }

            return View(productModel);
        }

        [Authorize(Roles = "Admin,Empleado")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productModel = await _context.Products.FindAsync(id);
            if (productModel != null)
            {
                _context.Products.Remove(productModel);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductModelExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
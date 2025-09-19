using Microsoft.EntityFrameworkCore;
using Pedidos_Yo.Models;
using System.Collections.Generic;
using System.Reflection.Emit;
using Pedidos_Yo.Models;

namespace Pedidos_Yo.Data
{
    public class Pedidos_YoDBContext : DbContext
    {
        public Pedidos_YoDBContext(DbContextOptions<Pedidos_YoDBContext> options) : base(options) { }

        public DbSet<UserModel> Users { get; set; }
        public DbSet<ProductModel> Products { get; set; }
        public DbSet<OrderModel> Orders { get; set; }
        public DbSet<OrderItemModel> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Relación: OrderModel -> UserModel (un usuario tiene muchos pedidos)
            modelBuilder.Entity<OrderModel>()
                .HasOne(o => o.Cliente)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación: OrderItemModel -> OrderModel (un pedido tiene muchos ítems)
            modelBuilder.Entity<OrderItemModel>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación: OrderItemModel -> ProductModel (un producto tiene muchos ítems de pedido)
            modelBuilder.Entity<OrderItemModel>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuración de precisión y escala para propiedades decimal
            modelBuilder.Entity<ProductModel>()
                .Property(p => p.Precio)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderModel>()
                .Property(o => o.Total)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItemModel>()
                .Property(oi => oi.Subtotal)
                .HasColumnType("decimal(18,2)");
        }
    }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using costbenefi.Models;

namespace costbenefi.Data
{
    public class AppDbContext : DbContext
    {
        // ===== DbSets EXISTENTES =====
        public DbSet<RawMaterial> RawMaterials { get; set; }
        public DbSet<Movimiento> Movimientos { get; set; }

        // ========== ✅ NUEVOS DbSets PARA POS ==========
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetalleVentas { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=costbenefi.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== CONFIGURACIÓN EXISTENTE PARA RawMaterial =====
            modelBuilder.Entity<RawMaterial>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NombreArticulo)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Categoria)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.UnidadMedida)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.UnidadBase)
                    .HasMaxLength(50);

                entity.Property(e => e.FactorConversion)
                    .HasColumnType("decimal(18,6)")
                    .HasDefaultValue(1);

                entity.Property(e => e.Proveedor)
                    .HasMaxLength(200);

                entity.Property(e => e.Observaciones)
                    .HasMaxLength(500);

                entity.Property(e => e.CodigoBarras)
                    .HasMaxLength(100);

                entity.Property(e => e.PrecioConIVA)
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.PrecioSinIVA)
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.PrecioBaseConIVA)
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.PrecioBaseSinIVA)
                    .HasColumnType("decimal(18,4)");

                // ========== ✅ NUEVAS CONFIGURACIONES PARA CAMPOS POS ==========
                entity.Property(e => e.PrecioVenta)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.PrecioVentaConIVA)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.ActivoParaVenta)
                    .HasDefaultValue(true);

                entity.Property(e => e.StockMinimoVenta)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(1);

                entity.Property(e => e.MargenObjetivo)
                    .HasColumnType("decimal(5,2)")
                    .HasDefaultValue(30);

                entity.Property(e => e.PrecioDescuento)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.PorcentajeDescuento)
                    .HasColumnType("decimal(5,2)")
                    .HasDefaultValue(0);

                entity.Property(e => e.DiasParaDescuento)
                    .HasDefaultValue(7);

                // ===== CONFIGURACIÓN EXISTENTE PARA ELIMINACIÓN LÓGICA =====
                entity.Property(e => e.Eliminado)
                    .HasDefaultValue(false);

                entity.Property(e => e.UsuarioEliminacion)
                    .HasMaxLength(100);

                entity.Property(e => e.MotivoEliminacion)
                    .HasMaxLength(500);

                // ===== FILTRO GLOBAL EXISTENTE: OCULTAR ELIMINADOS AUTOMÁTICAMENTE =====
                entity.HasQueryFilter(e => !e.Eliminado);

                // ===== ÍNDICES EXISTENTES =====
                entity.HasIndex(e => e.NombreArticulo);
                entity.HasIndex(e => e.Categoria);
                entity.HasIndex(e => e.Proveedor);
                entity.HasIndex(e => e.CodigoBarras);
                entity.HasIndex(e => e.UnidadMedida);
                entity.HasIndex(e => e.Eliminado);
                entity.HasIndex(e => e.FechaEliminacion);
                entity.HasIndex(e => e.UsuarioEliminacion);

                // ========== ✅ NUEVOS ÍNDICES PARA POS ==========
                entity.HasIndex(e => e.ActivoParaVenta);
                entity.HasIndex(e => e.FechaVencimiento);
                entity.HasIndex(e => e.PrecioVenta);
            });

            // ===== CONFIGURACIÓN EXISTENTE PARA Movimiento =====
            modelBuilder.Entity<Movimiento>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.TipoMovimiento)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Motivo)
                    .HasMaxLength(500);

                entity.Property(e => e.Usuario)
                    .HasMaxLength(100);

                entity.Property(e => e.Cantidad)
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.PrecioConIVA)
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.PrecioSinIVA)
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.UnidadMedida)
                    .HasMaxLength(50);

                // ===== CONFIGURACIÓN EXISTENTE DE RELACIÓN =====
                entity.HasOne(e => e.RawMaterial)
                    .WithMany()
                    .HasForeignKey(e => e.RawMaterialId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                entity.HasIndex(e => e.RawMaterialId);
                entity.HasIndex(e => e.TipoMovimiento);
                entity.HasIndex(e => e.FechaMovimiento);
            });

            // ========== ✅ NUEVA CONFIGURACIÓN PARA Venta ==========
            modelBuilder.Entity<Venta>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Cliente)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Usuario)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.FormaPago)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Estado)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Observaciones)
                    .HasMaxLength(500);

                entity.Property(e => e.SubTotal)
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.IVA)
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.Total)
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.Descuento)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.FechaVenta)
                    .HasDefaultValueSql("datetime('now')");

                // Índices para Venta
                entity.HasIndex(e => e.NumeroTicket).IsUnique();
                entity.HasIndex(e => e.FechaVenta);
                entity.HasIndex(e => e.Usuario);
                entity.HasIndex(e => e.Estado);
            });

            // ========== ✅ NUEVA CONFIGURACIÓN PARA DetalleVenta ==========
            modelBuilder.Entity<DetalleVenta>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NombreProducto)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.UnidadMedida)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.Cantidad)
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.PrecioUnitario)
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.SubTotal)
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.CostoUnitario)
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.PorcentajeIVA)
                    .HasColumnType("decimal(5,2)")
                    .HasDefaultValue(16.0m);

                entity.Property(e => e.DescuentoAplicado)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                // Relaciones para DetalleVenta
                entity.HasOne(e => e.Venta)
                      .WithMany(v => v.DetallesVenta)
                      .HasForeignKey(e => e.VentaId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RawMaterial)
                      .WithMany()
                      .HasForeignKey(e => e.RawMaterialId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Índices para DetalleVenta
                entity.HasIndex(e => e.VentaId);
                entity.HasIndex(e => e.RawMaterialId);
            });

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Datos iniciales si es necesario
        }

        // ===== MÉTODOS EXISTENTES PARA ACCESO A DATOS ELIMINADOS =====
        public IQueryable<RawMaterial> GetAllRawMaterialsIncludingDeleted()
        {
            return RawMaterials.IgnoreQueryFilters();
        }

        public IQueryable<RawMaterial> GetDeletedRawMaterials()
        {
            return RawMaterials.IgnoreQueryFilters().Where(m => m.Eliminado);
        }

        public async Task<RawMaterial?> FindRawMaterialIncludingDeletedAsync(int id)
        {
            return await RawMaterials.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == id);
        }

        public IQueryable<Movimiento> GetMovementsOfDeletedProducts()
        {
            return Movimientos
                .Include(m => m.RawMaterial)
                .Where(m => RawMaterials.IgnoreQueryFilters().Any(r => r.Id == m.RawMaterialId && r.Eliminado));
        }

        // ========== ✅ NUEVOS MÉTODOS PARA POS ==========

        /// <summary>
        /// Obtiene productos disponibles para venta en POS
        /// </summary>
        public IQueryable<RawMaterial> GetProductosDisponiblesParaVenta()
        {
            return RawMaterials.Where(p => p.ActivoParaVenta && (p.StockAntiguo + p.StockNuevo) > 0);
        }

        /// <summary>
        /// Obtiene productos próximos a vencer
        /// </summary>
        public IQueryable<RawMaterial> GetProductosProximosAVencer()
        {
            var fechaLimite = DateTime.Now.AddDays(7);
            return RawMaterials.Where(p =>
                p.FechaVencimiento.HasValue &&
                p.FechaVencimiento <= fechaLimite &&
               (p.StockAntiguo + p.StockNuevo) > 0);
        }

        /// <summary>
        /// Obtiene productos con descuento activo
        /// </summary>
        public IQueryable<RawMaterial> GetProductosConDescuento()
        {
            return RawMaterials.Where(p =>
                p.PrecioDescuento > 0 &&
                p.PorcentajeDescuento > 0 &&
                  (p.StockAntiguo + p.StockNuevo) > 0);
        }

        /// <summary>
        /// Obtiene ventas de un día específico
        /// </summary>
        public IQueryable<Venta> GetVentasDelDia(DateTime fecha)
        {
            var inicioDia = fecha.Date;
            var finDia = inicioDia.AddDays(1);

            return Ventas.Where(v =>
                v.FechaVenta >= inicioDia &&
                v.FechaVenta < finDia &&
                v.Estado == "Completada");
        }

        /// <summary>
        /// Obtiene el total de ventas de un día
        /// </summary>
        public async Task<decimal> GetTotalVentasDelDiaAsync(DateTime fecha)
        {
            var ventasDelDia = GetVentasDelDia(fecha);
            return await ventasDelDia.SumAsync(v => v.Total);
        }

        /// <summary>
        /// Obtiene productos más vendidos en un rango de fechas
        /// </summary>
        public IQueryable<RawMaterial> GetProductosMasVendidos(DateTime desde, DateTime hasta, int top = 10)
        {
            var ventasEnRango = Ventas.Where(v =>
                v.FechaVenta >= desde &&
                v.FechaVenta <= hasta &&
                v.Estado == "Completada");

            var productosVendidos = DetalleVentas
                .Where(d => ventasEnRango.Any(v => v.Id == d.VentaId))
                .GroupBy(d => d.RawMaterialId)
                .OrderByDescending(g => g.Sum(d => d.Cantidad))
                .Take(top)
                .Select(g => g.Key);

            return RawMaterials.Where(p => productosVendidos.Contains(p.Id));
        }

        /// <summary>
        /// Configura un producto para venta por primera vez
        /// </summary>
        public void ConfigurarProductoParaVenta(RawMaterial producto, decimal margenDeseado = 30)
        {
            producto.ConfigurarParaVenta(margenDeseado);
            SaveChanges();
        }

        /// <summary>
        /// Obtiene estadísticas rápidas para POS
        /// </summary>
        public async Task<dynamic> GetEstadisticasPOSAsync()
        {
            var hoy = DateTime.Today;
            var ventasHoy = await GetTotalVentasDelDiaAsync(hoy);
            var ticketsHoy = await GetVentasDelDia(hoy).CountAsync();
            var productosActivos = await GetProductosDisponiblesParaVenta().CountAsync();
            var productosStockBajo = await RawMaterials.CountAsync(p =>
                p.AlertaStockBajo > 0 && (p.StockAntiguo + p.StockNuevo) <= p.AlertaStockBajo);

            return new
            {
                VentasHoy = ventasHoy,
                TicketsHoy = ticketsHoy,
                ProductosActivos = productosActivos,
                ProductosStockBajo = productosStockBajo
            };
        }

        // ===== MÉTODOS EXISTENTES DE SaveChanges =====
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var materialEntries = ChangeTracker.Entries<RawMaterial>();
            foreach (var entry in materialEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.FechaCreacion = DateTime.Now;
                    entry.Entity.FechaActualizacion = DateTime.Now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.FechaActualizacion = DateTime.Now;
                }
            }

            var movimientoEntries = ChangeTracker.Entries<Movimiento>();
            foreach (var entry in movimientoEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.FechaMovimiento = DateTime.Now;
                }
            }

            // ========== ✅ NUEVO: Timestamp para entidades POS ==========
            var ventaEntries = ChangeTracker.Entries<Venta>();
            foreach (var entry in ventaEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.FechaVenta = DateTime.Now;
                   
if (entry.Entity.NumeroTicket == 0)
{
    var fecha = DateTime.Now;
    // Formato más corto: MMddHHmm (máximo 12311159 = 8 dígitos)
    var numeroTicket = int.Parse($"{fecha:MMdd}{fecha:HH}{fecha:mm}");
    entry.Entity.NumeroTicket = numeroTicket;
}
                }
            }
        }
    }
}
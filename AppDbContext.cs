using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using costbenefi.Models;
using System.Collections.Generic;

namespace costbenefi.Data
{
    public class AppDbContext : DbContext
    {
        // ===== DbSets EXISTENTES =====
        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<RawMaterial> RawMaterials { get; set; }
        public DbSet<Movimiento> Movimientos { get; set; }

        // ========== ✅ DbSets PARA POS ==========
        public DbSet<CorteCaja> CortesCaja { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetalleVentas { get; set; }

        // ========== ✅ DbSet PARA BÁSCULA ==========
        public DbSet<ConfiguracionBascula> ConfiguracionesBascula { get; set; }

        // ========== ✅ NUEVOS DbSets PARA MÓDULO DE SERVICIOS ==========
        public DbSet<ServicioVenta> ServiciosVenta { get; set; }
        public DbSet<MaterialServicio> MaterialesServicio { get; set; }
        public DbSet<PromocionVenta> PromocionesVenta { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // ✅ SOLUCIÓN 4: Configuración mejorada de SQLite
                optionsBuilder.UseSqlite("Data Source=costbenefi.db;Cache=Shared;Pooling=true;", options =>
                {
                    options.CommandTimeout(30); // 30 segundos timeout
                });

                // ✅ CONFIGURACIÓN PARA MEJOR MANEJO DE CONCURRENCIA
                optionsBuilder.EnableSensitiveDataLogging(false);
                optionsBuilder.EnableDetailedErrors(true);
            }
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

                // ========== ✅ CONFIGURACIONES PARA CAMPOS POS ==========
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

                // ========== ✅ ÍNDICES PARA POS ==========
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

            // ========== ✅ CONFIGURACIÓN PARA Venta ==========
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

                // Propiedades para comisiones
                entity.Property(e => e.ComisionTarjeta)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.PorcentajeComisionTarjeta)
                    .HasColumnType("decimal(5,2)")
                    .HasDefaultValue(0);

                entity.Property(e => e.MontoTarjeta)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.MontoEfectivo)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.MontoTransferencia)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.IVAComision)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.ComisionTotal)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                // Índices para Venta
                entity.HasIndex(e => e.NumeroTicket).IsUnique();
                entity.HasIndex(e => e.FechaVenta);
                entity.HasIndex(e => e.Usuario);
                entity.HasIndex(e => e.Estado);
            });

            // ========== ✅ CONFIGURACIÓN PARA DetalleVenta ==========
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

                // ✅ CORRECCIÓN: RawMaterialId ahora es NULLABLE para servicios
                entity.HasOne(e => e.RawMaterial)
                      .WithMany()
                      .HasForeignKey(e => e.RawMaterialId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false); // ✅ Permitir NULL

                // ✅ NUEVO: Relación con ServicioVenta
                entity.HasOne(e => e.ServicioVenta)
                      .WithMany()
                      .HasForeignKey(e => e.ServicioVentaId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false); // También opcional

                // Índices para DetalleVenta
                entity.HasIndex(e => e.VentaId);
                entity.HasIndex(e => e.RawMaterialId);
                entity.HasIndex(e => e.ServicioVentaId); // ✅ NUEVO índice
            });

            // ========== ✅ NUEVA CONFIGURACIÓN PARA ServicioVenta ==========
            modelBuilder.Entity<ServicioVenta>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NombreServicio)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(500);

                entity.Property(e => e.CategoriaServicio)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PrecioBase)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.PrecioServicio)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.DuracionEstimada)
                    .HasMaxLength(50)
                    .HasDefaultValue("30 min");

                entity.Property(e => e.CostoMateriales)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.CostoManoObra)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.MargenObjetivo)
                    .HasColumnType("decimal(5,2)")
                    .HasDefaultValue(40);

                entity.Property(e => e.PorcentajeIVA)
                    .HasColumnType("decimal(5,2)")
                    .HasDefaultValue(16);

                entity.Property(e => e.Activo)
                    .HasDefaultValue(true);

                entity.Property(e => e.IntegradoPOS)
                    .HasDefaultValue(false);

                entity.Property(e => e.PrioridadPOS)
                    .HasDefaultValue(100);

                entity.Property(e => e.RequiereConfirmacion)
                    .HasDefaultValue(false);

                entity.Property(e => e.LimiteDiario)
                    .HasDefaultValue(0);

                entity.Property(e => e.StockDisponible)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(999);

                entity.Property(e => e.CodigoServicio)
                    .HasMaxLength(50);

                entity.Property(e => e.Observaciones)
                    .HasMaxLength(500);

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.FechaActualizacion)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.UsuarioCreador)
                    .HasMaxLength(100);

                // Configuración para eliminación lógica
                entity.Property(e => e.Eliminado)
                    .HasDefaultValue(false);

                entity.Property(e => e.UsuarioEliminacion)
                    .HasMaxLength(100);

                entity.Property(e => e.MotivoEliminacion)
                    .HasMaxLength(500);

                // Filtro global: ocultar eliminados automáticamente
                entity.HasQueryFilter(e => !e.Eliminado);

                // Índices para ServicioVenta
                entity.HasIndex(e => e.NombreServicio);
                entity.HasIndex(e => e.CategoriaServicio);
                entity.HasIndex(e => e.Activo);
                entity.HasIndex(e => e.IntegradoPOS);
                entity.HasIndex(e => e.CodigoServicio);
                entity.HasIndex(e => e.Eliminado);
                entity.HasIndex(e => e.FechaCreacion);
            });

            // ========== ✅ NUEVA CONFIGURACIÓN PARA MaterialServicio ==========
            modelBuilder.Entity<MaterialServicio>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CantidadNecesaria)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.CostoUnitario)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.UnidadMedida)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.PorcentajeDesperdicio)
                    .HasColumnType("decimal(5,2)")
                    .HasDefaultValue(0);

                entity.Property(e => e.EsOpcional)
                    .HasDefaultValue(false);

                entity.Property(e => e.OrdenUso)
                    .HasDefaultValue(1);

                entity.Property(e => e.TiempoUsoMinutos)
                    .HasDefaultValue(0);

                entity.Property(e => e.Observaciones)
                    .HasMaxLength(500);

                entity.Property(e => e.VerificarDisponibilidad)
                    .HasDefaultValue(true);

                entity.Property(e => e.StockMinimoRequerido)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.FechaActualizacion)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.UsuarioCreador)
                    .HasMaxLength(100);

                // Relaciones para MaterialServicio
                entity.HasOne(e => e.ServicioVenta)
                      .WithMany(s => s.MaterialesNecesarios)
                      .HasForeignKey(e => e.ServicioVentaId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RawMaterial)
                      .WithMany()
                      .HasForeignKey(e => e.RawMaterialId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Índices para MaterialServicio
                entity.HasIndex(e => e.ServicioVentaId);
                entity.HasIndex(e => e.RawMaterialId);
                entity.HasIndex(e => e.EsOpcional);
                entity.HasIndex(e => e.OrdenUso);
            });

            // ========== ✅ NUEVA CONFIGURACIÓN PARA PromocionVenta ==========
            modelBuilder.Entity<PromocionVenta>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NombrePromocion)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(500);

                entity.Property(e => e.TipoPromocion)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasDefaultValue("DescuentoPorcentaje");

                entity.Property(e => e.CategoriaPromocion)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasDefaultValue("General");

                entity.Property(e => e.ValorPromocion)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.DescuentoMaximo)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.MontoMinimo)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.CantidadMinima)
                    .HasDefaultValue(1);

                entity.Property(e => e.FechaInicio)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.FechaFin)
                    .HasDefaultValueSql("datetime('now', '+30 days')");

                entity.Property(e => e.Activa)
                    .HasDefaultValue(true);

                entity.Property(e => e.AplicacionAutomatica)
                    .HasDefaultValue(true);

                entity.Property(e => e.IntegradaPOS)
                    .HasDefaultValue(false);

                entity.Property(e => e.Prioridad)
                    .HasDefaultValue(100);

                entity.Property(e => e.LimitePorCliente)
                    .HasDefaultValue(0);

                entity.Property(e => e.LimiteUsoTotal)
                    .HasDefaultValue(0);

                entity.Property(e => e.VecesUsada)
                    .HasDefaultValue(0);

                entity.Property(e => e.CodigoPromocion)
                    .HasMaxLength(50);

                entity.Property(e => e.ProductosAplicables)
                    .HasMaxLength(1000);

                entity.Property(e => e.ServiciosAplicables)
                    .HasMaxLength(1000);

                entity.Property(e => e.CategoriasAplicables)
                    .HasMaxLength(500);

                entity.Property(e => e.DiasAplicables)
                    .HasMaxLength(50)
                    .HasDefaultValue("L,M,Mi,J,V,S,D");

                entity.Property(e => e.HoraInicio)
                    .HasMaxLength(5)
                    .HasDefaultValue("00:00");

                entity.Property(e => e.HoraFin)
                    .HasMaxLength(5)
                    .HasDefaultValue("23:59");

                entity.Property(e => e.RequiereCodigo)
                    .HasDefaultValue(false);

                entity.Property(e => e.Combinable)
                    .HasDefaultValue(true);

                entity.Property(e => e.Observaciones)
                    .HasMaxLength(500);

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.FechaActualizacion)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.UsuarioCreador)
                    .HasMaxLength(100);

                // Configuración para eliminación lógica
                entity.Property(e => e.Eliminado)
                    .HasDefaultValue(false);

                entity.Property(e => e.UsuarioEliminacion)
                    .HasMaxLength(100);

                entity.Property(e => e.MotivoEliminacion)
                    .HasMaxLength(500);

                // Filtro global: ocultar eliminados automáticamente
                entity.HasQueryFilter(e => !e.Eliminado);

                // Índices para PromocionVenta
                entity.HasIndex(e => e.NombrePromocion);
                entity.HasIndex(e => e.TipoPromocion);
                entity.HasIndex(e => e.CategoriaPromocion);
                entity.HasIndex(e => e.Activa);
                entity.HasIndex(e => e.FechaInicio);
                entity.HasIndex(e => e.FechaFin);
                entity.HasIndex(e => e.CodigoPromocion);
                entity.HasIndex(e => e.IntegradaPOS);
                entity.HasIndex(e => e.Eliminado);
            });

            // ========== ✅ CONFIGURACIÓN PARA ConfiguracionBascula ==========
            modelBuilder.Entity<ConfiguracionBascula>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nombre)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Puerto)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.BaudRate)
                    .HasDefaultValue(9600);

                entity.Property(e => e.DataBits)
                    .HasDefaultValue(8);

                entity.Property(e => e.Paridad)
                    .HasDefaultValue(0); // None = 0, Odd = 1, Even = 2

                entity.Property(e => e.StopBits)
                    .HasDefaultValue(1); // One = 1, Two = 2

                entity.Property(e => e.ControlFlujo)
                    .HasDefaultValue(0); // None = 0, XOnXOff = 1, RequestToSend = 2

                entity.Property(e => e.TimeoutLectura)
                    .HasDefaultValue(1000);

                entity.Property(e => e.TimeoutEscritura)
                    .HasDefaultValue(1000);

                entity.Property(e => e.IntervaloLectura)
                    .HasDefaultValue(1000);

                entity.Property(e => e.UnidadPeso)
                    .HasMaxLength(10)
                    .HasDefaultValue("kg");

                entity.Property(e => e.TerminadorComando)
                    .HasMaxLength(5)
                    .HasDefaultValue("\r\n");

                entity.Property(e => e.RequiereSolicitudPeso)
                    .HasDefaultValue(false);

                entity.Property(e => e.ComandoSolicitarPeso)
                    .HasMaxLength(20)
                    .HasDefaultValue("P");

                entity.Property(e => e.ComandoTara)
                    .HasMaxLength(20)
                    .HasDefaultValue("T");

                entity.Property(e => e.ComandoInicializacion)
                    .HasMaxLength(50)
                    .HasDefaultValue("");

                entity.Property(e => e.PatronExtraccion)
                    .HasMaxLength(200)
                    .HasDefaultValue("");

                entity.Property(e => e.EsConfiguracionActiva)
                    .HasDefaultValue(false);

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.FechaActualizacion)
                    .HasDefaultValueSql("datetime('now')");

                // Índices para ConfiguracionBascula
                entity.HasIndex(e => e.EsConfiguracionActiva);
                entity.HasIndex(e => e.Puerto);
                entity.HasIndex(e => e.Nombre);
            });

            modelBuilder.Entity<CorteCaja>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UsuarioCorte)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Estado)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Pendiente");

                entity.Property(e => e.Observaciones)
                    .HasMaxLength(1000)
                    .HasDefaultValue("");

                entity.Property(e => e.MotivoSobrante)
                    .HasMaxLength(500)
                    .HasDefaultValue("");

                entity.Property(e => e.MotivoFaltante)
                    .HasMaxLength(500)
                    .HasDefaultValue("");

                entity.Property(e => e.ReferenciaDeposito)
                    .HasMaxLength(100)
                    .HasDefaultValue("");

                // Configuración de campos decimales
                entity.Property(e => e.TotalVentasCalculado)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.EfectivoCalculado)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.TarjetaCalculado)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.TransferenciaCalculado)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.ComisionesCalculadas)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.IVAComisionesCalculado)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.ComisionesTotalesCalculadas)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.GananciaBrutaCalculada)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.GananciaNetaCalculada)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.EfectivoContado)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.FondoCajaInicial)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(1000);

                entity.Property(e => e.FondoCajaSiguiente)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(1000);

                entity.Property(e => e.MontoDepositado)
                    .HasColumnType("decimal(18,4)")
                    .HasDefaultValue(0);

                entity.Property(e => e.DepositoRealizado)
                    .HasDefaultValue(false);

                entity.Property(e => e.CantidadTickets)
                    .HasDefaultValue(0);

                entity.Property(e => e.FechaCorte)
                    .HasDefaultValueSql("date('now')");

                entity.Property(e => e.FechaHoraCorte)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.FechaActualizacion)
                    .HasDefaultValueSql("datetime('now')");

                // Índices para CorteCaja
                entity.HasIndex(e => e.FechaCorte).IsUnique();
                entity.HasIndex(e => e.UsuarioCorte);
                entity.HasIndex(e => e.Estado);
                entity.HasIndex(e => e.FechaHoraCorte);
            });
            // ========== ✅ CONFIGURACIÓN PARA User ==========
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.NombreUsuario)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.NombreCompleto)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PasswordHash)
                    .IsRequired();

                entity.Property(e => e.Telefono)
                    .HasMaxLength(20)
                    .HasDefaultValue("");

                entity.Property(e => e.Rol)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Cajero");

                entity.Property(e => e.Activo)
                    .HasDefaultValue(true);

                entity.Property(e => e.IntentosFallidos)
                    .HasDefaultValue(0);

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.FechaActualizacion)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.UsuarioCreador)
                    .HasMaxLength(50)
                    .HasDefaultValue("");

                // Índices para User
                entity.HasIndex(e => e.NombreUsuario).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Rol);
                entity.HasIndex(e => e.Activo);
                entity.HasIndex(e => e.FechaCreacion);
            });
            // ========== ✅ CONFIGURACIÓN PARA UserSession ==========
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.SessionToken)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.IpAddress)
                    .HasMaxLength(50)
                    .HasDefaultValue("127.0.0.1");

                entity.Property(e => e.NombreMaquina)
                    .HasMaxLength(100)
                    .HasDefaultValue("");

                entity.Property(e => e.VersionApp)
                    .HasMaxLength(20)
                    .HasDefaultValue("1.0.0");

                entity.Property(e => e.MotivoCierre)
                    .HasMaxLength(100);

                entity.Property(e => e.FechaInicio)
                    .HasDefaultValueSql("datetime('now')");

                entity.Property(e => e.UltimaActividad)
                    .HasDefaultValueSql("datetime('now')");

                // Relación con User
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Índices para UserSession
                entity.HasIndex(e => e.SessionToken).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.FechaInicio);
                entity.HasIndex(e => e.FechaCierre);

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

        // ========== ✅ MÉTODOS EXISTENTES PARA POS ==========

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

        // ========== ✅ NUEVOS MÉTODOS PARA SERVICIOS Y PROMOCIONES ==========

        /// <summary>
        /// Obtiene servicios disponibles para venta en POS
        /// </summary>
        public IQueryable<ServicioVenta> GetServiciosDisponiblesParaVenta()
        {
            return ServiciosVenta.Where(s => s.Activo && s.StockDisponible > 0);
        }

        /// <summary>
        /// Obtiene servicios integrados con POS
        /// </summary>
        public IQueryable<ServicioVenta> GetServiciosIntegradosPOS()
        {
            return ServiciosVenta.Where(s => s.IntegradoPOS && s.Activo)
                                .OrderBy(s => s.PrioridadPOS)
                                .ThenBy(s => s.NombreServicio);
        }

        /// <summary>
        /// Obtiene promociones vigentes
        /// </summary>
        public IQueryable<PromocionVenta> GetPromocionesVigentes()
        {
            var ahora = DateTime.Now;
            return PromocionesVenta.Where(p =>
                p.Activa &&
                p.FechaInicio <= ahora &&
                p.FechaFin >= ahora &&
                (p.LimiteUsoTotal == 0 || p.VecesUsada < p.LimiteUsoTotal));
        }

        /// <summary>
        /// Obtiene promociones aplicables automáticamente
        /// </summary>
        public IQueryable<PromocionVenta> GetPromocionesAutomaticas()
        {
            return GetPromocionesVigentes().Where(p => p.AplicacionAutomatica);
        }

        /// <summary>
        /// Obtiene materiales necesarios para un servicio
        /// </summary>
        public async Task<List<MaterialServicio>> GetMaterialesServicioAsync(int servicioId)
        {
            return await MaterialesServicio
                .Include(m => m.RawMaterial)
                .Where(m => m.ServicioVentaId == servicioId)
                .OrderBy(m => m.OrdenUso)
                .ToListAsync();
        }

        /// <summary>
        /// Verifica si un servicio puede ejecutarse (materiales disponibles)
        /// </summary>
        public async Task<bool> ServicioPuedeEjecutarseAsync(int servicioId)
        {
            var materiales = await GetMaterialesServicioAsync(servicioId);
            return materiales.All(m => m.PuedeUtilizarse());
        }

        /// <summary>
        /// Obtiene servicios por categoría
        /// </summary>
        public IQueryable<ServicioVenta> GetServiciosPorCategoria(string categoria)
        {
            return ServiciosVenta.Where(s => s.CategoriaServicio == categoria && s.Activo);
        }

        /// <summary>
        /// Obtiene promociones aplicables a un producto
        /// </summary>
        public async Task<List<PromocionVenta>> GetPromocionesParaProductoAsync(int productId, string categoria = "")
        {
            var promociones = await GetPromocionesVigentes().ToListAsync();
            return promociones.Where(p => p.AplicaAProducto(productId, categoria)).ToList();
        }

        /// <summary>
        /// Obtiene promociones aplicables a un servicio
        /// </summary>
        public async Task<List<PromocionVenta>> GetPromocionesParaServicioAsync(int servicioId, string categoria = "")
        {
            var promociones = await GetPromocionesVigentes().ToListAsync();
            return promociones.Where(p => p.AplicaAServicio(servicioId, categoria)).ToList();
        }

        /// <summary>
        /// Actualiza costos de materiales para todos los servicios
        /// </summary>
        public async Task ActualizarCostosMaterialesServiciosAsync()
        {
            var servicios = await ServiciosVenta.Include(s => s.MaterialesNecesarios).ToListAsync();

            foreach (var servicio in servicios)
            {
                servicio.ActualizarCostoMateriales();
            }

            await SaveChangesAsync();
        }

        /// <summary>
        /// Obtiene estadísticas de servicios
        /// </summary>
        public async Task<dynamic> GetEstadisticasServiciosAsync()
        {
            var totalServicios = await ServiciosVenta.CountAsync();
            var serviciosActivos = await ServiciosVenta.CountAsync(s => s.Activo);
            var serviciosIntegrados = await ServiciosVenta.CountAsync(s => s.IntegradoPOS && s.Activo);
            var promocionesVigentes = await GetPromocionesVigentes().CountAsync();

            return new
            {
                TotalServicios = totalServicios,
                ServiciosActivos = serviciosActivos,
                ServiciosIntegrados = serviciosIntegrados,
                PromocionesVigentes = promocionesVigentes
            };
        }

        /// <summary>
        /// Obtiene servicios más rentables
        /// </summary>
        public IQueryable<ServicioVenta> GetServiciosMasRentables(int top = 10)
        {
            // ✅ SOLUCIÓN: Usar ToList() primero, luego ordenar en memoria
            return ServiciosVenta.Where(s => s.Activo)
                                .ToList() // ← Traer a memoria primero
                                .OrderByDescending(s => s.PrecioServicio - s.CostoMateriales - s.CostoManoObra)
                                .Take(top)
                                .AsQueryable();
        }
        /// <summary>
        /// Busca servicios por texto
        /// </summary>
        public IQueryable<ServicioVenta> BuscarServicios(string texto)
        {
            texto = texto.ToLower();
            return ServiciosVenta.Where(s =>
                s.NombreServicio.ToLower().Contains(texto) ||
                s.Descripcion.ToLower().Contains(texto) ||
                s.CategoriaServicio.ToLower().Contains(texto));
        }

        /// <summary>
        /// Busca promociones por texto
        /// </summary>
        public IQueryable<PromocionVenta> BuscarPromociones(string texto)
        {
            texto = texto.ToLower();
            return PromocionesVenta.Where(p =>
                p.NombrePromocion.ToLower().Contains(texto) ||
                p.Descripcion.ToLower().Contains(texto) ||
                p.TipoPromocion.ToLower().Contains(texto));
        }

        // ========== ✅ MÉTODOS EXISTENTES PARA BÁSCULA ==========

        /// <summary>
        /// Obtiene la configuración activa de báscula
        /// </summary>
        public async Task<ConfiguracionBascula?> GetConfiguracionBasculaActivaAsync()
        {
            return await ConfiguracionesBascula
                .FirstOrDefaultAsync(c => c.EsConfiguracionActiva);
        }

        /// <summary>
        /// Obtiene todas las configuraciones de báscula
        /// </summary>
        public async Task<ConfiguracionBascula[]> GetConfiguracionesBasculaAsync()
        {
            return await ConfiguracionesBascula
                .OrderByDescending(c => c.EsConfiguracionActiva)
                .ThenBy(c => c.Nombre)
                .ToArrayAsync();
        }

        /// <summary>
        /// Establece una configuración como activa
        /// </summary>
        public async Task<bool> EstablecerConfiguracionBasculaActivaAsync(int configuracionId)
        {
            try
            {
                // Desactivar todas las configuraciones
                var configuraciones = await ConfiguracionesBascula.ToListAsync();
                foreach (var config in configuraciones)
                {
                    config.EsConfiguracionActiva = false;
                }

                // Activar la configuración seleccionada
                var nuevaActiva = configuraciones.FirstOrDefault(c => c.Id == configuracionId);
                if (nuevaActiva != null)
                {
                    nuevaActiva.EsConfiguracionActiva = true;
                    nuevaActiva.FechaActualizacion = DateTime.Now;
                    await SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Crea configuración de báscula por defecto si no existe
        /// </summary>
        public async Task CrearConfiguracionBasculaPorDefectoAsync()
        {
            try
            {
                var existeConfiguracion = await ConfiguracionesBascula.AnyAsync();
                if (!existeConfiguracion)
                {
                    var puertos = System.IO.Ports.SerialPort.GetPortNames();
                    var puertoDefecto = puertos.Length > 0 ? puertos[0] : "COM1";

                    var configDefecto = new ConfiguracionBascula
                    {
                        Nombre = "Báscula Principal",
                        Puerto = puertoDefecto,
                        BaudRate = 9600,
                        UnidadPeso = "kg",
                        RequiereSolicitudPeso = false,
                        PatronExtraccion = @"(\d+\.?\d*)",
                        EsConfiguracionActiva = true
                    };

                    ConfiguracionesBascula.Add(configDefecto);
                    await SaveChangesAsync();
                }
            }
            catch
            {
                // Silencioso - no es crítico si falla
            }
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

            // ========== ✅ Timestamp para entidades POS ==========
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

            // ========== ✅ NUEVO: Timestamp para configuraciones de báscula ==========
            var basculaEntries = ChangeTracker.Entries<ConfiguracionBascula>();
            foreach (var entry in basculaEntries)
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

            var corteEntries = ChangeTracker.Entries<CorteCaja>();
            foreach (var entry in corteEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.FechaCreacion = DateTime.Now;
                    entry.Entity.FechaActualizacion = DateTime.Now;
                    entry.Entity.FechaHoraCorte = DateTime.Now;

                    // Establecer fecha de corte solo si no está definida
                    if (entry.Entity.FechaCorte == default)
                    {
                        entry.Entity.FechaCorte = DateTime.Today;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.FechaActualizacion = DateTime.Now;
                }
            }

            // ========== ✅ NUEVO: Timestamp para usuarios ==========
            var userEntries = ChangeTracker.Entries<User>();
            foreach (var entry in userEntries)
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

            var sessionEntries = ChangeTracker.Entries<UserSession>();
            foreach (var entry in sessionEntries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.FechaInicio = DateTime.Now;
                    entry.Entity.UltimaActividad = DateTime.Now;
                }
            }

            // ========== ✅ NUEVO: Timestamp para módulo de servicios ==========
            var servicioEntries = ChangeTracker.Entries<ServicioVenta>();
            foreach (var entry in servicioEntries)
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

            var materialServicioEntries = ChangeTracker.Entries<MaterialServicio>();
            foreach (var entry in materialServicioEntries)
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

            var promocionEntries = ChangeTracker.Entries<PromocionVenta>();
            foreach (var entry in promocionEntries)
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
        }

        /// <summary>
        /// Obtiene el corte de caja del día especificado
        /// </summary>
        public async Task<CorteCaja?> GetCorteDelDiaAsync(DateTime fecha)
        {
            var fechaSolo = fecha.Date;
            return await CortesCaja
                .FirstOrDefaultAsync(c => c.FechaCorte == fechaSolo);
        }

        /// <summary>
        /// Verifica si existe un corte para el día especificado
        /// </summary>
        public async Task<bool> ExisteCorteDelDiaAsync(DateTime fecha)
        {
            var fechaSolo = fecha.Date;
            return await CortesCaja
                .AnyAsync(c => c.FechaCorte == fechaSolo);
        }

        /// <summary>
        /// Obtiene cortes de caja en un rango de fechas
        /// </summary>
        public IQueryable<CorteCaja> GetCortesEnRango(DateTime desde, DateTime hasta)
        {
            return CortesCaja.Where(c =>
                c.FechaCorte >= desde.Date &&
                c.FechaCorte <= hasta.Date);
        }

        /// <summary>
        /// Obtiene estadísticas financieras del día para corte de caja
        /// </summary>
        public async Task<dynamic> GetEstadisticasFinancierasDelDiaAsync(DateTime fecha)
        {
            var ventasDelDia = GetVentasDelDia(fecha);

            var estadisticas = await ventasDelDia
                .GroupBy(v => 1) // Agrupar todas las ventas
                .Select(g => new
                {
                    TotalVentas = g.Sum(v => v.Total),
                    CantidadTickets = g.Count(),
                    TotalEfectivo = g.Sum(v => v.MontoEfectivo),
                    TotalTarjeta = g.Sum(v => v.MontoTarjeta),
                    TotalTransferencia = g.Sum(v => v.MontoTransferencia),
                    TotalComisiones = g.Sum(v => v.ComisionTarjeta),
                    TotalIVAComisiones = g.Sum(v => v.IVAComision),
                    TotalComisionesCompletas = g.Sum(v => v.ComisionTotal),
                    GananciaBruta = g.Sum(v => v.DetallesVenta.Sum(d => d.GananciaLinea)),
                    GananciaNeta = g.Sum(v => v.GananciaBruta) - g.Sum(v => v.ComisionTotal)
                })
                .FirstOrDefaultAsync();

            return estadisticas ?? new
            {
                TotalVentas = 0m,
                CantidadTickets = 0,
                TotalEfectivo = 0m,
                TotalTarjeta = 0m,
                TotalTransferencia = 0m,
                TotalComisiones = 0m,
                TotalIVAComisiones = 0m,
                TotalComisionesCompletas = 0m,
                GananciaBruta = 0m,
                GananciaNeta = 0m
            };
        }

        /// <summary>
        /// Obtiene ventas detalladas para el corte de caja
        /// </summary>
        public async Task<List<Venta>> GetVentasParaCorteAsync(DateTime fecha)
        {
            return await GetVentasDelDia(fecha)
                .Include(v => v.DetallesVenta)
                    .ThenInclude(d => d.RawMaterial)
                .OrderBy(v => v.FechaVenta)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene el último corte de caja completado (para obtener fondo anterior)
        /// </summary>
        public async Task<CorteCaja?> GetUltimoCorteCompletadoAsync()
        {
            return await CortesCaja
                .Where(c => c.Estado == "Completado")
                .OrderByDescending(c => c.FechaCorte)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Obtiene cortes con diferencias significativas para auditoría
        /// </summary>
        public async Task<List<CorteCaja>> GetCortesConDiferenciasAsync(decimal margenMaximo = 10)
        {
            // Como DiferenciaEfectivo es una propiedad calculada, 
            // tenemos que traer todos los cortes y filtrar en memoria
            var cortes = await CortesCaja
                .Where(c => c.Estado == "Completado")
                .ToListAsync();

            return cortes
                .Where(c => Math.Abs(c.DiferenciaEfectivo) > margenMaximo)
                .OrderByDescending(c => Math.Abs(c.DiferenciaEfectivo))
                .ToList();
        }

        /// <summary>
        /// Obtiene resumen mensual de cortes para reportes
        /// </summary>
        public async Task<dynamic> GetResumenMensualCortesAsync(int año, int mes)
        {
            var primerDia = new DateTime(año, mes, 1);
            var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

            var cortes = await GetCortesEnRango(primerDia, ultimoDia)
                .Where(c => c.Estado == "Completado")
                .ToListAsync();

            if (!cortes.Any())
            {
                return new
                {
                    Mes = $"{primerDia:MMMM yyyy}",
                    CantidadCortes = 0,
                    TotalVentas = 0m,
                    TotalGanancias = 0m,
                    PromedioDiario = 0m,
                    DiasConDiferencias = 0
                };
            }

            return new
            {
                Mes = $"{primerDia:MMMM yyyy}",
                CantidadCortes = cortes.Count,
                TotalVentas = cortes.Sum(c => c.TotalVentasCalculado),
                TotalGanancias = cortes.Sum(c => c.GananciaNetaCalculada),
                PromedioDiario = cortes.Average(c => c.TotalVentasCalculado),
                DiasConDiferencias = cortes.Count(c => !c.DiferenciaAceptable)
            };
        }

        // ========== ✅ MÉTODOS PARA GESTIÓN DE USUARIOS ==========

        /// <summary>
        /// Obtiene un usuario por nombre de usuario
        /// </summary>

        public async Task<dynamic> GetEstadisticasUsuariosRealesAsync()
        {
            var totalUsuarios = await Users.Where(u => u.Id > 0 && !u.Eliminado).CountAsync();
            var usuariosActivos = await Users.Where(u => u.Id > 0 && !u.Eliminado && u.Activo).CountAsync();
            var usuariosBloqueados = await Users.Where(u => u.Id > 0 && !u.Eliminado && u.EstaBloqueado).CountAsync();
            var sesionesActivas = await UserSessions.CountAsync(s => s.FechaCierre == null && s.UserId > 0);

            var usuariosPorRol = await Users
                .Where(u => u.Id > 0 && !u.Eliminado && u.Activo)
                .GroupBy(u => u.Rol)
                .Select(g => new { Rol = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            return new
            {
                TotalUsuarios = totalUsuarios,
                UsuariosActivos = usuariosActivos,
                UsuariosInactivos = totalUsuarios - usuariosActivos,
                UsuariosBloqueados = usuariosBloqueados,
                SesionesActivas = sesionesActivas,
                UsuariosPorRol = usuariosPorRol
            };
        }
        public async Task CrearUsuarioDuenoPorDefectoAsync()
        {
            try
            {
                var existeDuenoReal = await ExisteDuenoRealAsync();
                if (!existeDuenoReal)
                {
                    var dueno = new User
                    {
                        NombreUsuario = "dueno",
                        NombreCompleto = "Dueño del Negocio",
                        Email = "dueno@negocio.com",
                        PasswordHash = User.GenerarHashPassword("dueno123"), // Cambiar en producción
                        Rol = "Dueño",
                        Activo = true,
                        UsuarioCreador = "Sistema"
                    };

                    Users.Add(dueno);
                    await SaveChangesAsync();
                }
            }
            catch
            {
                // Silencioso - no es crítico si falla
            }
        }

        public async Task<List<UserSession>> GetSesionesActivasRealesAsync()
        {
            return await UserSessions
                .Include(s => s.User)
                .Where(s => s.FechaCierre == null && s.UserId > 0) // Solo usuarios reales
                .OrderByDescending(s => s.UltimaActividad)
                .ToListAsync();
        }


        /// <summary>
        /// Obtiene usuarios por rol
        /// </summary>
        public async Task<List<User>> GetUsersByRoleAsync(string rol)
        {
            return await Users
                .Where(u => !u.Eliminado && u.Rol == rol && u.Activo)
                .OrderBy(u => u.NombreCompleto)
                .ToListAsync();
        }

        /// <summary>
        /// Verifica si un nombre de usuario ya existe
        /// </summary>
        public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, int? excludeUserId = null)
        {
            var query = Users.Where(u => !u.Eliminado && u.NombreUsuario.ToLower() == nombreUsuario.ToLower());

            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return await query.AnyAsync();
        }

        /// <summary>
        /// Verifica si un email ya existe
        /// </summary>
        public async Task<bool> ExisteEmailAsync(string email, int? excludeUserId = null)
        {
            var query = Users.Where(u => !u.Eliminado && u.Email.ToLower() == email.ToLower());

            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return await query.AnyAsync();
        }
        /// <summary>
        /// Obtiene la sesión activa de un usuario
        /// </summary>
        public async Task<UserSession?> GetSesionActivaAsync(int userId)
        {
            return await UserSessions
                .Where(s => s.UserId == userId && s.FechaCierre == null)
                .OrderByDescending(s => s.FechaInicio)
                .FirstOrDefaultAsync();
        }
        public async Task<bool> ExisteDuenoRealAsync()
        {
            return await Users
                .Where(u => u.Id > 0 && !u.Eliminado) // Solo usuarios reales
                .AnyAsync(u => u.Rol == "Dueño" && u.Activo);
        }
        public async Task<int> ContarDuenosRealesActivosAsync(int? excludeUserId = null)
        {
            var query = Users.Where(u => u.Id > 0 && !u.Eliminado && u.Rol == "Dueño" && u.Activo);

            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return await query.CountAsync();
        }
        /// <summary>
        /// Obtiene sesiones activas en el sistema
        /// </summary>
        public async Task<List<UserSession>> GetSesionesActivasAsync()
        {
            return await UserSessions
                .Include(s => s.User)
                .Where(s => s.FechaCierre == null)
                .OrderByDescending(s => s.UltimaActividad)
                .ToListAsync();
        }
        public async Task<List<User>> GetUsuariosRealesAsync(bool incluirInactivos = false)
        {
            var query = Users.Where(u => u.Id > 0 && !u.Eliminado); // Solo usuarios reales

            if (!incluirInactivos)
                query = query.Where(u => u.Activo);

            return await query
                .OrderBy(u => u.Rol == "Dueño" ? 0 : u.Rol == "Encargado" ? 1 : 2) // Dueño primero
                .ThenBy(u => u.NombreCompleto)
                .ToListAsync();
        }
        /// <summary>
        /// Cierra sesiones inactivas automáticamente
        /// </summary>
        public async Task<int> CerrarSesionesInactivasAsync(int horasInactividad = 8)
        {
            var fechaLimite = DateTime.Now.AddHours(-horasInactividad);

            var sesionesInactivas = await UserSessions
                .Where(s => s.FechaCierre == null && s.UltimaActividad < fechaLimite)
                .ToListAsync();

            foreach (var sesion in sesionesInactivas)
            {
                sesion.CerrarSesion("Inactividad");
            }

            if (sesionesInactivas.Any())
            {
                await SaveChangesAsync();
            }

            return sesionesInactivas.Count;
        }
        public async Task<User?> GetUserByUsernameAsync(string nombreUsuario)
        {
            System.Diagnostics.Debug.WriteLine($"🔍 GetUserByUsernameAsync: Buscando usuario '{nombreUsuario}'");

            try
            {
                var usuario = await Users
                    .Where(u => u.Id > 0 && !u.Eliminado) // Solo usuarios reales, no eliminados
                    .FirstOrDefaultAsync(u => u.NombreUsuario.ToLower() == nombreUsuario.ToLower());

                if (usuario != null)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Usuario encontrado en BD: {usuario.NombreCompleto} ({usuario.Rol})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Usuario '{nombreUsuario}' NO encontrado en BD");
                }

                return usuario;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERROR en GetUserByUsernameAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Crea el usuario administrador por defecto si no existe
        /// </summary>
        public async Task CrearUsuarioAdminPorDefectoAsync()
        {
            try
            {
                var existeAdmin = await Users.AnyAsync(u => u.Rol == "Administrador");
                if (!existeAdmin)
                {
                    var adminUser = new User
                    {
                        NombreUsuario = "admin",
                        NombreCompleto = "Administrador del Sistema",
                        Email = "admin@costbenefi.com",
                        PasswordHash = User.GenerarHashPassword("admin123"), // Cambiar en producción
                        Rol = "Administrador",
                        Activo = true,
                        UsuarioCreador = "Sistema"
                    };

                    Users.Add(adminUser);
                    await SaveChangesAsync();
                }
            }
            catch
            {
                // Silencioso - no es crítico si falla
            }
        }

       
        /// <summary>
        /// Obtiene productos que se venden a granel (por peso, volumen o longitud)
        /// </summary>
        public IQueryable<RawMaterial> GetProductosAGranel()
        {
            return RawMaterials.Where(p =>
                p.ActivoParaVenta &&
                !p.Eliminado &&
                (// ✅ PESO
                 p.UnidadMedida.ToLower().Contains("kg") ||
                 p.UnidadMedida.ToLower().Contains("gr") ||
                 p.UnidadMedida.ToLower().Contains("gram") ||
                 p.UnidadMedida.ToLower().Contains("kilo") ||
                 // ✅ VOLUMEN
                 p.UnidadMedida.ToLower().Contains("lt") ||
                 p.UnidadMedida.ToLower().Contains("ltr") ||
                 p.UnidadMedida.ToLower().Contains("litro") ||
                 p.UnidadMedida.ToLower().Contains("ml") ||
                 p.UnidadMedida.ToLower().Contains("mililitro") ||
                 // ✅ LONGITUD
                 p.UnidadMedida.ToLower().Contains("m") ||
                 p.UnidadMedida.ToLower().Contains("mt") ||
                 p.UnidadMedida.ToLower().Contains("mtr") ||
                 p.UnidadMedida.ToLower().Contains("metro") ||
                 p.UnidadMedida.ToLower().Contains("cm") ||
                 p.UnidadMedida.ToLower().Contains("centimetro") ||
                 p.UnidadMedida.ToLower().Contains("centímetro"))
            ).OrderBy(p => p.NombreArticulo);
        }

        /// <summary>
        /// Verifica si un producto específico es a granel
        /// </summary>
        public bool EsProductoAGranel(int productId)
        {
            var producto = RawMaterials.Find(productId);
            if (producto == null) return false;

            return EsProductoAGranel(producto);
        }

        /// <summary>
        /// Verifica si un producto es a granel basado en su unidad de medida
        /// </summary>
        public bool EsProductoAGranel(RawMaterial producto)
        {
            if (producto == null) return false;

            var unidad = producto.UnidadMedida.ToLower().Trim();

            // ✅ UNIDADES DE PESO
            if (unidad.Contains("kg") || unidad.Contains("gram") ||
                unidad.Contains("kilo") || unidad.Contains("gr"))
                return true;

            // ✅ UNIDADES DE VOLUMEN    
            if (unidad.Contains("lt") || unidad.Contains("ltr") ||
                unidad.Contains("litro") || unidad.Contains("ml") ||
                unidad.Contains("mililitro"))
                return true;

            // ✅ UNIDADES DE LONGITUD
            if (unidad.Contains("m") || unidad.Contains("mt") ||
                unidad.Contains("mtr") || unidad.Contains("metro") ||
                unidad.Contains("cm") || unidad.Contains("centimetro") ||
                unidad.Contains("centímetro"))
                return true;

            return false;
        }

        /// <summary>
        /// Obtiene estadísticas de productos a granel
        /// </summary>
        public async Task<dynamic> GetEstadisticasProductosAGranelAsync()
        {
            var productosAGranel = await GetProductosAGranel().ToListAsync();
            var totalProductos = await RawMaterials.CountAsync(p => p.ActivoParaVenta && !p.Eliminado);

            return new
            {
                CantidadProductosAGranel = productosAGranel.Count,
                TotalProductos = totalProductos,
                PorcentajeAGranel = totalProductos > 0 ? (decimal)productosAGranel.Count / totalProductos * 100 : 0,
                ValorTotalAGranel = productosAGranel.Sum(p => p.ValorTotalConIVA),

                // ✅ ESTADÍSTICAS POR TIPO DE MEDIDA
                ProductosPorPeso = productosAGranel
                    .Where(p => p.UnidadMedida.ToLower().Contains("kg") || p.UnidadMedida.ToLower().Contains("gr"))
                    .Count(),
                ProductosPorVolumen = productosAGranel
                    .Where(p => p.UnidadMedida.ToLower().Contains("lt") || p.UnidadMedida.ToLower().Contains("ml"))
                    .Count(),
                ProductosPorLongitud = productosAGranel
                    .Where(p => p.UnidadMedida.ToLower().Contains("m") || p.UnidadMedida.ToLower().Contains("cm"))
                    .Count(),

                // ✅ EJEMPLOS VARIADOS POR CATEGORÍA  
                ProductosMasPesados = productosAGranel
                    .Where(p => p.UnidadMedida.ToLower().Contains("kg"))
                    .OrderByDescending(p => p.StockTotal)
                    .Take(3)
                    .Select(p => new { p.NombreArticulo, p.StockTotal, p.UnidadMedida })
                    .ToList(),

                ProductosMasLargos = productosAGranel
                    .Where(p => p.UnidadMedida.ToLower().Contains("m") && !p.UnidadMedida.ToLower().Contains("cm"))
                    .OrderByDescending(p => p.StockTotal)
                    .Take(3)
                    .Select(p => new { p.NombreArticulo, p.StockTotal, p.UnidadMedida })
                    .ToList(),

                ProductosMasVoluminosos = productosAGranel
                    .Where(p => p.UnidadMedida.ToLower().Contains("lt"))
                    .OrderByDescending(p => p.StockTotal)
                    .Take(3)
                    .Select(p => new { p.NombreArticulo, p.StockTotal, p.UnidadMedida })
                    .ToList()
            };
        }
    }
}
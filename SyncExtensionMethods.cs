using System;
using System.Linq; // ✅ AGREGAR: Para usar .Any()
using costbenefi.Models;

namespace costbenefi.Services
{
    /// <summary>
    /// Métodos de extensión para convertir entidades a modelos de sincronización
    /// </summary>
    public static class SyncExtensionMethods
    {
        /// <summary>
        /// Convierte RawMaterial a ProductoSync
        /// </summary>
        public static ProductoSync ToProductoSync(this RawMaterial material)
        {
            return new ProductoSync
            {
                Id = material.Id,
                CodigoBarras = material.CodigoBarras ?? "",
                NombreArticulo = material.NombreArticulo ?? "",
                Categoria = material.Categoria ?? "",
                PrecioUnitario = material.PrecioVenta, // Usar PrecioVenta para POS
                PrecioCompra = material.PrecioConIVA, // Usar PrecioConIVA como precio de compra
                StockTotal = (int)material.StockTotal,
                StockMinimo = (int)material.AlertaStockBajo,
                UnidadMedida = material.UnidadMedida ?? "",
                ActivoParaVenta = material.ActivoParaVenta,
                RequiereBascula = EsProductoAGranel(material), // Verificar si es a granel
                FechaActualizacion = material.FechaActualizacion,
                Eliminado = material.Eliminado,

                // Información adicional para POS
                ImagenUrl = "",
                Descripcion = material.Observaciones ?? "",
                PermiteDescuento = true,
                MaximoDescuento = material.PorcentajeDescuento
            };
        }

        /// <summary>
        /// Verifica si un producto es a granel basado en su unidad de medida
        /// </summary>
        private static bool EsProductoAGranel(RawMaterial producto)
        {
            if (producto == null) return false;

            var unidad = producto.UnidadMedida.ToLower().Trim();

            // Unidades de peso
            if (unidad.Contains("kg") || unidad.Contains("gram") ||
                unidad.Contains("kilo") || unidad.Contains("gr"))
                return true;

            // Unidades de volumen    
            if (unidad.Contains("lt") || unidad.Contains("ltr") ||
                unidad.Contains("litro") || unidad.Contains("ml") ||
                unidad.Contains("mililitro"))
                return true;

            // Unidades de longitud
            if (unidad.Contains("m") || unidad.Contains("mt") ||
                unidad.Contains("mtr") || unidad.Contains("metro") ||
                unidad.Contains("cm") || unidad.Contains("centimetro") ||
                unidad.Contains("centímetro"))
                return true;

            return false;
        }

        /// <summary>
        /// Convierte ServicioVenta a ServicioSync
        /// </summary>
        public static ServicioSync ToServicioSync(this ServicioVenta servicio)
        {
            // Parsear duración de forma segura
            int duracionMinutos = 60;
            if (!string.IsNullOrEmpty(servicio.DuracionEstimada))
            {
                var duracionTexto = servicio.DuracionEstimada.Replace("min", "").Replace(" ", "").Trim();
                if (int.TryParse(duracionTexto, out int duracion))
                {
                    duracionMinutos = duracion;
                }
            }

            return new ServicioSync
            {
                Id = servicio.Id,
                NombreServicio = servicio.NombreServicio ?? "",
                Descripcion = servicio.Descripcion ?? "",
                Precio = servicio.PrecioServicio,
                DuracionMinutos = duracionMinutos,
                Activo = servicio.Activo,
                IntegradoPOS = servicio.IntegradoPOS,
                PrioridadPOS = servicio.PrioridadPOS,
                FechaActualizacion = servicio.FechaActualizacion,
                Eliminado = servicio.Eliminado,

                // Información adicional
                ImagenUrl = "",
                RequiereCita = servicio.RequiereConfirmacion,
                CategoriaNombre = servicio.CategoriaServicio ?? ""
            };
        }

        /// <summary>
        /// Convierte PromocionVenta a PromocionSync
        /// </summary>
        public static PromocionSync ToPromocionSync(this PromocionVenta promocion)
        {
            return new PromocionSync
            {
                Id = promocion.Id,
                NombrePromocion = promocion.NombrePromocion ?? "",
                Descripcion = promocion.Descripcion ?? "",
                TipoPromocion = promocion.TipoPromocion ?? "",
                ValorDescuento = promocion.ValorPromocion, // Asumiendo que se llama ValorPromocion
                EsPorcentaje = false, // Valor por defecto
                FechaInicio = promocion.FechaInicio,
                FechaFin = promocion.FechaFin,
                Activa = promocion.Activa,
                FechaActualizacion = promocion.FechaActualizacion,
                Eliminado = false,

                // Condiciones (valores por defecto)
                MontoMinimoCompra = 0,
                CantidadMinimaProductos = 0,
                ProductosAplicables = new System.Collections.Generic.List<int>(),
                ServiciosAplicables = new System.Collections.Generic.List<int>()
            };
        }

        /// <summary>
        /// Convierte Venta a VentaSync (para envío a servidor)
        /// </summary>
        public static VentaSync ToVentaSync(this Venta venta)
        {
            var ventaSync = new VentaSync
            {
                NumeroTicket = venta.NumeroTicket.ToString(),
                Cliente = venta.Cliente ?? "",
                Usuario = venta.Usuario ?? "",
                FormaPago = venta.FormaPago ?? "",
                Total = venta.Total,
                MontoEfectivo = venta.MontoEfectivo,
                MontoTarjeta = venta.MontoTarjeta,
                MontoTransferencia = venta.MontoTransferencia,
                ComisionTotal = venta.ComisionTotal,
                FechaVenta = venta.FechaVenta,

                // Información de descuentos
                TieneDescuentosAplicados = venta.TieneDescuentosAplicados,
                TotalDescuentosAplicados = venta.TotalDescuentosAplicados,
                UsuarioAutorizadorDescuento = venta.UsuarioAutorizadorDescuento ?? "",
                MotivoDescuentoGeneral = venta.MotivoDescuentoGeneral ?? "",

                Detalles = new System.Collections.Generic.List<DetalleVentaSync>()
            };

            // Convertir detalles si existen
            if (venta.DetallesVenta != null && venta.DetallesVenta.Any())
            {
                foreach (var detalle in venta.DetallesVenta)
                {
                    ventaSync.Detalles.Add(detalle.ToDetalleVentaSync());
                }
            }

            return ventaSync;
        }

        /// <summary>
        /// Convierte DetalleVenta a DetalleVentaSync
        /// </summary>
        public static DetalleVentaSync ToDetalleVentaSync(this DetalleVenta detalle)
        {
            return new DetalleVentaSync
            {
                RawMaterialId = detalle.RawMaterialId,
                ServicioVentaId = detalle.ServicioVentaId,
                NombreProducto = detalle.NombreProducto ?? "",
                Cantidad = detalle.Cantidad,
                PrecioUnitario = detalle.PrecioUnitario,
                SubTotal = detalle.SubTotal,
                UnidadMedida = detalle.UnidadMedida ?? "",

                // Información de descuentos
                PrecioOriginal = detalle.PrecioOriginal,
                DescuentoUnitario = detalle.DescuentoUnitario,
                TieneDescuentoManual = detalle.TieneDescuentoManual,
                MotivoDescuentoDetalle = detalle.MotivoDescuentoDetalle ?? ""
            };
        }

        /// <summary>
        /// Convierte Movimiento a MovimientoStockSync
        /// </summary>
        public static MovimientoStockSync ToMovimientoStockSync(this Movimiento movimiento, string terminalOrigen = "")
        {
            return new MovimientoStockSync
            {
                ProductoId = movimiento.RawMaterialId, // Directamente sin ??, // ✅ CORRECCIÓN: Usar ?? en lugar de HasValue
                TipoMovimiento = movimiento.TipoMovimiento ?? "",
                CantidadMovida = movimiento.Cantidad,
                Motivo = movimiento.Motivo ?? "",
                Usuario = movimiento.Usuario ?? "",
                FechaMovimiento = movimiento.FechaMovimiento,
                TerminalOrigen = terminalOrigen
            };
        }

        /// <summary>
        /// Crea un evento de terminal para auditoría
        /// </summary>
        public static EventoTerminalSync CrearEvento(string terminalId, string tipoEvento, string descripcion, string usuario = "", string datosAdicionales = "")
        {
            return new EventoTerminalSync
            {
                TerminalId = terminalId,
                TipoEvento = tipoEvento,
                Descripcion = descripcion,
                Usuario = string.IsNullOrEmpty(usuario) ? Environment.UserName : usuario,
                FechaEvento = DateTime.Now,
                DatosAdicionales = datosAdicionales
            };
        }

        /// <summary>
        /// Valida si un ProductoSync está completo
        /// </summary>
        public static bool EsValido(this ProductoSync producto)
        {
            return producto.Id > 0 &&
                   !string.IsNullOrEmpty(producto.NombreArticulo) &&
                   !string.IsNullOrEmpty(producto.CodigoBarras);
        }

        /// <summary>
        /// Valida si un ServicioSync está completo
        /// </summary>
        public static bool EsValido(this ServicioSync servicio)
        {
            return servicio.Id > 0 &&
                   !string.IsNullOrEmpty(servicio.NombreServicio) &&
                   servicio.Precio > 0;
        }

        /// <summary>
        /// Valida si una VentaSync está completa
        /// </summary>
        public static bool EsValido(this VentaSync venta)
        {
            return !string.IsNullOrEmpty(venta.NumeroTicket) &&
                   venta.Total > 0 &&
                   venta.Detalles.Count > 0;
        }
    }
}
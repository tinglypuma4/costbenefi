using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace costbenefi.Models
{
    /// <summary>
    /// DetalleVenta CON NOTIFICACIONES - Solo se agregó INotifyPropertyChanged
    /// </summary>
    public class DetalleVenta : INotifyPropertyChanged
    {
        #region Campos privados para notificaciones
        private decimal _cantidad;
        private decimal _precioUnitario;
        private decimal _subTotal;
        private decimal _costoUnitario;
        private string _nombreProducto;
        private string _unidadMedida;
        private decimal _porcentajeIVA = 16.0m;
        private decimal _descuentoAplicado = 0;


        public decimal PrecioOriginal { get; set; } = 0; // Precio antes del descuento
        public decimal DescuentoUnitario { get; set; } = 0; // Descuento por unidad
        public string MotivoDescuentoDetalle { get; set; } = "";
        public bool TieneDescuentoManual { get; set; } = false;

        public decimal TotalDescuentoLinea => DescuentoUnitario * Cantidad;
        #endregion

        [Key]
        public int Id { get; set; }

        // ===== RELACIONES =====
        public int VentaId { get; set; }
        public virtual Venta Venta { get; set; }
        public int? RawMaterialId { get; set; }
        public virtual RawMaterial RawMaterial { get; set; }
        public int? ServicioVentaId { get; set; }
        public virtual ServicioVenta ServicioVenta { get; set; }

        // ===== DATOS DEL PRODUCTO VENDIDO CON NOTIFICACIÓN =====

        [Column(TypeName = "decimal(18,4)")]
        public decimal Cantidad
        {
            get => _cantidad;
            set
            {
                if (SetProperty(ref _cantidad, value))
                {
                    CalcularSubTotal();
                    OnPropertyChanged(nameof(ValorSinDescuento));
                    OnPropertyChanged(nameof(GananciaLinea));
                    OnPropertyChanged(nameof(MargenPorcentaje));
                }
            }
        }

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioUnitario
        {
            get => _precioUnitario;
            set
            {
                if (SetProperty(ref _precioUnitario, value))
                {
                    CalcularSubTotal();
                    OnPropertyChanged(nameof(ValorSinDescuento));
                    OnPropertyChanged(nameof(GananciaLinea));
                    OnPropertyChanged(nameof(MargenPorcentaje));
                }
            }
        }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SubTotal
        {
            get => _subTotal;
            set
            {
                if (SetProperty(ref _subTotal, value))
                {
                    OnPropertyChanged(nameof(GananciaLinea));
                    OnPropertyChanged(nameof(MargenPorcentaje));
                    OnPropertyChanged(nameof(IVALinea));
                    OnPropertyChanged(nameof(TotalConIVA));
                }
            }
        }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoUnitario
        {
            get => _costoUnitario;
            set
            {
                if (SetProperty(ref _costoUnitario, value))
                {
                    OnPropertyChanged(nameof(GananciaLinea));
                    OnPropertyChanged(nameof(MargenPorcentaje));
                }
            }
        }


        // ===== CAMPOS ADICIONALES PARA EL TICKET =====

        [Required]
        [StringLength(200)]
        public string NombreProducto
        {
            get => _nombreProducto;
            set => SetProperty(ref _nombreProducto, value);
        }

        [Required]
        [StringLength(20)]
        public string UnidadMedida
        {
            get => _unidadMedida;
            set => SetProperty(ref _unidadMedida, value);
        }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeIVA
        {
            get => _porcentajeIVA;
            set
            {
                if (SetProperty(ref _porcentajeIVA, value))
                {
                    OnPropertyChanged(nameof(IVALinea));
                    OnPropertyChanged(nameof(TotalConIVA));
                }
            }
        }

        [Column(TypeName = "decimal(18,4)")]
        public decimal DescuentoAplicado
        {
            get => _descuentoAplicado;
            set
            {
                if (SetProperty(ref _descuentoAplicado, value))
                {
                    CalcularSubTotal();
                    OnPropertyChanged(nameof(TieneDescuento));
                }
            }
        }

        // ===== PROPIEDADES CALCULADAS (IGUALES QUE ANTES) =====

        [NotMapped]
        public decimal GananciaLinea => SubTotal - (CostoUnitario * Cantidad);

        [NotMapped]
        public decimal MargenPorcentaje
        {
            get
            {
                if (SubTotal <= 0) return 0;
                return (GananciaLinea / SubTotal) * 100;
            }
        }

        [NotMapped]
        public decimal IVALinea => SubTotal * (PorcentajeIVA / 100);

        [NotMapped]
        public decimal TotalConIVA => SubTotal + IVALinea;

        [NotMapped]
        public decimal PrecioUnitarioConIVA => PrecioUnitario * (1 + (PorcentajeIVA / 100));

        [NotMapped]
        public decimal ValorSinDescuento => Cantidad * PrecioUnitario;

        [NotMapped]
        public bool TieneDescuento => DescuentoAplicado > 0;

        // ===== MÉTODOS (IGUALES QUE ANTES) =====

        public void CalcularSubTotal()
        {
            // ✅ CÁLCULO SIMPLE: Si PrecioUnitario ya tiene descuento aplicado,
            // solo usar DescuentoAplicado para descuentos adicionales tradicionales
            var nuevoSubTotal = (Cantidad * PrecioUnitario) - DescuentoAplicado;
            if (nuevoSubTotal < 0) nuevoSubTotal = 0;

            if (Math.Abs(_subTotal - nuevoSubTotal) > 0.001m)
            {
                _subTotal = nuevoSubTotal;
                OnPropertyChanged(nameof(SubTotal));
            }
        }

        public void AplicarDescuentoConAuditoria(decimal descuentoPorUnidad, string motivo, string usuarioAutorizador = "")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🎁 === APLICANDO DESCUENTO A {NombreProducto} ===");
                System.Diagnostics.Debug.WriteLine($"   • Precio actual: ${PrecioUnitario:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Cantidad: {Cantidad:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Descuento solicitado: ${descuentoPorUnidad:F2}/unidad");

                // ✅ GUARDAR PRECIO ORIGINAL SI NO SE HA GUARDADO
                if (PrecioOriginal == 0)
                {
                    PrecioOriginal = PrecioUnitario;
                    System.Diagnostics.Debug.WriteLine($"   • Precio original guardado: ${PrecioOriginal:F2}");
                }

                // ✅ ESTABLECER INFORMACIÓN DE AUDITORÍA
                DescuentoUnitario = descuentoPorUnidad;
                MotivoDescuentoDetalle = motivo;
                TieneDescuentoManual = true;

                // ✅ CALCULAR NUEVO PRECIO UNITARIO (YA CON DESCUENTO INCLUIDO)
                var nuevoPrecio = Math.Max(0, PrecioOriginal - descuentoPorUnidad);
                PrecioUnitario = nuevoPrecio;

                // ✅ IMPORTANTE: NO usar DescuentoAplicado para evitar doble descuento
                // El descuento ya está reflejado en el PrecioUnitario reducido
                DescuentoAplicado = 0; // ✅ CLAVE: Limpiar para evitar doble descuento

                // ✅ RECALCULAR SUBTOTAL (ahora con precio ya descontado)
                CalcularSubTotal();

                System.Diagnostics.Debug.WriteLine($"   ✅ RESULTADO:");
                System.Diagnostics.Debug.WriteLine($"      • Precio final: ${PrecioUnitario:F2}");
                System.Diagnostics.Debug.WriteLine($"      • SubTotal: ${SubTotal:F2}");
                System.Diagnostics.Debug.WriteLine($"      • Total descuento línea: ${TotalDescuentoLinea:F2}");
                System.Diagnostics.Debug.WriteLine($"      • Ahorro total: ${(PrecioOriginal - PrecioUnitario) * Cantidad:F2}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error aplicando descuento: {ex.Message}");
                throw;
            }
        }
        public void RemoverDescuento()
        {
            if (TieneDescuentoManual && PrecioOriginal > 0)
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Removiendo descuento de {NombreProducto}");
                System.Diagnostics.Debug.WriteLine($"   • Precio con descuento: ${PrecioUnitario:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Precio original: ${PrecioOriginal:F2}");

                // ✅ RESTAURAR PRECIO ORIGINAL
                PrecioUnitario = PrecioOriginal;

                // ✅ LIMPIAR INFORMACIÓN DE DESCUENTO
                DescuentoUnitario = 0;
                DescuentoAplicado = 0; // ✅ IMPORTANTE: Limpiar también
                MotivoDescuentoDetalle = "";
                TieneDescuentoManual = false;

                // ✅ RECALCULAR
                CalcularSubTotal();

                System.Diagnostics.Debug.WriteLine($"   ✅ Precio restaurado: ${PrecioUnitario:F2}");
                System.Diagnostics.Debug.WriteLine($"   ✅ SubTotal: ${SubTotal:F2}");
            }
        }

        [NotMapped]
        public string ResumenDescuentoDetalle
        {
            get
            {
                if (!TieneDescuentoManual || DescuentoUnitario <= 0)
                    return "Sin descuento";

                var porcentaje = PrecioOriginal > 0 ? (DescuentoUnitario / PrecioOriginal) * 100 : 0;
                return $"${DescuentoUnitario:F2}/unidad ({porcentaje:F1}%) - {MotivoDescuentoDetalle}";
            }
        }

        public bool ValidarDescuento()
        {
            if (!TieneDescuentoManual) return true;

            // ✅ VALIDACIONES MEJORADAS
            if (PrecioOriginal <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Precio original inválido en {NombreProducto}: ${PrecioOriginal:F2}");
                return false;
            }

            if (DescuentoUnitario > PrecioOriginal)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Descuento mayor al precio original en {NombreProducto}");
                System.Diagnostics.Debug.WriteLine($"   • Precio original: ${PrecioOriginal:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Descuento: ${DescuentoUnitario:F2}");
                return false;
            }

            if (PrecioUnitario != (PrecioOriginal - DescuentoUnitario))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Inconsistencia en precios de {NombreProducto}");
                System.Diagnostics.Debug.WriteLine($"   • Precio original: ${PrecioOriginal:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Descuento: ${DescuentoUnitario:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Precio actual: ${PrecioUnitario:F2}");
                System.Diagnostics.Debug.WriteLine($"   • Esperado: ${PrecioOriginal - DescuentoUnitario:F2}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(MotivoDescuentoDetalle))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Falta motivo de descuento en {NombreProducto}");
                return false;
            }

            return true;
        }
        public string ObtenerInfoDebugDescuento()
        {
            if (!TieneDescuentoManual)
                return "Sin descuento aplicado";

            return $"DESCUENTO EN {NombreProducto}:\n" +
                   $"• Precio original: ${PrecioOriginal:F2}\n" +
                   $"• Descuento unitario: ${DescuentoUnitario:F2}\n" +
                   $"• Precio final: ${PrecioUnitario:F2}\n" +
                   $"• Cantidad: {Cantidad:F2}\n" +
                   $"• Total línea descuento: ${TotalDescuentoLinea:F2}\n" +
                   $"• SubTotal: ${SubTotal:F2}\n" +
                   $"• Motivo: {MotivoDescuentoDetalle}";
        }

        public void AplicarDescuentoPorcentaje(decimal porcentaje)
        {
            var valorSinDescuento = Cantidad * PrecioUnitario;
            DescuentoAplicado = valorSinDescuento * (porcentaje / 100);
        }

        public void ActualizarCantidad(decimal nuevaCantidad)
        {
            Cantidad = nuevaCantidad;
        }

        public void ActualizarPrecio(decimal nuevoPrecio)
        {
            PrecioUnitario = nuevoPrecio;
        }

        public bool ValidarDetalle()
        {
            return Cantidad > 0 &&
                   PrecioUnitario > 0 &&
                   !string.IsNullOrEmpty(NombreProducto) &&
                   !string.IsNullOrEmpty(UnidadMedida) &&
                   RawMaterialId > 0;
        }

        public string ObtenerDescripcionTicket()
        {
            var descuento = TieneDescuento ? $" (Desc: {DescuentoAplicado:C2})" : "";
            return $"{NombreProducto} - {Cantidad:F2} {UnidadMedida} × {PrecioUnitario:C2}{descuento}";
        }

        public void CopiarDatosProducto(RawMaterial producto)
        {
            RawMaterialId = producto.Id;
            NombreProducto = producto.NombreArticulo;
            UnidadMedida = producto.UnidadMedida;
            PrecioUnitario = producto.PrecioVentaFinal;
            CostoUnitario = producto.PrecioConIVA;
            PorcentajeIVA = producto.PorcentajeIVA;
        }

        // ===== IMPLEMENTACIÓN DE INotifyPropertyChanged =====

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }


        /// <summary>
        /// Indica si este detalle corresponde a un producto
        /// </summary>
        [NotMapped]
        public bool EsProducto => RawMaterialId.HasValue && RawMaterialId.Value > 0;

        /// <summary>
        /// Indica si este detalle corresponde a un servicio
        /// </summary>
        [NotMapped]
        public bool EsServicio => ServicioVentaId.HasValue && ServicioVentaId.Value > 0;

        /// <summary>
        /// Tipo de item para mostrar en reportes
        /// </summary>
        [NotMapped]
        public string TipoItem => EsServicio ? "Servicio" : "Producto";

        /// <summary>
        /// Descripción completa del item
        /// </summary>
        [NotMapped]
        public string DescripcionCompleta
        {
            get
            {
                if (EsServicio)
                    return $"🛍️ {NombreProducto} (Servicio)";
                else
                    return $"📦 {NombreProducto} (Producto)";
            }
        }

        /// <summary>
        /// ID del item (producto o servicio)
        /// </summary>
        [NotMapped]
        public int ItemId => EsServicio ? ServicioVentaId.Value : (RawMaterialId ?? 0);
    }
}
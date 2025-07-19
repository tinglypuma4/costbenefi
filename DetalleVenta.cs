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
        #endregion

        [Key]
        public int Id { get; set; }

        // ===== RELACIONES =====
        public int VentaId { get; set; }
        public virtual Venta Venta { get; set; }

        public int RawMaterialId { get; set; }
        public virtual RawMaterial RawMaterial { get; set; }

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
            var nuevoSubTotal = (Cantidad * PrecioUnitario) - DescuentoAplicado;
            if (nuevoSubTotal < 0) nuevoSubTotal = 0;

            if (Math.Abs(_subTotal - nuevoSubTotal) > 0.001m)
            {
                _subTotal = nuevoSubTotal;
                OnPropertyChanged(nameof(SubTotal));
            }
        }

        public void AplicarDescuento(decimal descuento)
        {
            DescuentoAplicado = descuento;
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
    }
}
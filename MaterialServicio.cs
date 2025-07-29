using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace costbenefi.Models
{
    /// <summary>
    /// Representa un material del inventario que es necesario para realizar un servicio
    /// Tabla intermedia entre ServicioVenta y RawMaterial
    /// Similar a DetalleVenta pero para servicios en lugar de ventas
    /// </summary>
    public class MaterialServicio : INotifyPropertyChanged
    {
        #region Campos privados para notificaciones
        private decimal _cantidadNecesaria;
        private decimal _costoUnitario;
        private decimal _porcentajeDesperdicio = 0;
        private string _observaciones = string.Empty;
        private bool _esOpcional = false;
        #endregion

        [Key]
        public int Id { get; set; }

        // ===== RELACIONES =====
        public int ServicioVentaId { get; set; }
        public virtual ServicioVenta ServicioVenta { get; set; }

        public int RawMaterialId { get; set; }
        public virtual RawMaterial RawMaterial { get; set; }

        // ===== DATOS DEL MATERIAL NECESARIO CON NOTIFICACIÓN =====

        /// <summary>
        /// Cantidad necesaria del material para realizar el servicio
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadNecesaria
        {
            get => _cantidadNecesaria;
            set
            {
                if (SetProperty(ref _cantidadNecesaria, value))
                {
                    OnPropertyChanged(nameof(CantidadConDesperdicio));
                    OnPropertyChanged(nameof(CostoTotal));
                    OnPropertyChanged(nameof(CostoTotalConDesperdicio));
                }
            }
        }

        /// <summary>
        /// Costo del material al momento de configurar el servicio
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoUnitario
        {
            get => _costoUnitario;
            set
            {
                if (SetProperty(ref _costoUnitario, value))
                {
                    OnPropertyChanged(nameof(CostoTotal));
                    OnPropertyChanged(nameof(CostoTotalConDesperdicio));
                }
            }
        }

        /// <summary>
        /// Unidad de medida para este material en el servicio
        /// </summary>
        [Required]
        [StringLength(20)]
        public string UnidadMedida { get; set; } = string.Empty;

        /// <summary>
        /// Porcentaje de desperdicio estimado (5% = 5.00)
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeDesperdicio
        {
            get => _porcentajeDesperdicio;
            set
            {
                if (SetProperty(ref _porcentajeDesperdicio, value))
                {
                    OnPropertyChanged(nameof(CantidadConDesperdicio));
                    OnPropertyChanged(nameof(CostoTotalConDesperdicio));
                }
            }
        }

        /// <summary>
        /// Indica si este material es opcional para el servicio
        /// </summary>
        public bool EsOpcional
        {
            get => _esOpcional;
            set => SetProperty(ref _esOpcional, value);
        }

        /// <summary>
        /// Orden de aplicación/uso en el servicio
        /// </summary>
        public int OrdenUso { get; set; } = 1;

        /// <summary>
        /// Tiempo aproximado de uso en minutos
        /// </summary>
        public int TiempoUsoMinutos { get; set; } = 0;

        /// <summary>
        /// Observaciones específicas para este material en el servicio
        /// </summary>
        [StringLength(500)]
        public string Observaciones
        {
            get => _observaciones;
            set => SetProperty(ref _observaciones, value);
        }

        /// <summary>
        /// Indica si debe verificarse disponibilidad antes de ofrecer el servicio
        /// </summary>
        public bool VerificarDisponibilidad { get; set; } = true;

        /// <summary>
        /// Cantidad mínima en inventario para poder ofrecer el servicio
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal StockMinimoRequerido { get; set; } = 0;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string UsuarioCreador { get; set; } = string.Empty;

        // ===== PROPIEDADES CALCULADAS =====

        /// <summary>
        /// Nombre del material para mostrar en interfaz
        /// </summary>
        [NotMapped]
        public string NombreMaterial => RawMaterial?.NombreArticulo ?? "Material no encontrado";

        /// <summary>
        /// Categoría del material
        /// </summary>
        [NotMapped]
        public string CategoriaMaterial => RawMaterial?.Categoria ?? "";

        /// <summary>
        /// Costo total de este material para el servicio (sin desperdicio)
        /// </summary>
        [NotMapped]
        public decimal CostoTotal => CantidadNecesaria * CostoUnitario;

        /// <summary>
        /// Cantidad necesaria incluyendo el porcentaje de desperdicio
        /// </summary>
        [NotMapped]
        public decimal CantidadConDesperdicio => CantidadNecesaria * (1 + (PorcentajeDesperdicio / 100));

        /// <summary>
        /// Costo total incluyendo desperdicio
        /// </summary>
        [NotMapped]
        public decimal CostoTotalConDesperdicio => CantidadConDesperdicio * CostoUnitario;

        /// <summary>
        /// Costo actual del material en inventario
        /// </summary>
        [NotMapped]
        public decimal CostoActualUnitario => RawMaterial?.PrecioConIVA ?? 0;

        /// <summary>
        /// Diferencia entre costo configurado y costo actual
        /// </summary>
        [NotMapped]
        public decimal DiferenciaCosto => CostoActualUnitario - CostoUnitario;

        /// <summary>
        /// Porcentaje de variación del costo
        /// </summary>
        [NotMapped]
        public decimal PorcentajeVariacionCosto
        {
            get
            {
                if (CostoUnitario <= 0) return 0;
                return ((CostoActualUnitario - CostoUnitario) / CostoUnitario) * 100;
            }
        }

        /// <summary>
        /// Stock disponible actual del material
        /// </summary>
        [NotMapped]
        public decimal StockDisponible => RawMaterial?.StockTotal ?? 0;

        /// <summary>
        /// Indica si hay stock suficiente para el servicio
        /// </summary>
        [NotMapped]
        public bool TieneStockSuficiente => StockDisponible >= CantidadConDesperdicio;

        /// <summary>
        /// Número máximo de servicios que se pueden realizar con el stock actual
        /// </summary>
        [NotMapped]
        public int ServiciosPosibles
        {
            get
            {
                if (CantidadConDesperdicio <= 0) return 0;
                return (int)(StockDisponible / CantidadConDesperdicio);
            }
        }

        /// <summary>
        /// Estado de disponibilidad del material
        /// </summary>
        [NotMapped]
        public string EstadoDisponibilidad
        {
            get
            {
                if (RawMaterial == null) return "Material no encontrado";
                if (RawMaterial.Eliminado) return "Material eliminado";
                if (!RawMaterial.ActivoParaVenta) return "Material inactivo";
                if (!TieneStockSuficiente) return "Stock insuficiente";
                if (StockDisponible <= StockMinimoRequerido) return "Stock crítico";
                return "Disponible";
            }
        }

        /// <summary>
        /// Indica si el material está disponible para el servicio
        /// </summary>
        [NotMapped]
        public bool EstaDisponible => RawMaterial != null &&
                                     !RawMaterial.Eliminado &&
                                     TieneStockSuficiente &&
                                     (EsOpcional || StockDisponible > StockMinimoRequerido);

        /// <summary>
        /// Tiempo de uso formatado
        /// </summary>
        [NotMapped]
        public string TiempoUsoFormateado
        {
            get
            {
                if (TiempoUsoMinutos <= 0) return "N/A";
                if (TiempoUsoMinutos < 60) return $"{TiempoUsoMinutos} min";
                var horas = TiempoUsoMinutos / 60;
                var minutos = TiempoUsoMinutos % 60;
                return minutos > 0 ? $"{horas}h {minutos}min" : $"{horas}h";
            }
        }

        // ===== MÉTODOS =====

        /// <summary>
        /// Actualiza el costo unitario con el precio actual del inventario
        /// </summary>
        public void ActualizarCostoActual()
        {
            if (RawMaterial != null)
            {
                CostoUnitario = RawMaterial.PrecioConIVA;
                FechaActualizacion = DateTime.Now;
            }
        }

        /// <summary>
        /// Copia información básica desde el material del inventario
        /// </summary>
        public void CopiarDatosDesdeInventario(RawMaterial material)
        {
            RawMaterialId = material.Id;
            RawMaterial = material;
            UnidadMedida = material.UnidadMedida;
            CostoUnitario = material.PrecioConIVA;
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Verifica si el material puede ser utilizado para el servicio
        /// </summary>
        public bool PuedeUtilizarse()
        {
            if (EsOpcional) return true;
            return EstaDisponible && TieneStockSuficiente;
        }

        /// <summary>
        /// Calcula cuántos servicios más se pueden realizar
        /// </summary>
        public int CalcularServiciosRestantes()
        {
            if (!VerificarDisponibilidad) return int.MaxValue;
            return ServiciosPosibles;
        }

        /// <summary>
        /// Reduce el stock del material cuando se ejecuta el servicio
        /// </summary>
        public bool ConsumirMaterial()
        {
            if (RawMaterial == null || !TieneStockSuficiente) return false;

            return RawMaterial.ReducirStock(CantidadConDesperdicio);
        }

        /// <summary>
        /// Valida que la configuración del material sea correcta
        /// </summary>
        public bool ValidarConfiguracion()
        {
            return RawMaterialId > 0 &&
                   CantidadNecesaria > 0 &&
                   CostoUnitario >= 0 &&
                   !string.IsNullOrEmpty(UnidadMedida) &&
                   PorcentajeDesperdicio >= 0 &&
                   PorcentajeDesperdicio <= 100;
        }

        /// <summary>
        /// Obtiene descripción detallada del material para el servicio
        /// </summary>
        public string ObtenerDescripcionDetallada()
        {
            var descripcion = $"{NombreMaterial}: {CantidadNecesaria:F2} {UnidadMedida}";

            if (PorcentajeDesperdicio > 0)
            {
                descripcion += $" (+{PorcentajeDesperdicio:F1}% desperdicio = {CantidadConDesperdicio:F2})";
            }

            if (EsOpcional)
            {
                descripcion += " (Opcional)";
            }

            return descripcion;
        }

        /// <summary>
        /// Obtiene información de costos para análisis
        /// </summary>
        public string ObtenerAnalisisCosto()
        {
            return $"ANÁLISIS DE COSTO - {NombreMaterial}\n\n" +
                   $"📦 CANTIDAD:\n" +
                   $"   • Necesaria: {CantidadNecesaria:F2} {UnidadMedida}\n" +
                   $"   • Desperdicio: {PorcentajeDesperdicio:F1}%\n" +
                   $"   • Total con desperdicio: {CantidadConDesperdicio:F2} {UnidadMedida}\n\n" +
                   $"💰 COSTOS:\n" +
                   $"   • Costo configurado: {CostoUnitario:C2}\n" +
                   $"   • Costo actual: {CostoActualUnitario:C2}\n" +
                   $"   • Variación: {DiferenciaCosto:C2} ({PorcentajeVariacionCosto:F1}%)\n" +
                   $"   • Costo total: {CostoTotalConDesperdicio:C2}\n\n" +
                   $"📊 DISPONIBILIDAD:\n" +
                   $"   • Stock actual: {StockDisponible:F2} {UnidadMedida}\n" +
                   $"   • Servicios posibles: {ServiciosPosibles}\n" +
                   $"   • Estado: {EstadoDisponibilidad}";
        }

        /// <summary>
        /// Obtiene resumen para mostrar en listas
        /// </summary>
        public string ObtenerResumen()
        {
            var resumen = $"{NombreMaterial} - {CantidadNecesaria:F2} {UnidadMedida} - {CostoTotal:C2}";

            if (EsOpcional) resumen += " (Opcional)";
            if (!TieneStockSuficiente) resumen += " ⚠️";

            return resumen;
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

        public override string ToString()
        {
            return $"{NombreMaterial} - {CantidadNecesaria:F2} {UnidadMedida}";
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace costbenefi.Models
{
    /// <summary>
    /// Representa un servicio que puede venderse en el POS
    /// Consume materiales del inventario y se integra con el sistema de ventas
    /// </summary>
    public class ServicioVenta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string NombreServicio { get; set; } = string.Empty;

        [StringLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CategoriaServicio { get; set; } = string.Empty;

        /// <summary>
        /// Precio base del servicio (sin IVA)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioBase { get; set; } = 0;

        /// <summary>
        /// Precio final del servicio (con IVA incluido)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioServicio { get; set; } = 0;

        /// <summary>
        /// Duración estimada del servicio
        /// </summary>
        [StringLength(50)]
        public string DuracionEstimada { get; set; } = "30 min";

        /// <summary>
        /// Costo estimado de materiales que consume el servicio
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoMateriales { get; set; } = 0;

        /// <summary>
        /// Costo estimado de mano de obra
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoManoObra { get; set; } = 0;

        /// <summary>
        /// Margen de ganancia objetivo en porcentaje
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal MargenObjetivo { get; set; } = 40;

        /// <summary>
        /// Porcentaje de IVA aplicable al servicio
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal PorcentajeIVA { get; set; } = 16;

        /// <summary>
        /// Indica si el servicio está activo para venta
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Indica si está integrado con el punto de venta
        /// </summary>
        public bool IntegradoPOS { get; set; } = false;

        /// <summary>
        /// Prioridad para mostrar en POS (1 = más alta prioridad)
        /// </summary>
        public int PrioridadPOS { get; set; } = 100;

        /// <summary>
        /// Requiere confirmación especial antes de vender
        /// </summary>
        public bool RequiereConfirmacion { get; set; } = false;

        /// <summary>
        /// Número máximo de veces que puede venderse por día
        /// </summary>
        public int LimiteDiario { get; set; } = 0; // 0 = sin límite

        /// <summary>
        /// Stock virtual para servicios limitados
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal StockDisponible { get; set; } = 99999999;

        /// <summary>
        /// Código para identificar rápidamente el servicio
        /// </summary>
        [StringLength(50)]
        public string CodigoServicio { get; set; } = string.Empty;

        [StringLength(500)]
        public string Observaciones { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string UsuarioCreador { get; set; } = string.Empty;

        // ===== CAMPOS PARA ELIMINACIÓN LÓGICA =====
        public bool Eliminado { get; set; } = false;
        public DateTime? FechaEliminacion { get; set; }
        [StringLength(100)]
        public string? UsuarioEliminacion { get; set; }
        [StringLength(500)]
        public string? MotivoEliminacion { get; set; }

        // ===== NAVEGACIÓN =====
        public virtual ICollection<MaterialServicio> MaterialesNecesarios { get; set; } = new List<MaterialServicio>();

        // ===== PROPIEDADES CALCULADAS =====

        /// <summary>
        /// Indica si el servicio está disponible para venta
        /// </summary>
        [NotMapped]
        public bool DisponibleParaVenta => Activo && !Eliminado && StockDisponible > 0;

        /// <summary>
        /// Costo total del servicio (materiales + mano de obra)
        /// </summary>
        [NotMapped]
        public decimal CostoTotal => CostoMateriales + CostoManoObra;

        /// <summary>
        /// Ganancia estimada por servicio
        /// </summary>
        [NotMapped]
        public decimal GananciaEstimada => PrecioServicio - CostoTotal;

        /// <summary>
        /// Margen real de ganancia en porcentaje
        /// </summary>
        [NotMapped]
        public decimal MargenReal
        {
            get
            {
                if (PrecioServicio <= 0) return 0;
                return ((PrecioServicio - CostoTotal) / PrecioServicio) * 100;
            }
        }

        /// <summary>
        /// Precio sugerido basado en costos + margen objetivo
        /// </summary>
        [NotMapped]
        public decimal PrecioSugerido => CostoTotal * (1 + (MargenObjetivo / 100));

        /// <summary>
        /// IVA calculado sobre el precio del servicio
        /// </summary>
        [NotMapped]
        public decimal IVACalculado => PrecioBase * (PorcentajeIVA / 100);

        /// <summary>
        /// Precio con IVA incluido
        /// </summary>
        [NotMapped]
        public decimal PrecioConIVA => PrecioBase + IVACalculado;

        /// <summary>
        /// Estado del servicio para mostrar en interfaz
        /// </summary>
        [NotMapped]
        public string EstadoServicio
        {
            get
            {
                if (Eliminado) return "Eliminado";
                if (!Activo) return "Inactivo";
                if (StockDisponible <= 0) return "Agotado";
                if (!IntegradoPOS) return "No integrado";
                return "Disponible";
            }
        }

        /// <summary>
        /// Indica si está activo y no eliminado
        /// </summary>
        [NotMapped]
        public bool EstaActivo => !Eliminado && Activo;

        /// <summary>
        /// Costo promedio por minuto (basado en duración)
        /// </summary>
        [NotMapped]
        public decimal CostoPorMinuto
        {
            get
            {
                var minutos = ExtraerMinutosDeDuracion();
                return minutos > 0 ? CostoTotal / minutos : 0;
            }
        }

        /// <summary>
        /// Ganancia por minuto
        /// </summary>
        [NotMapped]
        public decimal GananciaPorMinuto
        {
            get
            {
                var minutos = ExtraerMinutosDeDuracion();
                return minutos > 0 ? GananciaEstimada / minutos : 0;
            }
        }

        // ===== MÉTODOS =====

        /// <summary>
        /// Calcula el precio sugerido basado en costos y margen
        /// </summary>
        public void CalcularPrecioSugerido()
        {
            PrecioBase = CostoTotal * (1 + (MargenObjetivo / 100));
            PrecioServicio = PrecioBase * (1 + (PorcentajeIVA / 100));
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Actualiza el costo de materiales sumando todos los materiales necesarios
        /// </summary>
        public void ActualizarCostoMateriales()
        {
            CostoMateriales = MaterialesNecesarios?.Sum(m => m.CostoTotal) ?? 0;
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Configura el servicio para su integración con POS
        /// </summary>
        public void ConfigurarParaPOS(bool activar = true)
        {
            IntegradoPOS = activar;
            if (activar && string.IsNullOrEmpty(CodigoServicio))
            {
                CodigoServicio = GenerarCodigoServicio();
            }
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Reduce el stock disponible (para servicios limitados)
        /// </summary>
        public bool ReducirStock(decimal cantidad = 1)
        {
            if (StockDisponible >= cantidad)
            {
                StockDisponible -= cantidad;
                FechaActualizacion = DateTime.Now;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Agrega un material necesario para el servicio
        /// </summary>
        public void AgregarMaterial(int rawMaterialId, decimal cantidad, string unidad, decimal costoUnitario)
        {
            var material = new MaterialServicio
            {
                ServicioVentaId = this.Id,
                RawMaterialId = rawMaterialId,
                CantidadNecesaria = cantidad,
                UnidadMedida = unidad,
                CostoUnitario = costoUnitario
            };

            MaterialesNecesarios.Add(material);
            ActualizarCostoMateriales();
        }

        /// <summary>
        /// Valida que el servicio esté correctamente configurado
        /// </summary>
        public bool ValidarConfiguracion()
        {
            return !string.IsNullOrEmpty(NombreServicio) &&
                   !string.IsNullOrEmpty(CategoriaServicio) &&
                   PrecioServicio > 0 &&
                   !string.IsNullOrEmpty(DuracionEstimada);
        }

        /// <summary>
        /// Genera código automático para el servicio
        /// </summary>
        public string GenerarCodigoServicio()
        {
            var categoria = CategoriaServicio.Length >= 3 ? CategoriaServicio.Substring(0, 3).ToUpper() : CategoriaServicio.ToUpper();
            var nombre = NombreServicio.Length >= 3 ? NombreServicio.Substring(0, 3).ToUpper() : NombreServicio.ToUpper();
            return $"{categoria}{nombre}{Id:000}";
        }

        /// <summary>
        /// Extrae los minutos de la duración estimada
        /// </summary>
        private int ExtraerMinutosDeDuracion()
        {
            try
            {
                var duracion = DuracionEstimada.ToLower().Replace(" ", "");

                if (duracion.Contains("hora") || duracion.Contains("hr"))
                {
                    var horas = ExtraerNumero(duracion);
                    return (int)(horas * 60);
                }
                else if (duracion.Contains("min"))
                {
                    return (int)ExtraerNumero(duracion);
                }
                else
                {
                    // Asumir que es en minutos si no se especifica
                    return (int)ExtraerNumero(duracion);
                }
            }
            catch
            {
                return 30; // Valor por defecto
            }
        }

        /// <summary>
        /// Extrae el primer número de una cadena
        /// </summary>
        private decimal ExtraerNumero(string texto)
        {
            var numeros = new string(texto.Where(c => char.IsDigit(c) || c == '.').ToArray());
            return decimal.TryParse(numeros, out var resultado) ? resultado : 30;
        }

        /// <summary>
        /// Marca el servicio como eliminado
        /// </summary>
        public void MarcarComoEliminado(string usuario, string motivo = "Eliminación manual")
        {
            Eliminado = true;
            FechaEliminacion = DateTime.Now;
            UsuarioEliminacion = usuario;
            MotivoEliminacion = motivo;
            Activo = false; // También desactivar
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Restaura un servicio eliminado
        /// </summary>
        public void Restaurar(string usuario)
        {
            Eliminado = false;
            FechaEliminacion = null;
            UsuarioEliminacion = null;
            MotivoEliminacion = null;
            FechaActualizacion = DateTime.Now;
            Observaciones += $"\n[{DateTime.Now:yyyy-MM-dd HH:mm}] Servicio restaurado por {usuario}";
        }

        /// <summary>
        /// Obtiene información del estado de eliminación
        /// </summary>
        [NotMapped]
        public string EstadoEliminacion => Eliminado
            ? $"Eliminado el {FechaEliminacion:dd/MM/yyyy} por {UsuarioEliminacion}"
            : "Activo";

        /// <summary>
        /// Obtiene resumen del servicio para mostrar
        /// </summary>
        public string ObtenerResumen()
        {
            return $"{NombreServicio} - {PrecioServicio:C2} ({DuracionEstimada}) - {CategoriaServicio}";
        }

        /// <summary>
        /// Obtiene análisis financiero del servicio
        /// </summary>
        public string ObtenerAnalisisFinanciero()
        {
            return $"ANÁLISIS FINANCIERO - {NombreServicio}\n\n" +
                   $"💰 COSTOS:\n" +
                   $"   • Materiales: {CostoMateriales:C2}\n" +
                   $"   • Mano de obra: {CostoManoObra:C2}\n" +
                   $"   • Costo total: {CostoTotal:C2}\n\n" +
                   $"📈 INGRESOS:\n" +
                   $"   • Precio base: {PrecioBase:C2}\n" +
                   $"   • IVA ({PorcentajeIVA:F1}%): {IVACalculado:C2}\n" +
                   $"   • Precio final: {PrecioServicio:C2}\n\n" +
                   $"📊 RENTABILIDAD:\n" +
                   $"   • Ganancia: {GananciaEstimada:C2}\n" +
                   $"   • Margen real: {MargenReal:F2}%\n" +
                   $"   • Margen objetivo: {MargenObjetivo:F2}%\n" +
                   $"   • Ganancia/minuto: {GananciaPorMinuto:C2}\n\n" +
                   $"⏱️ TIEMPO:\n" +
                   $"   • Duración: {DuracionEstimada}\n" +
                   $"   • Costo/minuto: {CostoPorMinuto:C2}";
        }

        public override string ToString()
        {
            return $"{NombreServicio} ({CategoriaServicio}) - {PrecioServicio:C2}";
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace costbenefi.Models
{
    /// <summary>
    /// Representa una promoción o descuento que puede aplicarse en ventas
    /// Maneja diferentes tipos de promociones, condiciones y límites de uso
    /// </summary>
    public class PromocionVenta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string NombrePromocion { get; set; } = string.Empty;

        [StringLength(500)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string TipoPromocion { get; set; } = "DescuentoPorcentaje"; // DescuentoPorcentaje, DescuentoFijo, Combo, Cantidad

        [Required]
        [StringLength(100)]
        public string CategoriaPromocion { get; set; } = "General";

        /// <summary>
        /// Valor de la promoción (porcentaje para descuentos %, monto para descuentos fijos)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal ValorPromocion { get; set; } = 0;

        /// <summary>
        /// Descuento máximo aplicable (para promociones por porcentaje)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal DescuentoMaximo { get; set; } = 0;

        /// <summary>
        /// Monto mínimo de compra para aplicar la promoción
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal MontoMinimo { get; set; } = 0;

        /// <summary>
        /// Cantidad mínima de productos para aplicar promoción
        /// </summary>
        public int CantidadMinima { get; set; } = 1;

        /// <summary>
        /// Fecha de inicio de vigencia
        /// </summary>
        public DateTime FechaInicio { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de fin de vigencia
        /// </summary>
        public DateTime FechaFin { get; set; } = DateTime.Now.AddDays(30);

        /// <summary>
        /// Indica si la promoción está activa
        /// </summary>
        public bool Activa { get; set; } = true;

        /// <summary>
        /// Indica si se aplica automáticamente cuando se cumplen condiciones
        /// </summary>
        public bool AplicacionAutomatica { get; set; } = true;

        /// <summary>
        /// Indica si está integrada con el punto de venta
        /// </summary>
        public bool IntegradaPOS { get; set; } = false;

        /// <summary>
        /// Prioridad de aplicación (1 = más alta prioridad)
        /// </summary>
        public int Prioridad { get; set; } = 100;

        /// <summary>
        /// Número máximo de usos por cliente por día
        /// </summary>
        public int LimitePorCliente { get; set; } = 0; // 0 = sin límite

        /// <summary>
        /// Número máximo de usos totales de la promoción
        /// </summary>
        public int LimiteUsoTotal { get; set; } = 0; // 0 = sin límite

        /// <summary>
        /// Número de veces que se ha usado la promoción
        /// </summary>
        public int VecesUsada { get; set; } = 0;

        /// <summary>
        /// Código único para identificar la promoción
        /// </summary>
        [StringLength(50)]
        public string CodigoPromocion { get; set; } = string.Empty;

        /// <summary>
        /// Productos específicos a los que aplica (separados por comas, vacío = todos)
        /// </summary>
        [StringLength(1000)]
        public string ProductosAplicables { get; set; } = string.Empty;

        /// <summary>
        /// Servicios específicos a los que aplica (separados por comas, vacío = todos)
        /// </summary>
        [StringLength(1000)]
        public string ServiciosAplicables { get; set; } = string.Empty;

        /// <summary>
        /// Categorías de productos a las que aplica (separadas por comas, vacío = todas)
        /// </summary>
        [StringLength(500)]
        public string CategoriasAplicables { get; set; } = string.Empty;

        /// <summary>
        /// Días de la semana en que aplica (L,M,Mi,J,V,S,D separados por comas)
        /// </summary>
        [StringLength(50)]
        public string DiasAplicables { get; set; } = "L,M,Mi,J,V,S,D"; // Todos los días por defecto

        /// <summary>
        /// Hora de inicio de aplicación (formato HH:mm)
        /// </summary>
        [StringLength(5)]
        public string HoraInicio { get; set; } = "00:00";

        /// <summary>
        /// Hora de fin de aplicación (formato HH:mm)
        /// </summary>
        [StringLength(5)]
        public string HoraFin { get; set; } = "23:59";

        /// <summary>
        /// Requiere código especial para aplicar
        /// </summary>
        public bool RequiereCodigo { get; set; } = false;

        /// <summary>
        /// Se puede combinar con otras promociones
        /// </summary>
        public bool Combinable { get; set; } = false;

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

        // ===== PROPIEDADES CALCULADAS =====

        /// <summary>
        /// Indica si la promoción está vigente en este momento
        /// </summary>
        [NotMapped]
        public bool EstaVigente
        {
            get
            {
                var ahora = DateTime.Now;
                return Activa && !Eliminado &&
                       ahora >= FechaInicio &&
                       ahora <= FechaFin &&
                       (LimiteUsoTotal == 0 || VecesUsada < LimiteUsoTotal);
            }
        }

        /// <summary>
        /// Indica si aplica en este día y hora
        /// </summary>
        [NotMapped]
        public bool AplicaAhora
        {
            get
            {
                if (!EstaVigente) return false;

                var ahora = DateTime.Now;
                var diaActual = ObtenerCodigoDia(ahora.DayOfWeek);
                var horaActual = ahora.ToString("HH:mm");

                return DiasAplicables.Contains(diaActual) &&
                       string.Compare(horaActual, HoraInicio) >= 0 &&
                       string.Compare(horaActual, HoraFin) <= 0;
            }
        }

        /// <summary>
        /// Indica si está disponible para uso (vigente + límites)
        /// </summary>
        [NotMapped]
        public bool DisponibleParaUso => EstaVigente && AplicaAhora;

        /// <summary>
        /// Días restantes de vigencia
        /// </summary>
        [NotMapped]
        public int DiasRestantes => Math.Max(0, (FechaFin - DateTime.Now).Days);

        /// <summary>
        /// Porcentaje de uso de la promoción
        /// </summary>
        [NotMapped]
        public decimal PorcentajeUso
        {
            get
            {
                if (LimiteUsoTotal <= 0) return 0;
                return (decimal)VecesUsada / LimiteUsoTotal * 100;
            }
        }

        /// <summary>
        /// Usos restantes de la promoción
        /// </summary>
        [NotMapped]
        public int UsosRestantes => LimiteUsoTotal > 0 ? Math.Max(0, LimiteUsoTotal - VecesUsada) : int.MaxValue;

        /// <summary>
        /// Estado de la promoción para mostrar en interfaz
        /// </summary>
        [NotMapped]
        public string EstadoPromocion
        {
            get
            {
                if (Eliminado) return "Eliminada";
                if (!Activa) return "Inactiva";
                if (DateTime.Now < FechaInicio) return "Programada";
                if (DateTime.Now > FechaFin) return "Vencida";
                if (LimiteUsoTotal > 0 && VecesUsada >= LimiteUsoTotal) return "Agotada";
                if (!AplicaAhora) return "Fuera de horario";
                return "Vigente";
            }
        }

        /// <summary>
        /// Indica si está activa y no eliminada
        /// </summary>
        [NotMapped]
        public bool EstaActiva => !Eliminado && Activa;

        /// <summary>
        /// Descripción del tipo de promoción
        /// </summary>
        [NotMapped]
        public string DescripcionTipo
        {
            get
            {
                return TipoPromocion switch
                {
                    "DescuentoPorcentaje" => $"Descuento {ValorPromocion:F1}%",
                    "DescuentoFijo" => $"Descuento ${ValorPromocion:F2}",
                    "Combo" => $"Combo especial",
                    "Cantidad" => $"{CantidadMinima} por ${ValorPromocion:F2}",
                    "CompraYLleva" => $"Compra {CantidadMinima} y lleva {ValorPromocion}",
                    _ => TipoPromocion
                };
            }
        }

        /// <summary>
        /// Efectividad de la promoción (usos vs tiempo transcurrido)
        /// </summary>
        [NotMapped]
        public decimal Efectividad
        {
            get
            {
                var diasTranscurridos = Math.Max(1, (DateTime.Now - FechaInicio).Days);
                return VecesUsada / (decimal)diasTranscurridos;
            }
        }

        // ===== MÉTODOS =====

        /// <summary>
        /// Calcula el descuento a aplicar sobre un monto específico
        /// </summary>
        public decimal CalcularDescuento(decimal montoBase, int cantidad = 1)
        {
            if (!DisponibleParaUso || montoBase < MontoMinimo || cantidad < CantidadMinima)
                return 0;

            return TipoPromocion switch
            {
                "DescuentoPorcentaje" => Math.Min(montoBase * (ValorPromocion / 100), DescuentoMaximo > 0 ? DescuentoMaximo : decimal.MaxValue),
                "DescuentoFijo" => Math.Min(ValorPromocion, montoBase),
                "Combo" => ValorPromocion, // Precio especial del combo
                "Cantidad" => cantidad >= CantidadMinima ? montoBase - (ValorPromocion * (cantidad / CantidadMinima)) : 0,
                _ => 0
            };
        }

        /// <summary>
        /// Verifica si la promoción aplica a un producto específico
        /// </summary>
        public bool AplicaAProducto(int productId, string categoria = "")
        {
            if (!DisponibleParaUso) return false;

            // Si no hay restricciones específicas, aplica a todos
            if (string.IsNullOrEmpty(ProductosAplicables) && string.IsNullOrEmpty(CategoriasAplicables))
                return true;

            // Verificar productos específicos
            if (!string.IsNullOrEmpty(ProductosAplicables))
            {
                var productos = ProductosAplicables.Split(',').Select(p => p.Trim());
                if (productos.Contains(productId.ToString()))
                    return true;
            }

            // Verificar categorías
            if (!string.IsNullOrEmpty(CategoriasAplicables) && !string.IsNullOrEmpty(categoria))
            {
                var categorias = CategoriasAplicables.Split(',').Select(c => c.Trim().ToLower());
                if (categorias.Contains(categoria.ToLower()))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Verifica si la promoción aplica a un servicio específico
        /// </summary>
        public bool AplicaAServicio(int servicioId, string categoria = "")
        {
            if (!DisponibleParaUso) return false;

            // Si no hay restricciones específicas, aplica a todos
            if (string.IsNullOrEmpty(ServiciosAplicables) && string.IsNullOrEmpty(CategoriasAplicables))
                return true;

            // Verificar servicios específicos
            if (!string.IsNullOrEmpty(ServiciosAplicables))
            {
                var servicios = ServiciosAplicables.Split(',').Select(s => s.Trim());
                if (servicios.Contains(servicioId.ToString()))
                    return true;
            }

            // Verificar categorías
            if (!string.IsNullOrEmpty(CategoriasAplicables) && !string.IsNullOrEmpty(categoria))
            {
                var categorias = CategoriasAplicables.Split(',').Select(c => c.Trim().ToLower());
                if (categorias.Contains(categoria.ToLower()))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Registra el uso de la promoción
        /// </summary>
        public bool RegistrarUso()
        {
            if (!DisponibleParaUso) return false;

            VecesUsada++;
            FechaActualizacion = DateTime.Now;
            return true;
        }

        /// <summary>
        /// Genera código automático para la promoción
        /// </summary>
        public string GenerarCodigoPromocion()
        {
            var tipo = TipoPromocion.Length >= 3 ? TipoPromocion.Substring(0, 3).ToUpper() : TipoPromocion.ToUpper();
            var nombre = NombrePromocion.Length >= 3 ? NombrePromocion.Substring(0, 3).ToUpper() : NombrePromocion.ToUpper();
            return $"{tipo}{nombre}{Id:000}";
        }

        /// <summary>
        /// Configura la promoción para su integración con POS
        /// </summary>
        public void ConfigurarParaPOS(bool activar = true)
        {
            IntegradaPOS = activar;
            if (activar && string.IsNullOrEmpty(CodigoPromocion))
            {
                CodigoPromocion = GenerarCodigoPromocion();
            }
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Obtiene el código del día de la semana
        /// </summary>
        private string ObtenerCodigoDia(DayOfWeek dia)
        {
            return dia switch
            {
                DayOfWeek.Monday => "L",
                DayOfWeek.Tuesday => "M",
                DayOfWeek.Wednesday => "Mi",
                DayOfWeek.Thursday => "J",
                DayOfWeek.Friday => "V",
                DayOfWeek.Saturday => "S",
                DayOfWeek.Sunday => "D",
                _ => ""
            };
        }

        /// <summary>
        /// Valida que la promoción esté correctamente configurada
        /// </summary>
        public bool ValidarConfiguracion()
        {
            return !string.IsNullOrEmpty(NombrePromocion) &&
                   !string.IsNullOrEmpty(TipoPromocion) &&
                   ValorPromocion > 0 &&
                   FechaInicio <= FechaFin &&
                   CantidadMinima > 0;
        }

        /// <summary>
        /// Duplica la promoción con fechas nuevas
        /// </summary>
        public PromocionVenta Duplicar(string nuevoNombre, DateTime nuevaFechaInicio, DateTime nuevaFechaFin)
        {
            return new PromocionVenta
            {
                NombrePromocion = nuevoNombre,
                Descripcion = Descripcion,
                TipoPromocion = TipoPromocion,
                CategoriaPromocion = CategoriaPromocion,
                ValorPromocion = ValorPromocion,
                DescuentoMaximo = DescuentoMaximo,
                MontoMinimo = MontoMinimo,
                CantidadMinima = CantidadMinima,
                FechaInicio = nuevaFechaInicio,
                FechaFin = nuevaFechaFin,
                ProductosAplicables = ProductosAplicables,
                ServiciosAplicables = ServiciosAplicables,
                CategoriasAplicables = CategoriasAplicables,
                DiasAplicables = DiasAplicables,
                HoraInicio = HoraInicio,
                HoraFin = HoraFin,
                AplicacionAutomatica = AplicacionAutomatica,
                Combinable = Combinable
            };
        }

        /// <summary>
        /// Marca la promoción como eliminada
        /// </summary>
        public void MarcarComoEliminado(string usuario, string motivo = "Eliminación manual")
        {
            Eliminado = true;
            FechaEliminacion = DateTime.Now;
            UsuarioEliminacion = usuario;
            MotivoEliminacion = motivo;
            Activa = false; // También desactivar
            FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Restaura una promoción eliminada
        /// </summary>
        public void Restaurar(string usuario)
        {
            Eliminado = false;
            FechaEliminacion = null;
            UsuarioEliminacion = null;
            MotivoEliminacion = null;
            FechaActualizacion = DateTime.Now;
            Observaciones += $"\n[{DateTime.Now:yyyy-MM-dd HH:mm}] Promoción restaurada por {usuario}";
        }

        /// <summary>
        /// Obtiene información del estado de eliminación
        /// </summary>
        [NotMapped]
        public string EstadoEliminacion => Eliminado
            ? $"Eliminado el {FechaEliminacion:dd/MM/yyyy} por {UsuarioEliminacion}"
            : "Activo";

        /// <summary>
        /// Obtiene resumen de la promoción para mostrar
        /// </summary>
        public string ObtenerResumen()
        {
            return $"{NombrePromocion} - {DescripcionTipo} - {EstadoPromocion}";
        }

        /// <summary>
        /// Obtiene análisis de efectividad de la promoción
        /// </summary>
        public string ObtenerAnalisisEfectividad()
        {
            return $"ANÁLISIS DE EFECTIVIDAD - {NombrePromocion}\n\n" +
                   $"📊 USO:\n" +
                   $"   • Total usado: {VecesUsada} veces\n" +
                   $"   • Límite: {(LimiteUsoTotal > 0 ? LimiteUsoTotal.ToString() : "Sin límite")}\n" +
                   $"   • Restantes: {(UsosRestantes == int.MaxValue ? "Ilimitado" : UsosRestantes.ToString())}\n" +
                   $"   • Porcentaje uso: {PorcentajeUso:F1}%\n\n" +
                   $"⏰ VIGENCIA:\n" +
                   $"   • Inicio: {FechaInicio:dd/MM/yyyy}\n" +
                   $"   • Fin: {FechaFin:dd/MM/yyyy}\n" +
                   $"   • Días restantes: {DiasRestantes}\n" +
                   $"   • Horario: {HoraInicio} - {HoraFin}\n" +
                   $"   • Días: {DiasAplicables}\n\n" +
                   $"📈 EFECTIVIDAD:\n" +
                   $"   • Usos por día: {Efectividad:F2}\n" +
                   $"   • Estado: {EstadoPromocion}\n" +
                   $"   • Aplicación: {(AplicacionAutomatica ? "Automática" : "Manual")}\n" +
                   $"   • Combinable: {(Combinable ? "Sí" : "No")}";
        }

        public override string ToString()
        {
            return $"{NombrePromocion} ({DescripcionTipo}) - {EstadoPromocion}";
        }
    }
}
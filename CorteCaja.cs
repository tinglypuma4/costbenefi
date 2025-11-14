using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Collections.Generic;

namespace costbenefi.Models
{
    /// <summary>
    /// Modelo para gestión de cortes de caja diarios
    /// Integrado con el sistema de ventas POS existente
    /// </summary>
    public class CorteCaja
    {
        [Key]
        public int Id { get; set; }

        // ===== INFORMACIÓN BÁSICA DEL CORTE =====

        [Required]
        public DateTime FechaCorte { get; set; } = DateTime.Today;

        [Required]
        public DateTime FechaHoraCorte { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string UsuarioCorte { get; set; } = Environment.UserName;

        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Completado, Cancelado

        // ===== TOTALES CALCULADOS AUTOMÁTICAMENTE (SISTEMA) =====

        /// <summary>Total de ventas del día</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalVentasCalculado { get; set; }

        /// <summary>Cantidad de tickets/ventas del día</summary>
        public int CantidadTickets { get; set; }

        /// <summary>Total en efectivo según sistema</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal EfectivoCalculado { get; set; }

        /// <summary>Total en tarjetas según sistema</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal TarjetaCalculado { get; set; }

        /// <summary>Total en transferencias según sistema</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal TransferenciaCalculado { get; set; }

        /// <summary>Total de comisiones base del día</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal ComisionesCalculadas { get; set; }

        /// <summary>Total de IVA sobre comisiones del día</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal IVAComisionesCalculado { get; set; }

        /// <summary>Total de comisiones (base + IVA) del día</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal ComisionesTotalesCalculadas { get; set; }

        /// <summary>Ganancia bruta total del día</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal GananciaBrutaCalculada { get; set; }

        /// <summary>Ganancia neta total del día (después de comisiones)</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal GananciaNetaCalculada { get; set; }

        // ===== GASTOS DEL DÍA (Calculados desde Movimientos - No persistidos) =====

        /// <summary>Total de gastos del día (calculado desde Movimientos)</summary>
        [NotMapped]
        public decimal GastosTotalesCalculados { get; set; }

        /// <summary>Ganancia neta real después de gastos</summary>
        [NotMapped]
        public decimal GananciaNetaFinal => GananciaNetaCalculada - GastosTotalesCalculados;

        /// <summary>Efectivo real disponible considerando gastos</summary>
        [NotMapped]
        public decimal EfectivoRealDisponible => EfectivoCalculado - GastosTotalesCalculados;

        // ===== CONTEO FÍSICO MANUAL (USUARIO) =====

        /// <summary>Efectivo contado físicamente en caja</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal EfectivoContado { get; set; }

        /// <summary>Fondo de caja del día anterior</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal FondoCajaInicial { get; set; } = 1000; // Configurable

        /// <summary>Fondo de caja para el día siguiente</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal FondoCajaSiguiente { get; set; } = 1000; // Configurable

        // ===== CONCILIACIÓN Y DIFERENCIAS =====

        /// <summary>Efectivo esperado (calculado + fondo inicial)</summary>
        [NotMapped]
        public decimal EfectivoEsperado => EfectivoCalculado + FondoCajaInicial;

        /// <summary>Diferencia entre contado y esperado</summary>
        [NotMapped]
        public decimal DiferenciaEfectivo => EfectivoContado - EfectivoEsperado;

        /// <summary>Efectivo para depositar (contado - fondo siguiente)</summary>
        [NotMapped]
        public decimal EfectivoParaDepositar => EfectivoContado - FondoCajaSiguiente;

        /// <summary>¿Hay sobrante de efectivo?</summary>
        [NotMapped]
        public bool TieneSobrante => DiferenciaEfectivo > 0;

        /// <summary>¿Hay faltante de efectivo?</summary>
        [NotMapped]
        public bool TieneFaltante => DiferenciaEfectivo < 0;

        /// <summary>¿La diferencia está dentro del margen aceptable?</summary>
        [NotMapped]
        public bool DiferenciaAceptable => Math.Abs(DiferenciaEfectivo) <= 10; // $10 pesos margen

        // ===== INFORMACIÓN ADICIONAL =====

        [StringLength(1000)]
        public string Observaciones { get; set; } = "";

        [StringLength(500)]
        public string MotivoSobrante { get; set; } = "";

        [StringLength(500)]
        public string MotivoFaltante { get; set; } = "";

        /// <summary>¿Se realizó depósito bancario?</summary>
        public bool DepositoRealizado { get; set; } = false;

        /// <summary>Referencia del depósito bancario</summary>
        [StringLength(100)]
        public string ReferenciaDeposito { get; set; } = "";

        [Column(TypeName = "decimal(18,4)")]
        public decimal MontoDepositado { get; set; }

        // ===== AUDITORÍA =====
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        // ===== NAVEGACIÓN =====
        public virtual ICollection<Venta> VentasDelDia { get; set; } = new List<Venta>();

        // ===== MÉTODOS DE NEGOCIO =====

        /// <summary>
        /// Calcula todos los totales basándose en las ventas del día
        /// </summary>
        public void CalcularTotalesAutomaticos(ICollection<Venta> ventasDelDia)
        {
            if (ventasDelDia?.Any() != true)
            {
                ResetearCalculos();
                return;
            }

            // Filtrar solo ventas completadas
            var ventasCompletadas = ventasDelDia.Where(v => v.Estado == "Completada").ToList();

            // Totales generales
            TotalVentasCalculado = ventasCompletadas.Sum(v => v.Total);
            CantidadTickets = ventasCompletadas.Count;

            // Totales por forma de pago
            EfectivoCalculado = ventasCompletadas.Sum(v => v.MontoEfectivo);
            TarjetaCalculado = ventasCompletadas.Sum(v => v.MontoTarjeta);
            TransferenciaCalculado = ventasCompletadas.Sum(v => v.MontoTransferencia);

            // Totales de comisiones
            ComisionesCalculadas = ventasCompletadas.Sum(v => v.ComisionTarjeta);
            IVAComisionesCalculado = ventasCompletadas.Sum(v => v.IVAComision);
            ComisionesTotalesCalculadas = ventasCompletadas.Sum(v => v.ComisionTotal);

            // Totales de rentabilidad
            GananciaBrutaCalculada = ventasCompletadas.Sum(v => v.GananciaBruta);
            GananciaNetaCalculada = ventasCompletadas.Sum(v => v.GananciaNeta);

            // Asignar ventas para navegación
            VentasDelDia = ventasCompletadas;
        }

        /// <summary>
        /// Resetea todos los cálculos a cero
        /// </summary>
        private void ResetearCalculos()
        {
            TotalVentasCalculado = 0;
            CantidadTickets = 0;
            EfectivoCalculado = 0;
            TarjetaCalculado = 0;
            TransferenciaCalculado = 0;
            ComisionesCalculadas = 0;
            IVAComisionesCalculado = 0;
            ComisionesTotalesCalculadas = 0;
            GananciaBrutaCalculada = 0;
            GananciaNetaCalculada = 0;
        }

        /// <summary>
        /// Establece el conteo físico y calcula diferencias
        /// </summary>
        public void EstablecerConteoFisico(decimal efectivoContado, decimal fondoInicial = 1000)
        {
            EfectivoContado = efectivoContado;
            FondoCajaInicial = fondoInicial;
        }

        /// <summary>
        /// Completa el corte de caja
        /// </summary>
        public void CompletarCorte(string observaciones = "", bool depositoRealizado = false,
                                  string referenciaDeposito = "", decimal montoDepositado = 0)
        {
            Estado = "Completado";
            FechaHoraCorte = DateTime.Now;
            Observaciones = observaciones;
            DepositoRealizado = depositoRealizado;
            ReferenciaDeposito = referenciaDeposito;
            MontoDepositado = montoDepositado;
        }

        /// <summary>
        /// Cancela el corte de caja
        /// </summary>
        public void CancelarCorte(string motivo)
        {
            Estado = "Cancelado";
            Observaciones = $"CANCELADO: {motivo}";
        }

        /// <summary>
        /// Valida que el corte esté completo y correcto
        /// </summary>
        public bool ValidarCorte()
        {
            return Estado == "Completado" &&
                   EfectivoContado >= 0 &&
                   FondoCajaInicial >= 0 &&
                   FondoCajaSiguiente >= 0 &&
                   !string.IsNullOrEmpty(UsuarioCorte);
        }

        /// <summary>
        /// Obtiene el estado del corte en formato amigable
        /// </summary>
        public string ObtenerEstadoDescriptivo()
        {
            return Estado switch
            {
                "Pendiente" => "⏳ Pendiente",
                "Completado" => TieneSobrante ? "✅ Completado (Sobrante)" :
                               TieneFaltante ? "⚠️ Completado (Faltante)" :
                               "✅ Completado (Exacto)",
                "Cancelado" => "❌ Cancelado",
                _ => Estado
            };
        }

        /// <summary>
        /// Obtiene resumen completo del corte para mostrar
        /// </summary>
        public string ObtenerResumenCompleto()
        {
            var resumen = $"📊 CORTE DE CAJA - {FechaCorte:dd/MM/yyyy}\n\n";

            // Información básica
            resumen += $"🕐 Realizado: {FechaHoraCorte:dd/MM/yyyy HH:mm}\n";
            resumen += $"👤 Usuario: {UsuarioCorte}\n";
            resumen += $"📄 Tickets procesados: {CantidadTickets}\n\n";

            // Totales del sistema
            resumen += $"💻 TOTALES DEL SISTEMA:\n";
            resumen += $"   • Total ventas: {TotalVentasCalculado:C2}\n";
            resumen += $"   • Efectivo: {EfectivoCalculado:C2}\n";
            resumen += $"   • Tarjeta: {TarjetaCalculado:C2}\n";
            resumen += $"   • Transferencia: {TransferenciaCalculado:C2}\n\n";

            // Comisiones si las hay
            if (ComisionesTotalesCalculadas > 0)
            {
                resumen += $"🏦 COMISIONES:\n";
                resumen += $"   • Comisión base: {ComisionesCalculadas:C2}\n";
                if (IVAComisionesCalculado > 0)
                    resumen += $"   • IVA sobre comisión: {IVAComisionesCalculado:C2}\n";
                resumen += $"   • Total comisiones: {ComisionesTotalesCalculadas:C2}\n\n";
            }

            // Gastos del día
            if (GastosTotalesCalculados > 0)
            {
                resumen += $"💸 GASTOS DEL DÍA:\n";
                resumen += $"   • Total gastos: {GastosTotalesCalculados:C2}\n";
                resumen += $"   • Efectivo después de gastos: {EfectivoRealDisponible:C2}\n\n";
            }

            // Conciliación de efectivo
            resumen += $"💰 CONCILIACIÓN DE EFECTIVO:\n";
            resumen += $"   • Fondo inicial: {FondoCajaInicial:C2}\n";
            resumen += $"   • Efectivo esperado: {EfectivoEsperado:C2}\n";
            resumen += $"   • Efectivo contado: {EfectivoContado:C2}\n";
            resumen += $"   • Diferencia: {DiferenciaEfectivo:C2} ";

            if (TieneSobrante)
                resumen += "(SOBRANTE 📈)\n";
            else if (TieneFaltante)
                resumen += "(FALTANTE 📉)\n";
            else
                resumen += "(EXACTO ✅)\n";

            resumen += $"   • Fondo para mañana: {FondoCajaSiguiente:C2}\n";
            resumen += $"   • Para depositar: {EfectivoParaDepositar:C2}\n\n";

            // Rentabilidad
            resumen += $"📈 RENTABILIDAD:\n";
            resumen += $"   • Ganancia bruta: {GananciaBrutaCalculada:C2}\n";
            resumen += $"   • Ganancia neta (sin gastos): {GananciaNetaCalculada:C2}\n";
            if (GastosTotalesCalculados > 0)
            {
                resumen += $"   • Gastos del día: -{GastosTotalesCalculados:C2}\n";
                resumen += $"   • Ganancia neta final: {GananciaNetaFinal:C2}\n";
            }

            // Información de depósito
            if (DepositoRealizado)
            {
                resumen += $"\n🏧 DEPÓSITO REALIZADO:\n";
                resumen += $"   • Monto: {MontoDepositado:C2}\n";
                resumen += $"   • Referencia: {ReferenciaDeposito}\n";
            }

            // Observaciones
            if (!string.IsNullOrEmpty(Observaciones))
            {
                resumen += $"\n📝 OBSERVACIONES:\n{Observaciones}\n";
            }

            resumen += $"\n⚡ Estado: {ObtenerEstadoDescriptivo()}";

            return resumen;
        }

        /// <summary>
        /// Obtiene análisis de diferencias para auditoría
        /// </summary>
        public string ObtenerAnalisisDiferencias()
        {
            if (DiferenciaAceptable && Math.Abs(DiferenciaEfectivo) <= 1)
                return "✅ Sin diferencias significativas";

            var analisis = $"📊 ANÁLISIS DE DIFERENCIAS:\n\n";

            if (TieneSobrante)
            {
                analisis += $"📈 SOBRANTE DETECTADO: {DiferenciaEfectivo:C2}\n";
                analisis += $"Posibles causas:\n";
                analisis += $"• Devolución no registrada\n";
                analisis += $"• Error en cambio entregado\n";
                analisis += $"• Venta no capturada en sistema\n";
                if (!string.IsNullOrEmpty(MotivoSobrante))
                    analisis += $"• Motivo registrado: {MotivoSobrante}\n";
            }
            else if (TieneFaltante)
            {
                analisis += $"📉 FALTANTE DETECTADO: {Math.Abs(DiferenciaEfectivo):C2}\n";
                analisis += $"Posibles causas:\n";
                analisis += $"• Error en cambio calculado\n";
                analisis += $"• Gasto no registrado en sistema\n";
                analisis += $"• Diferencia en conteo físico\n";
                if (GastosTotalesCalculados > 0)
                    analisis += $"• Gastos registrados del día: {GastosTotalesCalculados:C2}\n";
                if (!string.IsNullOrEmpty(MotivoFaltante))
                    analisis += $"• Motivo registrado: {MotivoFaltante}\n";
            }

            analisis += $"\n⚡ Margen aceptable: ±$10.00\n";
            analisis += $"⚡ Estado: {(DiferenciaAceptable ? "Dentro del margen" : "Fuera del margen")}";

            return analisis;
        }
    }
}
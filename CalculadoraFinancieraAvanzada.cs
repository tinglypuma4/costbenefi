using System;
using System.Collections.Generic;
using System.Linq;

namespace costbenefi.Models
{
    /// <summary>
    /// Clase para cálculos financieros avanzados según estándares internacionales
    /// </summary>
    public class CalculadoraFinancieraAvanzada
    {
        #region Valor Presente Neto (VPN)

        /// <summary>
        /// Calcula el Valor Presente Neto de un proyecto
        /// </summary>
        /// <param name="flujosCaja">Flujos de caja por período</param>
        /// <param name="tasaDescuento">Tasa de descuento (como decimal, ej: 0.12 para 12%)</param>
        /// <param name="inversionInicial">Inversión inicial</param>
        /// <returns>VPN calculado</returns>
        public static decimal CalcularVPN(List<decimal> flujosCaja, decimal tasaDescuento, decimal inversionInicial)
        {
            try
            {
                var vpn = -inversionInicial;

                for (int t = 1; t <= flujosCaja.Count; t++)
                {
                    var flujoDescontado = flujosCaja[t - 1] / (decimal)Math.Pow((double)(1 + tasaDescuento), t);
                    vpn += flujoDescontado;
                }

                return vpn;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error calculando VPN: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Calcula VPN para proyecto con flujos anuales constantes
        /// </summary>
        public static decimal CalcularVPNSimple(decimal flujoAnual, int años, decimal tasaDescuento, decimal inversionInicial)
        {
            var flujos = Enumerable.Repeat(flujoAnual, años).ToList();
            return CalcularVPN(flujos, tasaDescuento, inversionInicial);
        }

        #endregion

        #region Tasa Interna de Retorno (TIR)

        /// <summary>
        /// Calcula la TIR usando método iterativo
        /// </summary>
        public static decimal CalcularTIR(List<decimal> flujosCaja, decimal inversionInicial, decimal precision = 0.0001m)
        {
            try
            {
                decimal tir = 0.1m; // Estimación inicial 10%
                decimal incremento = 0.01m;
                decimal vpnAnterior = 0;
                int iteraciones = 0;
                int maxIteraciones = 1000;

                while (iteraciones < maxIteraciones)
                {
                    var vpnActual = CalcularVPN(flujosCaja, tir, inversionInicial);

                    if (Math.Abs(vpnActual) < precision)
                        return tir;

                    if (vpnActual > 0)
                    {
                        tir += incremento;
                    }
                    else
                    {
                        tir -= incremento;
                        incremento /= 2; // Reducir incremento para mayor precisión
                    }

                    vpnAnterior = vpnActual;
                    iteraciones++;
                }

                return tir;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error calculando TIR: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region Relación Beneficio-Costo (RBC)

        /// <summary>
        /// Calcula la Relación Beneficio-Costo
        /// </summary>
        public static decimal CalcularRBC(List<decimal> beneficios, List<decimal> costos, decimal tasaDescuento)
        {
            try
            {
                var vpBeneficios = 0m;
                var vpCostos = 0m;

                for (int t = 1; t <= Math.Max(beneficios.Count, costos.Count); t++)
                {
                    var factor = (decimal)Math.Pow((double)(1 + tasaDescuento), t);

                    if (t <= beneficios.Count)
                        vpBeneficios += beneficios[t - 1] / factor;

                    if (t <= costos.Count)
                        vpCostos += costos[t - 1] / factor;
                }

                return vpCostos > 0 ? vpBeneficios / vpCostos : 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error calculando RBC: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region WACC (Costo Promedio Ponderado de Capital)

        /// <summary>
        /// Calcula el WACC
        /// </summary>
        public static decimal CalcularWACC(decimal valorPatrimonio, decimal valorDeuda,
                                         decimal costoPatrimonio, decimal costoDeuda, decimal tasaImpuesto)
        {
            try
            {
                var valorTotal = valorPatrimonio + valorDeuda;
                if (valorTotal == 0) return 0;

                var pesoPatrimonio = valorPatrimonio / valorTotal;
                var pesoDeuda = valorDeuda / valorTotal;

                return (pesoPatrimonio * costoPatrimonio) + (pesoDeuda * costoDeuda * (1 - tasaImpuesto));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error calculando WACC: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region EOQ (Cantidad Económica de Pedido)

        /// <summary>
        /// Calcula la Cantidad Económica de Pedido
        /// </summary>
        public static decimal CalcularEOQ(decimal demandaAnual, decimal costoPorPedido, decimal costoMantenimientoUnitario)
        {
            try
            {
                if (costoMantenimientoUnitario <= 0) return 0;

                var eoq = Math.Sqrt((double)(2 * demandaAnual * costoPorPedido / costoMantenimientoUnitario));
                return (decimal)eoq;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error calculando EOQ: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Calcula el costo total de inventario con EOQ
        /// </summary>
        public static decimal CalcularCostoTotalEOQ(decimal eoq, decimal demandaAnual,
                                                   decimal costoPorPedido, decimal costoMantenimientoUnitario)
        {
            try
            {
                var costoOrdenar = (demandaAnual / eoq) * costoPorPedido;
                var costoMantener = (eoq / 2) * costoMantenimientoUnitario;
                return costoOrdenar + costoMantener;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error calculando costo total EOQ: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region Período de Recuperación

        /// <summary>
        /// Calcula el período de recuperación simple
        /// </summary>
        public static decimal CalcularPeriodoRecuperacion(List<decimal> flujosCaja, decimal inversionInicial)
        {
            try
            {
                decimal acumulado = 0;

                for (int periodo = 0; periodo < flujosCaja.Count; periodo++)
                {
                    acumulado += flujosCaja[periodo];

                    if (acumulado >= inversionInicial)
                    {
                        // Interpolación para obtener fracción del período
                        var exceso = acumulado - inversionInicial;
                        var fraccion = exceso / flujosCaja[periodo];
                        return periodo + 1 - fraccion;
                    }
                }

                return -1; // No se recupera en el período analizado
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error calculando período recuperación: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Calcula el período de recuperación descontado
        /// </summary>
        public static decimal CalcularPeriodoRecuperacionDescontado(List<decimal> flujosCaja,
                                                                   decimal inversionInicial, decimal tasaDescuento)
        {
            try
            {
                decimal acumulado = 0;

                for (int periodo = 1; periodo <= flujosCaja.Count; periodo++)
                {
                    var flujoDescontado = flujosCaja[periodo - 1] / (decimal)Math.Pow((double)(1 + tasaDescuento), periodo);
                    acumulado += flujoDescontado;

                    if (acumulado >= inversionInicial)
                    {
                        var exceso = acumulado - inversionInicial;
                        var fraccion = exceso / flujoDescontado;
                        return periodo - fraccion;
                    }
                }

                return -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error calculando período recuperación descontado: {ex.Message}");
                return -1;
            }
        }

        #endregion

        #region Análisis de Sensibilidad

        /// <summary>
        /// Analiza sensibilidad del VPN ante cambios en variables
        /// </summary>
        public static List<ResultadoSensibilidad> AnalisisSensibilidadVPN(
            List<decimal> flujosCajaBase, decimal inversionInicial, decimal tasaDescuentoBase,
            decimal rangoVariacion = 0.2m, int pasos = 5)
        {
            var resultados = new List<ResultadoSensibilidad>();

            try
            {
                var vpnBase = CalcularVPN(flujosCajaBase, tasaDescuentoBase, inversionInicial);

                // Análisis de sensibilidad en tasa de descuento
                for (int i = -pasos; i <= pasos; i++)
                {
                    var variacion = (decimal)i / pasos * rangoVariacion;
                    var nuevaTasa = tasaDescuentoBase * (1 + variacion);
                    var nuevoVPN = CalcularVPN(flujosCajaBase, nuevaTasa, inversionInicial);

                    resultados.Add(new ResultadoSensibilidad
                    {
                        Variable = "Tasa de Descuento",
                        VariacionPorcentual = variacion * 100,
                        ValorOriginal = tasaDescuentoBase * 100,
                        ValorNuevo = nuevaTasa * 100,
                        VPNOriginal = vpnBase,
                        VPNNuevo = nuevoVPN,
                        SensibilidadPorcentual = vpnBase != 0 ? ((nuevoVPN - vpnBase) / vpnBase) * 100 : 0
                    });
                }

                // Análisis de sensibilidad en flujos de caja
                for (int i = -pasos; i <= pasos; i++)
                {
                    var variacion = (decimal)i / pasos * rangoVariacion;
                    var nuevosFlujos = flujosCajaBase.Select(f => f * (1 + variacion)).ToList();
                    var nuevoVPN = CalcularVPN(nuevosFlujos, tasaDescuentoBase, inversionInicial);

                    resultados.Add(new ResultadoSensibilidad
                    {
                        Variable = "Flujos de Caja",
                        VariacionPorcentual = variacion * 100,
                        ValorOriginal = flujosCajaBase.Average(),
                        ValorNuevo = nuevosFlujos.Average(),
                        VPNOriginal = vpnBase,
                        VPNNuevo = nuevoVPN,
                        SensibilidadPorcentual = vpnBase != 0 ? ((nuevoVPN - vpnBase) / vpnBase) * 100 : 0
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en análisis de sensibilidad: {ex.Message}");
            }

            return resultados;
        }

        #endregion
    }

    /// <summary>
    /// Resultado del análisis de sensibilidad
    /// </summary>
    public class ResultadoSensibilidad
    {
        public string Variable { get; set; } = "";
        public decimal VariacionPorcentual { get; set; }
        public decimal ValorOriginal { get; set; }
        public decimal ValorNuevo { get; set; }
        public decimal VPNOriginal { get; set; }
        public decimal VPNNuevo { get; set; }
        public decimal SensibilidadPorcentual { get; set; }

        // Propiedades formateadas
        public string VariacionFormateada => $"{VariacionPorcentual:+0.0;-0.0}%";
        public string VPNFormateado => VPNNuevo >= 1000 ? $"${VPNNuevo / 1000:F1}K" : $"${VPNNuevo:F0}";
        public string SensibilidadFormateada => $"{SensibilidadPorcentual:+0.0;-0.0}%";
    }

    /// <summary>
    /// Proyecto para análisis financiero
    /// </summary>
    public class ProyectoFinanciero
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal InversionInicial { get; set; }
        public List<decimal> FlujosCaja { get; set; } = new();
        public decimal TasaDescuento { get; set; }
        public DateTime FechaInicio { get; set; }
        public int DuracionAños { get; set; }

        // Resultados calculados
        public decimal VPN => CalculadoraFinancieraAvanzada.CalcularVPN(FlujosCaja, TasaDescuento, InversionInicial);
        public decimal TIR => CalculadoraFinancieraAvanzada.CalcularTIR(FlujosCaja, InversionInicial);
        public decimal PeriodoRecuperacion => CalculadoraFinancieraAvanzada.CalcularPeriodoRecuperacion(FlujosCaja, InversionInicial);

        // Propiedades formateadas
        public string VPNFormateado => VPN >= 1000 ? $"${VPN / 1000:F1}K" : $"${VPN:F0}";
        public string TIRFormateada => $"{TIR * 100:F2}%";
        public string ViabilidadTexto => VPN > 0 ? "✅ VIABLE" : "❌ NO VIABLE";
        public string RecomendacionTexto => VPN > 0 ? "Se recomienda ejecutar" : "Se recomienda rechazar";
    }
}
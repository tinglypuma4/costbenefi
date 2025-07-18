using System;
using costbenefi.Models;
using costbenefi.Views;

namespace costbenefi.Helpers
{
    /// <summary>
    /// Clase helper para corregir y estandarizar los cálculos de materiales
    /// </summary>
    public static class MaterialCalculationHelper
    {
        /// <summary>
        /// Aplica los cálculos correctos a un RawMaterial basado en ProductoSimplificado
        /// </summary>
        public static void AplicarCalculosCorrectos(RawMaterial material, ProductoSimplificado producto, bool almacenarComoPiezas = false)
        {
            var resultado = CalculadoraInteligente.Calcular(producto);

            if (!resultado.Exitoso)
            {
                throw new InvalidOperationException($"Error en cálculos: {resultado.Error}");
            }

            // 1. UNIDADES Y STOCK
            if (almacenarComoPiezas)
            {
                // Almacenar como piezas individuales
                material.UnidadMedida = "piezas";
                material.UnidadBase = ObtenerUnidadCorta(producto.UnidadMedida);
                material.StockNuevo = resultado.UnidadesTotales;
                material.FactorConversion = producto.CantidadPorUnidad; // Factor para convertir piezas a contenido
            }
            else
            {
                // Almacenar como contenido total
                string unidadCorta = ObtenerUnidadCorta(producto.UnidadMedida);
                material.UnidadMedida = unidadCorta;
                material.UnidadBase = unidadCorta;
                material.StockNuevo = resultado.ContenidoTotalAgregado;
                material.FactorConversion = 1; // No hay conversión
            }

            // 2. PRECIOS PRINCIPALES (lo que muestra el DataGrid)
            material.PrecioConIVA = resultado.PrecioConIVA;
            material.PrecioSinIVA = resultado.PrecioSinIVA;

            // 3. PRECIO POR UNIDAD (según cómo se almacene)
            if (almacenarComoPiezas)
            {
                // Si almacenamos por piezas, el precio por unidad es por pieza
                material.PrecioPorUnidad = resultado.CostoPorUnidad;
            }
            else
            {
                // Si almacenamos por contenido, el precio por unidad es por unidad base
                material.PrecioPorUnidad = resultado.CostoPorUnidadBase;
            }

            // 4. PRECIOS BASE (siempre por unidad de contenido - ml, g, etc.)
            material.PrecioPorUnidadBase = resultado.CostoPorUnidadBase;
            material.PrecioBaseConIVA = resultado.CostoPorUnidadBase * (1 + producto.PorcentajeIVA / 100m);
            material.PrecioBaseSinIVA = resultado.CostoPorUnidadBase;

            // 5. STOCK ANTERIOR
            material.StockAntiguo = producto.StockAnterior;

            // 6. FECHAS
            material.FechaActualizacion = DateTime.Now;
        }

        /// <summary>
        /// Genera observaciones detalladas para el material
        /// </summary>
        public static string GenerarObservaciones(string observacionesUsuario, ProductoSimplificado producto, ResultadoCalculoInteligente resultado, bool almacenarComoPiezas)
        {
            string tipoAlmacenamiento = almacenarComoPiezas ? "PIEZAS INDIVIDUALES" : "CONTENIDO TOTAL";
            string tipoProducto = producto.EsEmpaquetado ? "EMPAQUETADO" : "INDIVIDUAL";

            string observaciones = $"{observacionesUsuario}\n\n";
            observaciones += $"--- REGISTRO AUTOMÁTICO [{DateTime.Now:dd/MM/yyyy HH:mm}] ---\n";
            observaciones += $"Tipo: {tipoProducto} | Almacenamiento: {tipoAlmacenamiento}\n";
            observaciones += $"Presentación: {producto.Presentacion}\n";

            if (producto.EsEmpaquetado)
            {
                observaciones += $"Empaquetado: {producto.UnidadesPorPaquete} unidades por paquete\n";
                observaciones += $"Compra: {producto.CantidadComprada} paquetes = {resultado.UnidadesTotales} unidades\n";
            }
            else
            {
                observaciones += $"Compra: {producto.CantidadComprada} unidades\n";
            }

            observaciones += $"Contenido por unidad: {producto.CantidadPorUnidad} {ObtenerUnidadCorta(producto.UnidadMedida)}\n";
            observaciones += $"Contenido total: {resultado.ContenidoTotalAgregado} {ObtenerUnidadCorta(producto.UnidadMedida)}\n";
            observaciones += $"Precio: {resultado.PrecioConIVA:C2} (incluye IVA: {producto.IncluyeIVA})\n";
            observaciones += $"Costo por unidad base: {resultado.CostoPorUnidadBase:C4}/{ObtenerUnidadCorta(producto.UnidadMedida)}\n";

            if (almacenarComoPiezas)
            {
                observaciones += $"Costo por pieza: {resultado.CostoPorUnidad:C4}\n";
            }

            observaciones += $"\n{resultado.AnalisisCompleto}";

            return observaciones;
        }

        /// <summary>
        /// Actualiza un material existente agregando stock
        /// </summary>
        public static void ActualizarMaterialExistente(RawMaterial materialExistente, ProductoSimplificado producto, bool almacenarComoPiezas = false)
        {
            var resultado = CalculadoraInteligente.Calcular(producto);

            if (!resultado.Exitoso)
            {
                throw new InvalidOperationException($"Error en cálculos: {resultado.Error}");
            }

            // Agregar al stock existente
            if (almacenarComoPiezas)
            {
                materialExistente.StockNuevo += resultado.UnidadesTotales;
            }
            else
            {
                materialExistente.StockNuevo += resultado.ContenidoTotalAgregado;
            }

            // Recalcular precios promedio ponderado
            decimal stockTotalAnterior = materialExistente.StockAntiguo + materialExistente.StockNuevo -
                                       (almacenarComoPiezas ? resultado.UnidadesTotales : resultado.ContenidoTotalAgregado);

            if (stockTotalAnterior > 0)
            {
                decimal valorAnterior = stockTotalAnterior * materialExistente.PrecioSinIVA;
                decimal valorNuevo = resultado.PrecioSinIVA;
                decimal stockTotal = materialExistente.StockTotal;

                // Promedio ponderado
                materialExistente.PrecioSinIVA = (valorAnterior + valorNuevo) / stockTotal;
                materialExistente.PrecioConIVA = materialExistente.PrecioSinIVA * (1 + producto.PorcentajeIVA / 100m);
            }
            else
            {
                // Si no había stock anterior, usar precios nuevos
                materialExistente.PrecioSinIVA = resultado.PrecioSinIVA;
                materialExistente.PrecioConIVA = resultado.PrecioConIVA;
            }

            // Actualizar fecha
            materialExistente.FechaActualizacion = DateTime.Now;

            // Agregar observaciones
            string nuevasObservaciones = $"\n\n--- ACTUALIZACIÓN [{DateTime.Now:dd/MM/yyyy HH:mm}] ---\n";
            nuevasObservaciones += $"Stock agregado: {(almacenarComoPiezas ? resultado.UnidadesTotales : resultado.ContenidoTotalAgregado)} {materialExistente.UnidadMedida}\n";
            nuevasObservaciones += $"Precio de compra: {resultado.PrecioConIVA:C2}\n";
            nuevasObservaciones += $"Nuevo stock total: {materialExistente.StockTotal} {materialExistente.UnidadMedida}";

            materialExistente.Observaciones += nuevasObservaciones;
        }

        /// <summary>
        /// Obtiene la unidad corta para almacenamiento
        /// </summary>
        private static string ObtenerUnidadCorta(string unidad)
        {
            return unidad switch
            {
                "Mililitros" => "ml",
                "Litros" => "L",
                "Gramos" => "g",
                "Kilos" => "kg",
                "Piezas" => "pzs",
                "ml" => "ml",
                "L" => "L",
                "g" => "g",
                "kg" => "kg",
                "pzs" => "pzs",
                _ => unidad?.ToLower() ?? "u"
            };
        }

        /// <summary>
        /// Valida que los cálculos del material sean consistentes
        /// </summary>
        public static bool ValidarCalculosMaterial(RawMaterial material)
        {
            try
            {
                // Verificar que los valores totales calculados sean correctos
                decimal valorTotalConIVACalculado = material.StockTotal * material.PrecioConIVA;
                decimal valorTotalSinIVACalculado = material.StockTotal * material.PrecioSinIVA;

                bool valoresCorrectos = Math.Abs(material.ValorTotalConIVA - valorTotalConIVACalculado) < 0.01m &&
                                      Math.Abs(material.ValorTotalSinIVA - valorTotalSinIVACalculado) < 0.01m;

                bool preciosPositivos = material.PrecioConIVA > 0 && material.PrecioSinIVA > 0 &&
                                      material.PrecioPorUnidad > 0;

                bool stockPositivo = material.StockTotal >= 0;

                return valoresCorrectos && preciosPositivos && stockPositivo;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Corrige un material con cálculos incorrectos
        /// </summary>
        public static void CorregirCalculosMaterial(RawMaterial material)
        {
            // Si los precios base están mal, calcularlos desde los precios principales
            if (material.PrecioBaseConIVA <= 0 && material.PrecioConIVA > 0)
            {
                material.PrecioBaseConIVA = material.PrecioConIVA;
            }

            if (material.PrecioBaseSinIVA <= 0 && material.PrecioSinIVA > 0)
            {
                material.PrecioBaseSinIVA = material.PrecioSinIVA;
            }

            // Si el precio por unidad base está mal, calcularlo
            if (material.PrecioPorUnidadBase <= 0 && material.PrecioSinIVA > 0)
            {
                material.PrecioPorUnidadBase = material.PrecioSinIVA;
            }

            // Actualizar fecha de corrección
            material.FechaActualizacion = DateTime.Now;
        }
    }
}
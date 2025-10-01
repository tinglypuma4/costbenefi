using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace costbenefi.Models
{
    /// <summary>
    /// Modelo para análisis de punto de equilibrio por item (producto, categoría, proveedor, etc.)
    /// </summary>
    public class ItemPuntoEquilibrio : INotifyPropertyChanged
    {
        #region Propiedades Básicas
        private int _id;
        private string _nombre = string.Empty;
        private string _categoria = string.Empty;
        private string _proveedor = string.Empty;
        private int _posicion;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Nombre
        {
            get => _nombre;
            set { _nombre = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string Categoria
        {
            get => _categoria;
            set { _categoria = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string Proveedor
        {
            get => _proveedor;
            set { _proveedor = value ?? string.Empty; OnPropertyChanged(); }
        }

        public int Posicion
        {
            get => _posicion;
            set { _posicion = value; OnPropertyChanged(); }
        }
        #endregion

        #region Datos Financieros Base
        private decimal _precioVentaPromedio;
        private decimal _costoVariableUnitario;
        private decimal _cantidadVendidaPeriodo;
        private decimal _ingresosTotalesPeriodo;

        public decimal PrecioVentaPromedio
        {
            get => _precioVentaPromedio;
            set
            {
                _precioVentaPromedio = value;
                OnPropertyChanged();
                RecalcularMargenContribucion();
            }
        }

        public decimal CostoVariableUnitario
        {
            get => _costoVariableUnitario;
            set
            {
                _costoVariableUnitario = value;
                OnPropertyChanged();
                RecalcularMargenContribucion();
            }
        }

        public decimal CantidadVendidaPeriodo
        {
            get => _cantidadVendidaPeriodo;
            set { _cantidadVendidaPeriodo = value; OnPropertyChanged(); }
        }

        public decimal IngresosTotalesPeriodo
        {
            get => _ingresosTotalesPeriodo;
            set { _ingresosTotalesPeriodo = value; OnPropertyChanged(); }
        }
        #endregion

        #region Margen de Contribución
        private decimal _margenContribucionUnitario;
        private decimal _margenContribucionPorcentaje;

        public decimal MargenContribucionUnitario
        {
            get => _margenContribucionUnitario;
            set
            {
                _margenContribucionUnitario = value;
                OnPropertyChanged();
                RecalcularPuntoEquilibrio();
            }
        }

        public decimal MargenContribucionPorcentaje
        {
            get => _margenContribucionPorcentaje;
            set { _margenContribucionPorcentaje = value; OnPropertyChanged(); }
        }
        #endregion

        #region Costos Fijos
        private decimal _costosFijosPeriodo;
        private decimal _costosFijosAjustados;

        public decimal CostosFijosPeriodo
        {
            get => _costosFijosPeriodo;
            set
            {
                _costosFijosPeriodo = value;
                OnPropertyChanged();
                RecalcularPuntoEquilibrio();
            }
        }

        public decimal CostosFijosAjustados
        {
            get => _costosFijosAjustados;
            set
            {
                _costosFijosAjustados = value;
                OnPropertyChanged();
                RecalcularPuntoEquilibrio();
            }
        }
        #endregion

        #region Punto de Equilibrio
        private decimal _puntoEquilibrioUnidades;
        private decimal _puntoEquilibrioIngresos;
        private int _diasParaEquilibrio;
        private string _estadoEquilibrio = string.Empty;

        public decimal PuntoEquilibrioUnidades
        {
            get => _puntoEquilibrioUnidades;
            set
            {
                _puntoEquilibrioUnidades = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PuntoEquilibrioFormateado));
            }
        }

        public decimal PuntoEquilibrioIngresos
        {
            get => _puntoEquilibrioIngresos;
            set { _puntoEquilibrioIngresos = value; OnPropertyChanged(); }
        }

        public int DiasParaEquilibrio
        {
            get => _diasParaEquilibrio;
            set { _diasParaEquilibrio = value; OnPropertyChanged(); }
        }

        public string EstadoEquilibrio
        {
            get => _estadoEquilibrio;
            set { _estadoEquilibrio = value ?? string.Empty; OnPropertyChanged(); }
        }
        #endregion

        #region Propiedades Formateadas para UI
        public string PuntoEquilibrioFormateado
        {
            get
            {
                if (PuntoEquilibrioUnidades <= 0)
                    return "N/A";
                else if (PuntoEquilibrioUnidades >= 1000)
                    return $"{PuntoEquilibrioUnidades / 1000:F1}K";
                else
                    return $"{PuntoEquilibrioUnidades:F0}";
            }
        }

        public string MargenFormateado
        {
            get
            {
                return $"{MargenContribucionPorcentaje:F1}%";
            }
        }

        public string PrecioFormateado
        {
            get
            {
                return $"${PrecioVentaPromedio:F2}";
            }
        }

        public string CostoFormateado
        {
            get
            {
                return $"${CostoVariableUnitario:F2}";
            }
        }

        public string IngresosFormateados
        {
            get
            {
                if (IngresosTotalesPeriodo >= 1000)
                    return $"${IngresosTotalesPeriodo / 1000:F1}K";
                else
                    return $"${IngresosTotalesPeriodo:F0}";
            }
        }

        public string CantidadFormateada
        {
            get
            {
                if (CantidadVendidaPeriodo >= 1000)
                    return $"{CantidadVendidaPeriodo / 1000:F1}K";
                else
                    return $"{CantidadVendidaPeriodo:F0}";
            }
        }

        public string DiasEquilibrioFormateado
        {
            get
            {
                if (DiasParaEquilibrio >= 999)
                    return "∞";
                else if (DiasParaEquilibrio > 365)
                    return $"{DiasParaEquilibrio / 365:F1} años";
                else if (DiasParaEquilibrio > 30)
                    return $"{DiasParaEquilibrio / 30:F1} meses";
                else
                    return $"{DiasParaEquilibrio} días";
            }
        }
        #endregion

        #region Propiedades de Estado y Clasificación
        public string ClasificacionRiesgo
        {
            get
            {
                if (PuntoEquilibrioUnidades <= 0 || MargenContribucionUnitario <= 0)
                    return "🚨 Alto Riesgo";
                else if (PuntoEquilibrioUnidades <= 50)
                    return "✅ Bajo Riesgo";
                else if (PuntoEquilibrioUnidades <= 200)
                    return "⚠️ Riesgo Moderado";
                else
                    return "🚨 Alto Riesgo";
            }
        }

        public string RendimientoCategoria
        {
            get
            {
                if (MargenContribucionPorcentaje >= 40)
                    return "🥇 Excelente";
                else if (MargenContribucionPorcentaje >= 25)
                    return "🥈 Bueno";
                else if (MargenContribucionPorcentaje >= 15)
                    return "🥉 Regular";
                else
                    return "❌ Deficiente";
            }
        }

        public bool EsRentable => EstadoEquilibrio.Contains("✅");

        public bool RequiereAtencion => EstadoEquilibrio.Contains("❌") || PuntoEquilibrioUnidades > 500;
        #endregion

        #region Métodos de Cálculo
        private void RecalcularMargenContribucion()
        {
            MargenContribucionUnitario = PrecioVentaPromedio - CostoVariableUnitario;

            if (PrecioVentaPromedio > 0)
            {
                MargenContribucionPorcentaje = (MargenContribucionUnitario / PrecioVentaPromedio) * 100;
            }
            else
            {
                MargenContribucionPorcentaje = 0;
            }
        }

        private void RecalcularPuntoEquilibrio()
        {
            if (MargenContribucionUnitario > 0)
            {
                PuntoEquilibrioUnidades = CostosFijosAjustados / MargenContribucionUnitario;
                PuntoEquilibrioIngresos = PuntoEquilibrioUnidades * PrecioVentaPromedio;
            }
            else
            {
                PuntoEquilibrioUnidades = 0;
                PuntoEquilibrioIngresos = 0;
            }
        }

        /// <summary>
        /// Calcula punto de equilibrio con precio ajustado (para análisis de sensibilidad)
        /// </summary>
        public decimal CalcularEquilibrioConPrecioAjustado(decimal porcentajeVariacion)
        {
            var precioAjustado = PrecioVentaPromedio * (1 + porcentajeVariacion);
            var margenAjustado = precioAjustado - CostoVariableUnitario;

            if (margenAjustado > 0)
                return CostosFijosAjustados / margenAjustado;
            else
                return 0;
        }

        /// <summary>
        /// Calcula punto de equilibrio con costo ajustado
        /// </summary>
        public decimal CalcularEquilibrioConCostoAjustado(decimal porcentajeVariacion)
        {
            var costoAjustado = CostoVariableUnitario * (1 + porcentajeVariacion);
            var margenAjustado = PrecioVentaPromedio - costoAjustado;

            if (margenAjustado > 0)
                return CostosFijosAjustados / margenAjustado;
            else
                return 0;
        }

        /// <summary>
        /// Determina el estado del equilibrio basado en ventas actuales
        /// </summary>
        public void ActualizarEstadoEquilibrio(decimal factorPeriodo = 1.0m)
        {
            if (MargenContribucionUnitario <= 0)
            {
                EstadoEquilibrio = "❌ Margen Negativo";
                return;
            }

            var ventasNecesarias = PuntoEquilibrioUnidades * factorPeriodo;

            if (CantidadVendidaPeriodo >= ventasNecesarias)
                EstadoEquilibrio = "✅ Rentable";
            else if (CantidadVendidaPeriodo >= ventasNecesarias * 0.7m)
                EstadoEquilibrio = "⚠️ Cerca";
            else
                EstadoEquilibrio = "❌ Déficit";
        }
        #endregion

        #region Métodos de Validación
        public bool EsValido()
        {
            return PrecioVentaPromedio > 0 &&
                   CostoVariableUnitario >= 0 &&
                   CostosFijosAjustados >= 0 &&
                   !string.IsNullOrWhiteSpace(Nombre);
        }

        public List<string> ValidarDatos()
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(Nombre))
                errores.Add("El nombre del item es requerido");

            if (PrecioVentaPromedio <= 0)
                errores.Add("El precio de venta debe ser mayor a cero");

            if (CostoVariableUnitario < 0)
                errores.Add("El costo variable no puede ser negativo");

            if (CostosFijosAjustados < 0)
                errores.Add("Los costos fijos no pueden ser negativos");

            if (PrecioVentaPromedio <= CostoVariableUnitario)
                errores.Add("El precio de venta debe ser mayor al costo variable");

            return errores;
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Métodos de Utilidad
        public override string ToString()
        {
            return $"{Nombre} - Equilibrio: {PuntoEquilibrioFormateado} unidades, Margen: {MargenFormateado}";
        }

        public ItemPuntoEquilibrio Clone()
        {
            return new ItemPuntoEquilibrio
            {
                Id = this.Id,
                Nombre = this.Nombre,
                Categoria = this.Categoria,
                Proveedor = this.Proveedor,
                PrecioVentaPromedio = this.PrecioVentaPromedio,
                CostoVariableUnitario = this.CostoVariableUnitario,
                CantidadVendidaPeriodo = this.CantidadVendidaPeriodo,
                IngresosTotalesPeriodo = this.IngresosTotalesPeriodo,
                CostosFijosPeriodo = this.CostosFijosPeriodo,
                CostosFijosAjustados = this.CostosFijosAjustados,
                EstadoEquilibrio = this.EstadoEquilibrio,
                DiasParaEquilibrio = this.DiasParaEquilibrio
            };
        }
        #endregion

        #region Constructor
        public ItemPuntoEquilibrio()
        {
            // Constructor vacío para inicialización
        }

        public ItemPuntoEquilibrio(string nombre, decimal precioVenta, decimal costoVariable, decimal costosFijos)
        {
            Nombre = nombre;
            PrecioVentaPromedio = precioVenta;
            CostoVariableUnitario = costoVariable;
            CostosFijosAjustados = costosFijos;

            RecalcularMargenContribucion();
            RecalcularPuntoEquilibrio();
        }
        #endregion
    }
}
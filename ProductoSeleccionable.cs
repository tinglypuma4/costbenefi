using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using costbenefi.Models;

namespace costbenefi.ViewModels
{
    /// <summary>
    /// ViewModel wrapper para RawMaterial con funcionalidad de selección
    /// Implementa INotifyPropertyChanged para binding bidireccional
    /// </summary>
    public class ProductoSeleccionable : INotifyPropertyChanged
    {
        #region Campos Privados
        private bool _isSelected;
        private readonly RawMaterial _rawMaterial;
        #endregion

        #region Constructor
        public ProductoSeleccionable(RawMaterial rawMaterial, bool isSelectedByDefault = true)
        {
            _rawMaterial = rawMaterial ?? throw new ArgumentNullException(nameof(rawMaterial));
            _isSelected = isSelectedByDefault;
        }
        #endregion

        #region Propiedad de Selección
        /// <summary>
        /// Indica si el producto está seleccionado para incluir en reportes
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region Propiedades del Producto (Delegadas)
        /// <summary>
        /// Referencia al modelo original de RawMaterial
        /// </summary>
        public RawMaterial RawMaterial => _rawMaterial;

        // Propiedades para el DataGrid - delegadas al modelo original
        public string Nombre => _rawMaterial.NombreArticulo ?? "Sin nombre";
        public string Categoria => _rawMaterial.Categoria ?? "Sin categoría";
        public decimal Stock => _rawMaterial.StockTotal;
        public string Unidad => _rawMaterial.UnidadMedida ?? "";
        public decimal PrecioUnitario => _rawMaterial.PrecioConIVA > 0 ? _rawMaterial.PrecioConIVA : _rawMaterial.PrecioPorUnidad;
        public decimal ValorTotal => _rawMaterial.ValorTotalConIVA;
        public string StockBajo => _rawMaterial.TieneStockBajo ? "⚠️ Sí" : "✅ No";
        public string Proveedor => _rawMaterial.Proveedor ?? "Sin proveedor";

        // Propiedades adicionales útiles
        public bool TieneStockBajo => _rawMaterial.TieneStockBajo;
        public DateTime FechaActualizacion => _rawMaterial.FechaActualizacion;
        public string CodigoBarras => _rawMaterial.CodigoBarras ?? "";
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Métodos de Utilidad
        /// <summary>
        /// Convierte este wrapper de vuelta al modelo RawMaterial original
        /// </summary>
        /// <returns>El modelo RawMaterial original</returns>
        public RawMaterial ToRawMaterial() => _rawMaterial;

        /// <summary>
        /// Representación de texto para debugging
        /// </summary>
        public override string ToString()
        {
            return $"{Nombre} - Seleccionado: {IsSelected}";
        }
        #endregion
    }
}
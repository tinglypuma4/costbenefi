namespace costbenefi.Models
{
    /// <summary>
    /// Estados posibles de un lote de fabricación
    /// </summary>
    public enum EstadoLote
    {
        Planificado = 0,
        EnProceso = 1,
        Completado = 2,
        Cancelado = 3,
        Fallido = 4
    }

    /// <summary>
    /// Tipos de fabricación disponibles
    /// </summary>
    public enum TipoFabricacion
    {
        PorLotes = 0,           // Fabricar cantidad específica
        PorCantidad = 1,        // Fabricar hasta alcanzar cantidad
        PorPresupuesto = 2,     // Fabricar lo máximo con presupuesto dado
        PorFecha = 3,           // Fabricar antes de fecha límite
        Continua = 4            // Reposición automática
    }

    /// <summary>
    /// Unidades de medida más comunes para productos fabricados
    /// </summary>
    public enum UnidadMedidaFabricacion
    {
        Litros,
        Mililitros,
        Kilogramos,
        Gramos,
        Piezas,
        Metros,
        Centimetros
    }

    /// <summary>
    /// Categorías de productos fabricados
    /// </summary>
    public enum CategoriaProductoFabricacion
    {
        Liquidos,
        Polvos,
        Alimentos,
        Cosmeticos,
        Quimicos,
        Otros
    }
}
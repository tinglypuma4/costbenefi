namespace costbenefi.Models
{
    /// <summary>
    /// Estados posibles de un lote de fabricaci�n
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
    /// Tipos de fabricaci�n disponibles
    /// </summary>
    public enum TipoFabricacion
    {
        PorLotes = 0,           // Fabricar cantidad espec�fica
        PorCantidad = 1,        // Fabricar hasta alcanzar cantidad
        PorPresupuesto = 2,     // Fabricar lo m�ximo con presupuesto dado
        PorFecha = 3,           // Fabricar antes de fecha l�mite
        Continua = 4            // Reposici�n autom�tica
    }

    /// <summary>
    /// Unidades de medida m�s comunes para productos fabricados
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
    /// Categor�as de productos fabricados
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
namespace Banco.ViewModels
{
    public class ConsultaMovimientoVM
    {
        public DateTime Fecha { get; set; }
        public string TipoMovimiento { get; set; }
        public decimal Cantidad { get; set; }
        public string NombreUsuario { get; set; }
        public decimal Saldo { get; set; }
    }

}

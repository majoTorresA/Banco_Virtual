namespace Banco.Models
{
    public class Cuenta
    {
        public int IdCuenta { get; set; }
        public int IdUsuario { get; set; }
        public Usuario Usuario { get; set; }
        public decimal Saldo { get; set; }
    }
}

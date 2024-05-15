namespace Banco.Models
{
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Clave { get; set; }
        public string Identificacion { get; set; }
        public ICollection<Movimientos> Movimientos { get; set; }
        public Cuenta Cuenta { get; set; } // Esto representa la relación uno a uno con la cuenta
    }
}

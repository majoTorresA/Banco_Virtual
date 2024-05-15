using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Banco.Models
{
    public class Movimientos
    {
        public int IdMovimiento { get; set; }
        public int IdUsuario { get; set; }
        public Usuario Usuario { get; set; }
        public int IdTipoMovimiento { get; set; }
        public TipoMovimiento TipoMovimiento { get; set; }
        public decimal Cantidad { get; set; }
        public DateTime Fecha { get; set; }
    }

    
}


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CronConfigure.Models.Hangfire
{
    ///<summary>
    ///Clase usada por Hangfire para el guardado de las tareas
    ///</summary>

    [Table("counter", Schema = "hangfire")]
    public partial class Counter
    {
        [Column(Order = 0)]
        [StringLength(100)]
        public string Key { get; set; }

        [Column(Order = 1)]
        public int Value { get; set; }

        public DateTime? ExpireAt { get; set; }
    }
}

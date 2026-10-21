using System;
using System.Collections.Generic;
using System.Text;

namespace WRKJRNL01.Data.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Holidays")]
    public class Holiday
    {
        [Key]
        [Column("pk_Id")]
        public Guid Id { get; set; }

        [Column("date")]
        public DateTime? Date { get; set; }

        public short FY { get; set; }
    }
}

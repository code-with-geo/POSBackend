using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class Products
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int Units {  get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }
    }
}

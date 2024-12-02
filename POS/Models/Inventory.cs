using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Models
{
    public class Inventory
    {
        public int InventoryId { get; set; }
        public int Units { get; set; }
        public int? ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Products? Products { get; set; }
        public int? LocationId { get; set; }
        [ForeignKey("LocationId")]
        public Locations? Locations { get; set; }
        [Required]
        [Range(0, 1, ErrorMessage = "Status must be 0 (inactive) or 1 (active).")]
        public int Status { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime DateCreated { get; set; }


    }
}

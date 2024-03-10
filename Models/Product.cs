using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ShopMobile.Models
{
    public class Product
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int? ProductId { set; get; }

        [Required(ErrorMessage = "Title không được để trống")]
        [StringLength(50, MinimumLength = 5)]
        public string title { set; get; }

        [Required(ErrorMessage = "Description không được để trống")]
        [StringLength(50, MinimumLength = 5)]
        public string description { set; get; }

        [Required(ErrorMessage = "price không được để trống")]
        public string price { set; get; }

        [Required(ErrorMessage = "quantity không được để trống")]
        [Range(1, Int32.MaxValue)] //  minimum value 1
        public int quantity { set; get; }

        [Required(ErrorMessage = "color không được để trống")]
        public string color { set; get; }

        [Required(ErrorMessage = "images không được để trống")]
        public string images { set; get; }


        public int? CategoryId { set; get; }


        public Category? Category { get; set; }

        public int? UserId { get; set; }

        public User? User { get; set; }

        public ICollection<Invoice_Products>? Invoice_Products { get; set; }



    }

    public class ProductOrder
    {
        [Required]
        public int ProductId;
        [Required]
        public int quantity;
        [Required]
        public string size;
    }
}

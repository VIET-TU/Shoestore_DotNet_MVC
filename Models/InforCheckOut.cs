using System.ComponentModel.DataAnnotations;

namespace ShopMobile.Models
{
    public class InforCheckOut
    {
        [Required]
        public string fristName { get; set; }
        [Required]
        public string lastName { get; set; }
        [Required]
        public string email { get; set; }
        [Required]
        public string phone { get; set; }
        [Required]
        public string address { get; set; }
        [Required]
        public string note { get; set; }
        [Required]
        public int InvoiceId { get; set; }
    }
}

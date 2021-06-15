using System.ComponentModel.DataAnnotations;

namespace Arcus.API.Market.Contracts
{
    public class OrderRequest
    {
        [Required]
        [Range(0, 100)]
        public int Amount { get; set; }
    }
}

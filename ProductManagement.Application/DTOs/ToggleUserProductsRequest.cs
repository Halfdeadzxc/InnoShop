using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Application.DTOs
{
    public class ToggleUserProductsRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public bool IsActive { get; set; }
    }
}

namespace ProductManagement.Application.DTOs
{
    public class BulkUpdateStatusDto
    {
        public List<Guid> ProductIds { get; set; } = new();
        public bool IsAvailable { get; set; }
    }
}

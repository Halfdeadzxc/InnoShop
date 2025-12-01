namespace UserManagement.Application.DTOs
{
    public class BulkUpdateUserStatusDto
    {
        public List<Guid> UserIds { get; set; }
        public bool IsActive { get; set; }
    }
}

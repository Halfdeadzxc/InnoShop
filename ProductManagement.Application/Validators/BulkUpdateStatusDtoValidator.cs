using FluentValidation;
using ProductManagement.Application.DTOs;

namespace ProductManagement.Application.Validators
{
    public class BulkUpdateStatusDtoValidator : AbstractValidator<BulkUpdateStatusDto>
    {
        public BulkUpdateStatusDtoValidator()
        {
            RuleFor(x => x.ProductIds)
                .NotEmpty().WithMessage("At least one product ID is required")
                .Must(ids => ids.Count <= 50).WithMessage("Cannot update more than 50 products at once")
                .ForEach(id => id.NotEmpty().WithMessage("Product ID cannot be empty"));

            RuleFor(x => x.IsAvailable)
                .NotNull().WithMessage("Availability status is required");
        }
    }
}
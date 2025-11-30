using FluentValidation;
using ProductManagement.Application.DTOs;

namespace ProductManagement.Application.Validators
{
    public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name cannot be empty")
                .When(x => !string.IsNullOrEmpty(x.Name))
                .Length(2, 100).WithMessage("Product name must be between 2 and 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Name))
                .Matches(@"^[a-zA-Z0-9\s\-_.,!?()]*$").WithMessage("Product name contains invalid characters")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Product description cannot be empty")
                .When(x => !string.IsNullOrEmpty(x.Description))
                .Length(10, 1000).WithMessage("Product description must be between 10 and 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0")
                .When(x => x.Price.HasValue)
                .LessThanOrEqualTo(1000000).WithMessage("Price must be less than or equal to 1,000,000")
                .When(x => x.Price.HasValue);
        }
    }
}
using FluentValidation;
using ProductManagement.Application.DTOs;

namespace ProductManagement.Application.Validators
{
    public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
    {
        public CreateProductDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required")
                .Length(2, 100).WithMessage("Product name must be between 2 and 100 characters")
                .Matches(@"^[a-zA-Z0-9\s\-_.,!?()]+$").WithMessage("Product name contains invalid characters");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Product description is required")
                .Length(10, 1000).WithMessage("Product description must be between 10 and 1000 characters");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0")
                .LessThanOrEqualTo(1000000).WithMessage("Price must be less than or equal to 1,000,000");


        }
    }
}
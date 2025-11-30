using FluentValidation;
using ProductManagement.Application.DTOs;

namespace ProductManagement.Application.Validators
{
    public class ProductQueryDtoValidator : AbstractValidator<ProductQueryDto>
    {
        public ProductQueryDtoValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0).WithMessage("Page must be greater than 0")
                .LessThanOrEqualTo(1000).WithMessage("Page must be less than or equal to 1000");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Page size must be less than or equal to 100");

            RuleFor(x => x.Search)
                .MaximumLength(50).WithMessage("Search term must not exceed 50 characters")
                .When(x => !string.IsNullOrEmpty(x.Search));

            RuleFor(x => x.MinPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Minimum price must be greater than or equal to 0")
                .When(x => x.MinPrice.HasValue);

            RuleFor(x => x.MaxPrice)
                .GreaterThan(0).WithMessage("Maximum price must be greater than 0")
                .When(x => x.MaxPrice.HasValue)
                .Must((query, maxPrice) => maxPrice > query.MinPrice)
                .WithMessage("Maximum price must be greater than minimum price")
                .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue);

            RuleFor(x => x.SortBy)
                .Must(BeAValidSortField).WithMessage("Invalid sort field")
                .When(x => !string.IsNullOrEmpty(x.SortBy));
        }

        private static bool BeAValidSortField(string? sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return true;

            var validFields = new[] { "name", "price", "createdat", "updatedat" };
            return validFields.Contains(sortBy.ToLower());
        }
    }
}
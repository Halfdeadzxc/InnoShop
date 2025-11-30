using FluentValidation;
using UserManagement.Application.DTOs;
using UserManagement.Domain.Enums;

namespace UserManagement.Application.Validators
{
    public class UserQueryDtoValidator : AbstractValidator<UserQueryDto>
    {
        public UserQueryDtoValidator()
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

            RuleFor(x => x.Role)
                .Must(BeAValidRole).WithMessage("Invalid role specified")
                .When(x => !string.IsNullOrEmpty(x.Role));

            RuleFor(x => x.SortBy)
                .Must(BeAValidSortField).WithMessage("Invalid sort field")
                .When(x => !string.IsNullOrEmpty(x.SortBy));
        }

        private static bool BeAValidRole(string? role)
        {
            if (string.IsNullOrEmpty(role)) return true;
            return Enum.TryParse<UserRole>(role, true, out _);
        }

        private static bool BeAValidSortField(string? sortBy)
        {
            if (string.IsNullOrEmpty(sortBy)) return true;

            var validFields = new[] { "email", "firstname", "lastname", "createdat" };
            return validFields.Contains(sortBy.ToLower());
        }
    }
}
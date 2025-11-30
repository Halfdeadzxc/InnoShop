using FluentValidation;
using UserManagement.Application.DTOs;
using UserManagement.Domain.Enums;

namespace UserManagement.Application.Validators
{
    public class UpdateUserRoleDtoValidator : AbstractValidator<string>
    {
        public UpdateUserRoleDtoValidator()
        {
            RuleFor(role => role)
                .NotEmpty().WithMessage("Role is required")
                .Must(BeAValidRole).WithMessage("Invalid role specified. Valid roles are: User, Admin");
        }

        private static bool BeAValidRole(string role)
        {
            return Enum.TryParse<UserRole>(role, true, out _);
        }
    }
}
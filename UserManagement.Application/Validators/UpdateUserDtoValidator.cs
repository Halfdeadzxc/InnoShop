using FluentValidation;
using UserManagement.Application.DTOs;

namespace UserManagement.Application.Validators
{
    public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name cannot be empty")
                .When(x => !string.IsNullOrEmpty(x.FirstName))
                .Length(2, 50).WithMessage("First name must be between 2 and 50 characters")
                .When(x => !string.IsNullOrEmpty(x.FirstName))
                .Matches(@"^[a-zA-Zа-яА-Я\s\-']*$").WithMessage("First name can only contain letters, spaces, hyphens and apostrophes")
                .When(x => !string.IsNullOrEmpty(x.FirstName));

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name cannot be empty")
                .When(x => !string.IsNullOrEmpty(x.LastName))
                .Length(2, 50).WithMessage("Last name must be between 2 and 50 characters")
                .When(x => !string.IsNullOrEmpty(x.LastName))
                .Matches(@"^[a-zA-Zа-яА-Я\s\-']*$").WithMessage("Last name can only contain letters, spaces, hyphens and apostrophes")
                .When(x => !string.IsNullOrEmpty(x.LastName));

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format")
                .When(x => !string.IsNullOrEmpty(x.Email))
                .MaximumLength(100).WithMessage("Email must not exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Email))
                .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").WithMessage("Invalid email format")
                .When(x => !string.IsNullOrEmpty(x.Email));
        }
    }
}
using FluentValidation.TestHelper;
using UserManagement.Application.DTOs;
using UserManagement.Application.Validators;

namespace UserManagement.UnitTests.Validators
{
    public class RegisterUserDtoValidatorTests
    {
        private readonly RegisterUserDtoValidator _validator;

        public RegisterUserDtoValidatorTests()
        {
            _validator = new RegisterUserDtoValidator();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_HaveError_When_FirstNameIsEmpty(string firstName)
        {
            // Arrange
            var model = new RegisterUserDto
            {
                FirstName = firstName,
                LastName = "Doe",
                Email = "test@example.com",
                Password = "Password123!"
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.FirstName);
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("valid-name")]
        [InlineData("John")]
        public void Should_NotHaveError_When_FirstNameIsValid(string firstName)
        {
            // Arrange
            var model = new RegisterUserDto
            {
                FirstName = firstName,
                LastName = "Doe",
                Email = "test@example.com",
                Password = "Password123!"
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("missing.at.com")]
        [InlineData("@missingusername.com")]
        public void Should_HaveError_When_EmailIsInvalid(string email)
        {
            // Arrange
            var model = new RegisterUserDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = email,
                Password = "Password123!"
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Theory]
        [InlineData("weak")]
        [InlineData("short")]
        [InlineData("nouppercase123!")]
        [InlineData("NOLOWERCASE123!")]
        [InlineData("NoNumber!")]
        public void Should_HaveError_When_PasswordIsWeak(string password)
        {
            // Arrange
            var model = new RegisterUserDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "test@example.com",
                Password = password
            };

            // Act
            var result = _validator.TestValidate(model);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }
    }
}
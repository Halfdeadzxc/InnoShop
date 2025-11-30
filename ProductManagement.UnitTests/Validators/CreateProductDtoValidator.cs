using FluentValidation.TestHelper;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Validators;
using Xunit;

namespace ProductManagement.UnitTests.Validators
{
    public class CreateProductDtoValidatorTests
    {
        private readonly CreateProductDtoValidator _validator;

        public CreateProductDtoValidatorTests()
        {
            _validator = new CreateProductDtoValidator();
        }

        [Fact]
        public void Validate_ValidData_ShouldNotHaveErrors()
        {
            var dto = new CreateProductDto
            {
                Name = "Valid Product Name",
                Description = "This is a valid product description that meets length requirements",
                Price = 99.99m,
                IsAvailable = true
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Validate_EmptyName_ShouldHaveValidationError(string name)
        {
            var dto = new CreateProductDto
            {
                Name = name,
                Description = "Valid description",
                Price = 99.99m
            };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Product name is required");
        }

        [Theory]
        [InlineData("A")] 
        [InlineData("AB")] 
        public void Validate_InvalidNameLength_ShouldHaveValidationError(string name)
        {
            var dto = new CreateProductDto
            {
                Name = name,
                Description = "Valid description",
                Price = 99.99m
            };

            var result = _validator.TestValidate(dto);

            if (name.Length < 2)
            {
                result.ShouldHaveValidationErrorFor(x => x.Name)
                    .WithErrorMessage("Product name must be between 2 and 100 characters");
            }
            else
            {
                result.ShouldNotHaveValidationErrorFor(x => x.Name);
            }
        }

        [Fact]
        public void Validate_NameTooLong_ShouldHaveValidationError()
        {
            var longName = new string('x', 101); 
            var dto = new CreateProductDto
            {
                Name = longName,
                Description = "Valid description",
                Price = 99.99m
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Product name must be between 2 and 100 characters");
        }

        [Fact]
        public void Validate_NameExactly100Characters_ShouldNotHaveValidationError()
        {
            var exactLengthName = new string('x', 100); 
            var dto = new CreateProductDto
            {
                Name = exactLengthName,
                Description = "Valid description",
                Price = 99.99m
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Theory]
        [InlineData("Product@Name")]
        [InlineData("Product$Name")]
        [InlineData("Product#Name")]
        [InlineData("Product%Name")]
        [InlineData("Product<Name")]
        [InlineData("Product>Name")]
        public void Validate_InvalidNameCharacters_ShouldHaveValidationError(string name)
        {
            
            var dto = new CreateProductDto
            {
                Name = name,
                Description = "Valid description",
                Price = 99.99m
            };

            
            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Product name contains invalid characters");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Validate_EmptyDescription_ShouldHaveValidationError(string description)
        {
            var dto = new CreateProductDto
            {
                Name = "Valid Product",
                Description = description,
                Price = 99.99m
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Product description is required");
        }

        [Fact]
        public void Validate_DescriptionTooShort_ShouldHaveValidationError()
        {
            var dto = new CreateProductDto
            {
                Name = "Valid Product",
                Description = "Too short",
                Price = 99.99m
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Product description must be between 10 and 1000 characters");
        }

        [Fact]
        public void Validate_DescriptionTooLong_ShouldHaveValidationError()
        {
            var longDescription = new string('x', 1001); 
            var dto = new CreateProductDto
            {
                Name = "Valid Product",
                Description = longDescription,
                Price = 99.99m
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Product description must be between 10 and 1000 characters");
        }

        [Fact]
        public void Validate_DescriptionExactly10Characters_ShouldNotHaveValidationError()
        {
            var exactLengthDescription = new string('x', 10); 
            var dto = new CreateProductDto
            {
                Name = "Valid Product",
                Description = exactLengthDescription,
                Price = 99.99m
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_DescriptionExactly1000Characters_ShouldNotHaveValidationError()
        {
            var exactLengthDescription = new string('x', 1000); 
            var dto = new CreateProductDto
            {
                Name = "Valid Product",
                Description = exactLengthDescription,
                Price = 99.99m
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Validate_InvalidPrice_ShouldHaveValidationError(decimal price)
        {
            var dto = new CreateProductDto
            {
                Name = "Valid Product",
                Description = "Valid description",
                Price = price
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Price)
                .WithErrorMessage("Price must be greater than 0");
        }

        [Fact]
        public void Validate_PriceTooHigh_ShouldHaveValidationError()
        {
            var dto = new CreateProductDto
            {
                Name = "Valid Product",
                Description = "Valid description",
                Price = 1000001
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Price)
                .WithErrorMessage("Price must be less than or equal to 1,000,000");
        }

        [Theory]
        [InlineData("Valid-Product.Name")]
        [InlineData("Valid Product Name")]
        [InlineData("Valid_Product_Name")]
        [InlineData("Product 123")]
        [InlineData("Product (Special Edition)")]
        [InlineData("Product, with comma")]
        [InlineData("Product!")]
        [InlineData("Product?")]
        public void Validate_ValidNameCharacters_ShouldNotHaveValidationError(string name)
        {
            var dto = new CreateProductDto
            {
                Name = name,
                Description = "Valid description that meets the length requirements",
                Price = 99.99m
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(999999.99)]
        [InlineData(1000000)] 
        public void Validate_ValidPrice_ShouldNotHaveValidationError(decimal price)
        {
            
            var dto = new CreateProductDto
            {
                Name = "Valid Product",
                Description = "Valid description",
                Price = price
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.Price);
        }

        [Fact]
        public void Validate_PriceExactlyOneMillion_ShouldNotHaveValidationError()
        {
            var dto = new CreateProductDto
            {
                Name = "Valid Product",
                Description = "Valid description",
                Price = 1000000
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.Price);
        }
    }
}
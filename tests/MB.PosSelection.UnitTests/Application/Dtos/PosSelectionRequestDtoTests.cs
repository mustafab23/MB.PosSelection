using FluentAssertions;
using MB.PosSelection.Application.Dtos;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace MB.PosSelection.UnitTests.Application.Dtos
{
    public class PosSelectionRequestDtoTests
    {
        private IList<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        [Fact]
        public void Should_Pass_Validation_With_Valid_Data()
        {
            var dto = new PosSelectionRequestDto
            {
                Amount = 100,
                Currency = "TRY",
                Installment = 1,
                CardType = "Credit"
            };

            var errors = Validate(dto);
            errors.Should().BeEmpty();
        }

        [Fact]
        public void Should_Fail_Validation_When_Amount_Is_Zero()
        {
            var dto = new PosSelectionRequestDto { Amount = 0, Currency = "TRY" };
            var errors = Validate(dto);

            errors.Should().Contain(e => e.ErrorMessage.Contains("Tutar 0'dan büyük olmalıdır"));
        }

        [Fact]
        public void Should_Fail_Validation_When_Currency_Is_Invalid()
        {
            var dto = new PosSelectionRequestDto { Amount = 100, Currency = "JPY" }; // Desteklenmeyen kur
            var errors = Validate(dto);

            errors.Should().Contain(e => e.ErrorMessage.Contains("Sadece TRY, USD veya EUR"));
        }

        [Fact]
        public void Should_Fail_Validation_When_Installment_Is_Out_Of_Range()
        {
            var dto = new PosSelectionRequestDto { Amount = 100, Installment = 13 };
            var errors = Validate(dto);

            errors.Should().Contain(e => e.ErrorMessage.Contains("Taksit sayısı 1 ile 12 arasında"));
        }
    }
}

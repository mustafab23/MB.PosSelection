using FluentAssertions;
using MB.PosSelection.Domain.Entities;
using MB.PosSelection.Domain.Enums;
using Xunit;

namespace MB.PosSelection.UnitTests.Domain.Entities
{
    public class PosRatioTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_Should_Throw_ArgumentNullException_When_BatchId_Is_Invalid(string invalidBatchId)
        {
            // Act
            Action act = () => new PosRatio(
                invalidBatchId, "Garanti", CardType.Credit, "bonus", 1, Currency.TRY, 0.01m, 0, 1);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_Should_Throw_ArgumentNullException_When_PosName_Is_Invalid(string invalidPosName)
        {
            // Act
            Action act = () => new PosRatio(
                "batch-1", invalidPosName, CardType.Credit, "bonus", 1, Currency.TRY, 0.01m, 0, 1);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_Should_Throw_ArgumentException_When_CommissionRate_Is_Negative()
        {
            // Act
            Action act = () => new PosRatio(
                "batch-1", "Garanti", CardType.Credit, "bonus", 1, Currency.TRY, -0.01m, 0, 1);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Komisyon oranı 0'dan küçük olamaz*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_Should_Throw_ArgumentException_When_Installment_Is_Less_Than_One(int invalidInstallment)
        {
            // Act
            Action act = () => new PosRatio(
                "batch-1", "Garanti", CardType.Credit, "bonus", invalidInstallment, Currency.TRY, 0.01m, 0, 1);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Taksit sayısı en az 1 olmalıdır*");
        }

        [Fact]
        public void Constructor_Should_LowerCase_CardBrand()
        {
            // Arrange & Act
            var posRatio = new PosRatio(
                "batch-1", "Garanti", CardType.Credit, "BoNuS", 1, Currency.TRY, 0.01m, 0, 1);

            // Assert
            posRatio.CardBrand.Should().Be("bonus"); // Küçük harfe çevrilmeli
        }
    }
}

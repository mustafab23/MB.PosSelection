using FluentAssertions;
using MB.PosSelection.Application.Models;
using MB.PosSelection.Domain.Entities;
using MB.PosSelection.Domain.Enums;
using Xunit;

namespace MB.PosSelection.UnitTests.Application.Models
{
    public class PosRateLookupIndexTests
    {
        private PosRatio CreateRatio(string currency, int installment, string cardType, string cardBrand)
        {
            return new PosRatio(
                "b1", "PosA",
                Enum.Parse<CardType>(cardType, true),
                cardBrand,
                installment,
                Enum.Parse<Currency>(currency, true),
                0.01m, 0, 1);
        }

        [Fact]
        public void FindRates_Should_Return_Rates_When_Exact_Match_Found()
        {
            // Arrange
            var ratio = CreateRatio("TRY", 6, "Credit", "bonus");
            var index = new PosRateLookupIndex(new[] { ratio });

            // Act
            var result = index.FindRates("TRY", 6, "Credit", "bonus");

            // Assert
            result.Should().ContainSingle();
            result.First().Should().Be(ratio);
        }

        [Fact]
        public void FindRates_Should_Be_Case_Insensitive()
        {
            // Arrange
            var ratio = CreateRatio("TRY", 6, "Credit", "bonus"); // Küçük harf kayıt
            var index = new PosRateLookupIndex(new[] { ratio });

            // Act: Büyük harf ile arama
            var result = index.FindRates("try", 6, "CREDIT", "BONUS");

            // Assert
            result.Should().ContainSingle();
        }

        [Fact]
        public void FindRates_Should_Handle_Null_Or_Empty_CardBrand_Correctly()
        {
            // Arrange: Markası olmayan (null) bir kayıt oluşturuyoruz. 
            // Index bunu "NONE" olarak saklamalı.
            var ratio = new PosRatio("b1", "PosB", CardType.Debit, "", 1, Currency.USD, 0.01m, 0, 1);
            var index = new PosRateLookupIndex(new[] { ratio });

            // Act: Null ile arama yapıyoruz
            var result = index.FindRates("USD", 1, "Debit", null);

            // Assert
            result.Should().ContainSingle();
            result.First().PosName.Should().Be("PosB");
        }

        [Fact]
        public void FindRates_Should_Return_Empty_List_When_No_Match()
        {
            var ratio = CreateRatio("TRY", 6, "Credit", "bonus");
            var index = new PosRateLookupIndex(new[] { ratio });

            // Taksit sayısı tutmuyor
            var result = index.FindRates("TRY", 12, "Credit", "bonus");

            result.Should().BeEmpty();
        }
    }
}

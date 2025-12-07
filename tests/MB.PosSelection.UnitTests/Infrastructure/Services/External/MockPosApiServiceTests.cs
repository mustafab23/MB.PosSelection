using FluentAssertions;
using MB.PosSelection.Application.Dtos.External;
using MB.PosSelection.Application.Interfaces;
using MB.PosSelection.Infrastructure.Services.External;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace MB.PosSelection.UnitTests.Infrastructure.Services.External
{
    public class MockPosApiServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<IPosMetricsService> _mockMetrics;
        private readonly HttpClient _httpClient;
        private readonly MockPosApiService _service;

        public MockPosApiServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockMetrics = new Mock<IPosMetricsService>();

            // Mock Handler ile HttpClient oluşturuyoruz
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://test-api.com/")
            };

            _service = new MockPosApiService(_httpClient, _mockMetrics.Object);
        }

        [Fact]
        public async Task GetRatesAsync_Should_Return_List_When_Api_Call_Is_Successful()
        {
            // Arrange
            var fakeResponse = new List<PosRatioExternalDto>
        {
            new() { PosName = "TestBank", CommissionRate = 0.01m }
        };
            var jsonResponse = JsonSerializer.Serialize(fakeResponse);

            // HttpClient.SendAsync metodunu mockluyoruz
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // Act
            var result = await _service.GetRatesAsync(CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().PosName.Should().Be("TestBank");

            // Metrik kaydedildi mi?
            _mockMetrics.Verify(m => m.RecordExternalApiDuration(It.IsAny<double>()), Times.Once);
        }

        [Fact]
        public async Task GetRatesAsync_Should_Return_Empty_List_When_Api_Returns_Null_Or_Empty()
        {
            // Arrange
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("[]") // Boş array
                });

            // Act
            var result = await _service.GetRatesAsync(CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRatesAsync_Should_Throw_Exception_When_Api_Fails()
        {
            // Arrange
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError // 500 Hatası
                });

            // Act
            Func<Task> act = async () => await _service.GetRatesAsync(CancellationToken.None);

            // Assert
            // Servisimiz 500 hatasını yakalayıp kendi formatında fırlatmalı
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("*Mock API*başarısız*");

            // Hata olsa bile süre ölçülmeli (finally bloğu)
            _mockMetrics.Verify(m => m.RecordExternalApiDuration(It.IsAny<double>()), Times.Once);
        }
    }
}

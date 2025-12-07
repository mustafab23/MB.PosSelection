using MB.PosSelection.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MB.PosSelection.Api.Middlewares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "İşlenemeyen bir hata oluştu: {Message}", exception.Message);

            var problemDetails = new ProblemDetails
            {
                Instance = httpContext.Request.Path
            };

            switch (exception)
            {
                case NotFoundException notFound:
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    problemDetails.Title = "Kayıt Bulunamadı";
                    problemDetails.Detail = notFound.Message;
                    problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                    break;

                case ValidationException validation:
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "Doğrulama Hatası";
                    problemDetails.Detail = validation.Message;
                    break;
                
                case ArgumentException argument:
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "Geçersiz Argüman";
                    problemDetails.Detail = argument.Message;
                    break;
                
                default:
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    problemDetails.Title = "Sunucu Hatası";
                    problemDetails.Detail = "İşleminiz sırasında beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.";
                    break;
            }

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}

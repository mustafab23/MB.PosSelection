using MB.PosSelection.Application.Dtos;
using MB.PosSelection.Application.Features.Pos.Commands.SyncPosRates;
using MB.PosSelection.Application.Features.Pos.Queries.SelectBestPos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MB.PosSelection.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PosSelectionController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PosSelectionController> _logger;

        public PosSelectionController(IMediator mediator, ILogger<PosSelectionController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Verilen işlem kriterlerine göre en düşük maliyetli Sanal POS'u hesaplar ve seçer.
        /// </summary>
        /// <remarks>
        /// Algoritma şu öncelik sırasına göre çalışır:
        /// 1. En düşük toplam maliyet (Komisyon + Kur Farkı + Min Fee)
        /// 2. En yüksek POS öncelik puanı (Priority)
        /// 3. En düşük komisyon oranı
        /// 4. POS adı (Alfabetik)
        /// 
        /// **Örnek İstek:**
        /// 
        ///     POST /api/v1/posselection/select-best
        ///     {
        ///        "amount": 100.00,
        ///        "installment": 3,
        ///        "currency": "TRY",
        ///        "card_type": "Credit",
        ///        "card_brand": "Bonus"
        ///     }
        /// 
        /// </remarks>
        /// <param name="request">İşlem tutarı, taksit ve kart bilgilerini içeren model.</param>
        /// <returns>Seçilen POS bilgileri ve hesaplanan maliyet detayları.</returns>
        /// <response code="200">Başarılı. En uygun POS bulundu.</response>
        /// <response code="400">Hatalı istek. Validasyon kurallarına uymayan veri.</response>
        /// <response code="404">Kriterlere uygun hiçbir POS bulunamadı.</response>
        /// <response code="500">Sunucu hatası.</response>
        [HttpPost("select-best")]
        [ProducesResponseType(typeof(PosSelectionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SelectBestPos([FromBody] PosSelectionRequestDto request)
        {
            var resultWrapper = await _mediator.Send(new SelectBestPosQuery(request));

            Response.Headers.Append("X-Pos-Evaluated-Count", resultWrapper.Meta.EvaluatedCount.ToString());
            Response.Headers.Append("X-Pos-Selection-Reason", resultWrapper.Meta.SelectionReason);
            Response.Headers.Append("X-Pos-Winner", resultWrapper.Meta.WinnerPosName);
            Response.Headers.Append("X-Server-Execution-Time-Ms", resultWrapper.Meta.ExecutionTimeMs.ToString("F2"));

            var finalResponse = new PosSelectionResultDto(request, resultWrapper.Data);

            return Ok(finalResponse);
        }

        /// <summary>
        /// [ADMIN] POS oranlarını dış servisten (Mock API) manuel olarak çeker ve günceller.
        /// </summary>
        /// <remarks>
        /// Bu işlem normalde her gece 23:59'da otomatik çalışır. 
        /// Acil durumlarda veya test amaçlı manuel tetiklemek için kullanılır.
        /// İşlem şunları yapar:
        /// - Dış API'den veriyi çeker.
        /// - Veritabanına yazar.
        /// - Cache'i (Redis + Memory) temizler ve günceller.
        /// </remarks>
        /// <returns>İşlem sonucu mesajı.</returns>
        /// <response code="200">Senkronizasyon başarılı.</response>
        /// <response code="500">Dış servis hatası veya veritabanı yazma hatası.</response>
        [HttpPost("sync-rates")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SyncRates()
        {
            _logger.LogInformation("Manuel POS senkronizasyonu tetiklendi.");

            var command = new SyncPosRatesCommand();
            var result = await _mediator.Send(command);

            return result
                ? Ok(new { Message = "POS oranları başarıyla güncellendi." })
                : StatusCode(500, "Senkronizasyon başarısız oldu.");
        }
    }
}

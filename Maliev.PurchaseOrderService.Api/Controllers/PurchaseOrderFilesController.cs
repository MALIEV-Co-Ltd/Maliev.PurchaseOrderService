namespace Maliev.PurchaseOrderService.Api.Controllers
{
    using Maliev.PurchaseOrderService.Api.DTOs;
    using Maliev.PurchaseOrderService.Api.Services;
    using Microsoft.AspNetCore.Mvc;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Asp.Versioning;

    [ApiController]
    [Route("v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class PurchaseOrderFilesController : ControllerBase
    {
        private readonly IPurchaseOrderService _purchaseOrderService;

        public PurchaseOrderFilesController(IPurchaseOrderService purchaseOrderService)
        {
            _purchaseOrderService = purchaseOrderService;
        }

        [HttpGet("ByPurchaseOrder/{purchaseOrderId}")]
        public async Task<ActionResult<IEnumerable<PurchaseOrderFileDto>>> GetPurchaseOrderFilesByPurchaseOrderId(int purchaseOrderId)
        {
            var purchaseOrderFiles = await _purchaseOrderService.GetPurchaseOrderFilesByPurchaseOrderIdAsync(purchaseOrderId);
            return Ok(purchaseOrderFiles);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseOrderFileDto>> GetPurchaseOrderFile(int id)
        {
            var purchaseOrderFile = await _purchaseOrderService.GetPurchaseOrderFileByIdAsync(id);
            if (purchaseOrderFile == null)
            {
                return NotFound();
            }
            return Ok(purchaseOrderFile);
        }

        [HttpPost]
        public async Task<ActionResult<PurchaseOrderFileDto>> PostPurchaseOrderFile(CreatePurchaseOrderFileDto purchaseOrderFileDto)
        {
            var purchaseOrderFile = await _purchaseOrderService.CreatePurchaseOrderFileAsync(purchaseOrderFileDto);
            return CreatedAtAction(nameof(GetPurchaseOrderFile), new { id = purchaseOrderFile.Id }, purchaseOrderFile);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutPurchaseOrderFile(int id, UpdatePurchaseOrderFileDto purchaseOrderFileDto)
        {
            var purchaseOrderFile = await _purchaseOrderService.UpdatePurchaseOrderFileAsync(id, purchaseOrderFileDto);
            if (purchaseOrderFile == null)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePurchaseOrderFile(int id)
        {
            var result = await _purchaseOrderService.DeletePurchaseOrderFileAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}

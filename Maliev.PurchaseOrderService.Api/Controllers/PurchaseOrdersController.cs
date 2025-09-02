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
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderService _purchaseOrderService;

        public PurchaseOrdersController(IPurchaseOrderService purchaseOrderService)
        {
            _purchaseOrderService = purchaseOrderService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetPurchaseOrders()
        {
            var purchaseOrders = await _purchaseOrderService.GetAllPurchaseOrdersAsync();
            return Ok(purchaseOrders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseOrderDto>> GetPurchaseOrder(int id)
        {
            var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id);
            if (purchaseOrder == null)
            {
                return NotFound();
            }
            return Ok(purchaseOrder);
        }

        [HttpPost]
        public async Task<ActionResult<PurchaseOrderDto>> PostPurchaseOrder(CreatePurchaseOrderDto purchaseOrderDto)
        {
            var purchaseOrder = await _purchaseOrderService.CreatePurchaseOrderAsync(purchaseOrderDto);
            return CreatedAtAction(nameof(GetPurchaseOrder), new { id = purchaseOrder.Id }, purchaseOrder);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutPurchaseOrder(int id, UpdatePurchaseOrderDto purchaseOrderDto)
        {
            var purchaseOrder = await _purchaseOrderService.UpdatePurchaseOrderAsync(id, purchaseOrderDto);
            if (purchaseOrder == null)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePurchaseOrder(int id)
        {
            var result = await _purchaseOrderService.DeletePurchaseOrderAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}

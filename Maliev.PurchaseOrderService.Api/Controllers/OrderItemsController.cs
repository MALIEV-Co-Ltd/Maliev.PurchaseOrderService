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
    public class OrderItemsController : ControllerBase
    {
        private readonly IPurchaseOrderService _purchaseOrderService;

        public OrderItemsController(IPurchaseOrderService purchaseOrderService)
        {
            _purchaseOrderService = purchaseOrderService;
        }

        [HttpGet("ByPurchaseOrder/{purchaseOrderId}")]
        public async Task<ActionResult<IEnumerable<OrderItemDto>>> GetOrderItemsByPurchaseOrderId(int purchaseOrderId)
        {
            var orderItems = await _purchaseOrderService.GetOrderItemsByPurchaseOrderIdAsync(purchaseOrderId);
            return Ok(orderItems);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderItemDto>> GetOrderItem(int id)
        {
            var orderItem = await _purchaseOrderService.GetOrderItemByIdAsync(id);
            if (orderItem == null)
            {
                return NotFound();
            }
            return Ok(orderItem);
        }

        [HttpPost]
        public async Task<ActionResult<OrderItemDto>> PostOrderItem(CreateOrderItemDto orderItemDto)
        {
            var orderItem = await _purchaseOrderService.CreateOrderItemAsync(orderItemDto);
            return CreatedAtAction(nameof(GetOrderItem), new { id = orderItem.Id }, orderItem);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrderItem(int id, UpdateOrderItemDto orderItemDto)
        {
            var orderItem = await _purchaseOrderService.UpdateOrderItemAsync(id, orderItemDto);
            if (orderItem == null)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrderItem(int id)
        {
            var result = await _purchaseOrderService.DeleteOrderItemAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}

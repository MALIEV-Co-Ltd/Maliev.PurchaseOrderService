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
    public class AddressesController : ControllerBase
    {
        private readonly IPurchaseOrderService _purchaseOrderService;

        public AddressesController(IPurchaseOrderService purchaseOrderService)
        {
            _purchaseOrderService = purchaseOrderService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AddressDto>>> GetAddresses()
        {
            var addresses = await _purchaseOrderService.GetAllAddressesAsync();
            return Ok(addresses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AddressDto>> GetAddress(int id)
        {
            var address = await _purchaseOrderService.GetAddressByIdAsync(id);
            if (address == null)
            {
                return NotFound();
            }
            return Ok(address);
        }

        [HttpPost]
        public async Task<ActionResult<AddressDto>> PostAddress(CreateAddressDto addressDto)
        {
            var address = await _purchaseOrderService.CreateAddressAsync(addressDto);
            return CreatedAtAction(nameof(GetAddress), new { id = address.Id }, address);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAddress(int id, UpdateAddressDto addressDto)
        {
            var address = await _purchaseOrderService.UpdateAddressAsync(id, addressDto);
            if (address == null)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var result = await _purchaseOrderService.DeleteAddressAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}

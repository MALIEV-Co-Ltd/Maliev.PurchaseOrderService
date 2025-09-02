namespace Maliev.PurchaseOrderService.Api.DTOs
{
    public class AddressDto
    {
        public int Id { get; set; }
        public required string Building { get; set; }
        public required string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
        public required string PostalCode { get; set; }
        public int CountryId { get; set; }
    }

    public class CreateAddressDto
    {
        public required string Building { get; set; }
        public required string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
        public required string PostalCode { get; set; }
        public int CountryId { get; set; }
    }

    public class UpdateAddressDto
    {
        public required string Building { get; set; }
        public required string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
        public required string PostalCode { get; set; }
        public int CountryId { get; set; }
    }
}

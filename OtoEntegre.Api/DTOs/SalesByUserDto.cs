namespace OtoEntegre.Api.DTOs
{
    public class SalesByUserDto
    {
        public Guid UserId { get; set; }
        public string? Name { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class SalesByUserResponseDto
    {
        public List<SalesByUserDto> Users { get; set; } = new();
        public SalesByUserDto? TopByCount { get; set; }
        public SalesByUserDto? TopByAmount { get; set; }
    }
}

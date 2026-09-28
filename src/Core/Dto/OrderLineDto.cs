namespace Core.Dto;

public record OrderLineDto(string OrderId, string ProductId, int Quantity, decimal UnitPrice);

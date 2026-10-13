namespace Core.Dto;

public record OrderDto(string Id, string CustomerId, string Status, IReadOnlyList<OrderLineDto> Lines);

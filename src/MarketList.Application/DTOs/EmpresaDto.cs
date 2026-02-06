namespace MarketList.Application.DTOs;

public record EmpresaDto(
    Guid Id,
    string Nome,
    string? Cnpj,
    DateTime CreatedAt,
    int QuantidadeListas
);

public record EmpresaCreateDto(
    string Nome,
    string? Cnpj
);

public record EmpresaUpdateDto(
    string Nome,
    string? Cnpj
);

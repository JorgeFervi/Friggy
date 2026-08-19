namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Datos necesarios para corregir la fecha de caducidad de un lote.
/// </summary>
public sealed record CorrectInventoryExpirationRequest(DateOnly ExpirationDate);

using System.ComponentModel.DataAnnotations;

namespace MarketList.Domain.Entities;

public class Usuario : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string SenhaHash { get; set; } = string.Empty;
}

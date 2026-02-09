using System.Threading;

namespace MarketList.Application.Interfaces;

public interface IEmpresaResolverService
{
    Task<Guid?> ResolverEmpresaIdPorNomeAsync(string nomeEmpresa, CancellationToken cancellationToken = default);
}

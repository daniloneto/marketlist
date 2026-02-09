using MarketList.Application.Interfaces;
using MarketList.Domain.Interfaces;
using MarketList.Application.Services;

namespace MarketList.Application.Services;

public class EmpresaResolverService : IEmpresaResolverService
{
    private readonly IEmpresaRepository _empresaRepository;
    private readonly ITextoNormalizacaoService _normalizacaoService;

    public EmpresaResolverService(IEmpresaRepository empresaRepository, ITextoNormalizacaoService normalizacaoService)
    {
        _empresaRepository = empresaRepository;
        _normalizacaoService = normalizacaoService;
    }

    public async Task<Guid?> ResolverEmpresaIdPorNomeAsync(string nomeEmpresa, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nomeEmpresa))
            return null;

        var normalizado = _normalizacaoService.Normalizar(nomeEmpresa);

        var empresas = await _empresaRepository.GetAllAsync(cancellationToken);
        var match = empresas.FirstOrDefault(e =>
            !string.IsNullOrWhiteSpace(e.Nome) &&
            _normalizacaoService.Normalizar(e.Nome) == normalizado);

        return match?.Id;
    }
}

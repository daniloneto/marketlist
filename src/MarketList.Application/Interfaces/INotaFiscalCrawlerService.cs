using MarketList.Application.DTOs;

namespace MarketList.Application.Interfaces;

/// <summary>
/// Serviço responsável por baixar o HTML da NFC-e (SEFAZ-BA) e extrair
/// o texto estruturado no formato aceito pelo pipeline de importação existente.
/// </summary>
public interface INotaFiscalCrawlerService
{
    /// <summary>
    /// Baixa o HTML da URL da NFC-e, faz o parse e retorna o texto
    /// no formato esperado pelo <see cref="ILeitorNotaFiscal"/>
    /// junto com a data de emissão extraída do HTML.
    /// A primeira linha do texto retornado é o nome da empresa.
    /// </summary>
    /// <param name="url">URL da NFC-e (ex: SEFAZ-BA)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>DTO com texto estruturado e data de emissão</returns>
    Task<NotaFiscalExtraidaDto> BaixarEExtrairTextoAsync(string url, CancellationToken cancellationToken = default);
}

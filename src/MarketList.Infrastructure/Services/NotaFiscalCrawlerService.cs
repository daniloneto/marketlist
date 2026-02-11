using HtmlAgilityPack;
using MarketList.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MarketList.Infrastructure.Services;

/// <summary>
/// Crawler da NFC-e SEFAZ-BA.
/// Baixa o HTML da URL, extrai empresa e itens, e retorna um TEXTO
/// no formato aceito pelo pipeline existente (LeitorNotaFiscal).
/// </summary>
public class NotaFiscalCrawlerService : INotaFiscalCrawlerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotaFiscalCrawlerService> _logger;

    public NotaFiscalCrawlerService(
        HttpClient httpClient,
        ILogger<NotaFiscalCrawlerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> BaixarEExtrairTextoAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("A URL da NFC-e não pode ser vazia.", nameof(url));

        _logger.LogInformation("Iniciando download da NFC-e: {Url}", url);

        // 1. Baixar o HTML
        string html;
        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            html = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha ao baixar HTML da NFC-e: {Url}", url);
            throw new InvalidOperationException($"Não foi possível acessar a URL da NFC-e: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout ao baixar HTML da NFC-e: {Url}", url);
            throw new InvalidOperationException("Timeout ao acessar a URL da NFC-e.", ex);
        }

        if (string.IsNullOrWhiteSpace(html))
        {
            throw new InvalidOperationException("O HTML retornado pela SEFAZ está vazio.");
        }

        // 2. Parse do HTML
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // 3. Extrair nome da empresa
        var nomeEmpresa = ExtrairNomeEmpresa(doc);
        if (string.IsNullOrWhiteSpace(nomeEmpresa))
        {
            _logger.LogWarning("Nome da empresa não encontrado no HTML da NFC-e: {Url}", url);
            throw new InvalidOperationException("Não foi possível extrair o nome da empresa do HTML da NFC-e. A estrutura da página pode ter mudado.");
        }

        // 4. Extrair itens
        var itens = ExtrairItens(doc);
        if (itens.Count == 0)
        {
            _logger.LogWarning("Nenhum item encontrado no HTML da NFC-e: {Url}", url);
            throw new InvalidOperationException("Nenhum produto encontrado no HTML da NFC-e. A estrutura da página pode ter mudado.");
        }

        // 5. Montar texto no formato do pipeline
        var texto = MontarTexto(nomeEmpresa, itens);

        _logger.LogInformation("NFC-e processada com sucesso: {Empresa}, {QtdItens} itens extraídos", nomeEmpresa, itens.Count);

        return texto;
    }

    /// <summary>
    /// Extrai o nome da empresa do HTML.
    /// Seletor: //div[@id='u20'] (classe "txtTopo")
    /// </summary>
    private string ExtrairNomeEmpresa(HtmlDocument doc)
    {
        // Tenta pelo id u20
        var node = doc.DocumentNode.SelectSingleNode("//div[@id='u20']");

        // Fallback: tenta pela classe txtTopo
        node ??= doc.DocumentNode.SelectSingleNode("//div[contains(@class,'txtTopo')]");

        if (node == null)
            return string.Empty;

        return LimparTexto(node.InnerText);
    }

    /// <summary>
    /// Extrai os itens da tabela de produtos.
    /// Seletor: //table[@id='tabResult']//tr
    /// </summary>
    private List<ItemCrawled> ExtrairItens(HtmlDocument doc)
    {
        var itens = new List<ItemCrawled>();

        var tableNode = doc.DocumentNode.SelectSingleNode("//table[@id='tabResult']");
        if (tableNode == null)
        {
            _logger.LogWarning("Tabela de produtos (tabResult) não encontrada no HTML");
            return itens;
        }

        var rows = tableNode.SelectNodes(".//tr");
        if (rows == null || rows.Count == 0)
        {
            _logger.LogWarning("Nenhuma linha encontrada na tabela de produtos");
            return itens;
        }

        foreach (var row in rows)
        {
            try
            {
                var item = ExtrairItemDeRow(row);
                if (item != null)
                {
                    itens.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao extrair item de uma linha da tabela. Continuando com próximo item.");
            }
        }

        return itens;
    }

    /// <summary>
    /// Extrai um item de um <tr> da tabela de produtos.
    /// </summary>
    private ItemCrawled? ExtrairItemDeRow(HtmlNode row)
    {
        // Nome do produto: span.txtTit
        var nomeNode = row.SelectSingleNode(".//span[contains(@class,'txtTit')]");
        if (nomeNode == null)
            return null; // Linha sem produto (pode ser header ou separador)

        var nome = LimparTexto(nomeNode.InnerText);
        if (string.IsNullOrWhiteSpace(nome))
            return null;

        // Código: span.RCod
        var codigoNode = row.SelectSingleNode(".//span[contains(@class,'RCod')]");
        var codigo = codigoNode != null ? LimparCodigo(codigoNode.InnerText) : string.Empty;

        // Quantidade: span.Rqtd
        var qtdNode = row.SelectSingleNode(".//span[contains(@class,'Rqtd')]");
        var quantidade = qtdNode != null ? LimparQuantidade(qtdNode.InnerText) : "1";

        // Unidade: span.RUN
        var unidadeNode = row.SelectSingleNode(".//span[contains(@class,'RUN')]");
        var unidade = unidadeNode != null ? LimparUnidade(unidadeNode.InnerText) : "UN";

        // Valor unitário: span.RvlUnit
        var vlUnitNode = row.SelectSingleNode(".//span[contains(@class,'RvlUnit')]");
        var valorUnitario = vlUnitNode != null ? LimparValor(vlUnitNode.InnerText) : "0,00";

        // Valor total: span.valor (no td da direita)
        var vlTotalNode = row.SelectSingleNode(".//td[last()]//span[contains(@class,'valor')]")
                       ?? row.SelectSingleNode(".//span[contains(@class,'valor')]");
        var valorTotal = vlTotalNode != null ? LimparValor(vlTotalNode.InnerText) : "0,00";

        return new ItemCrawled(nome, codigo, quantidade, unidade, valorUnitario, valorTotal);
    }

    /// <summary>
    /// Monta o texto final no formato aceito pelo pipeline existente (LeitorNotaFiscal).
    /// Formato:
    /// NOME DA EMPRESA
    /// (linha em branco)
    /// PRODUTO (Código: XXXX)
    /// Qtde.:1,915 UN: KG9 Vl. Unit.: 6,99 Vl. Total 13,39
    /// </summary>
    private static string MontarTexto(string nomeEmpresa, List<ItemCrawled> itens)
    {
        var sb = new StringBuilder();

        sb.AppendLine(nomeEmpresa);
        sb.AppendLine();

        foreach (var item in itens)
        {
            // Linha 1: NOME DO PRODUTO (Código: XXXX)
            if (!string.IsNullOrWhiteSpace(item.Codigo))
            {
                sb.AppendLine($"{item.Nome} (Código: {item.Codigo})");
            }
            else
            {
                sb.AppendLine($"{item.Nome} (Código: 000000)");
            }

            // Linha 2: Qtde.:X UN: YYY Vl. Unit.: Z,ZZ
            sb.AppendLine($"Qtde.:{item.Quantidade} UN: {item.Unidade} Vl. Unit.: {item.ValorUnitario}");

            // Linha 3: Valor total (apenas o número)
            sb.AppendLine(item.ValorTotal);

            // Linha em branco entre itens
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    #region Helpers de limpeza de texto

    /// <summary>
    /// Limpa texto genérico: decodifica HTML entities, remove espaços extras.
    /// </summary>
    private static string LimparTexto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var decoded = WebUtility.HtmlDecode(texto);
        // Remove quebras de linha e espaços extras
        decoded = Regex.Replace(decoded, @"\s+", " ");
        return decoded.Trim();
    }

    /// <summary>
    /// Limpa o campo de código, removendo prefixos como "Código:" e espaços.
    /// </summary>
    private static string LimparCodigo(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var limpo = LimparTexto(texto);
        // Remove prefixo "Código:" ou "(Código:" ou "Cod.:" etc.
        limpo = Regex.Replace(limpo, @"^[\(\s]*C[óo]d(?:igo)?\.?:?\s*", "", RegexOptions.IgnoreCase);
        limpo = limpo.TrimEnd(')', ' ');
        return limpo.Trim();
    }

    /// <summary>
    /// Limpa o campo de quantidade, removendo prefixos como "Qtde.:" e espaços.
    /// Mantém vírgula como separador decimal (pt-BR).
    /// </summary>
    private static string LimparQuantidade(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return "1";

        var limpo = LimparTexto(texto);
        // Remove prefixo "Qtde.:" ou "Qtd:" etc.
        limpo = Regex.Replace(limpo, @"^Qtd[e]?\.?:?\s*", "", RegexOptions.IgnoreCase);
        return limpo.Trim();
    }

    /// <summary>
    /// Limpa o campo de unidade, removendo prefixos como "UN:" e espaços.
    /// </summary>
    private static string LimparUnidade(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return "UN";

        var limpo = LimparTexto(texto);
        // Remove prefixo "UN:" etc.
        limpo = Regex.Replace(limpo, @"^UN\.?:?\s*", "", RegexOptions.IgnoreCase);
        return limpo.Trim();
    }

    /// <summary>
    /// Limpa o campo de valor, removendo prefixos e "R$".
    /// Mantém vírgula como separador decimal.
    /// </summary>
    private static string LimparValor(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return "0,00";

        var limpo = LimparTexto(texto);
        // Remove prefixo "Vl. Unit.:" ou "Vl. Total" ou "R$" etc.
        limpo = Regex.Replace(limpo, @"^Vl\.?\s*(Unit|Total)\.?:?\s*", "", RegexOptions.IgnoreCase);
        limpo = Regex.Replace(limpo, @"R\$\s*", "", RegexOptions.IgnoreCase);
        return limpo.Trim();
    }

    #endregion

    /// <summary>
    /// Record interno para representar um item extraído do HTML.
    /// </summary>
    private record ItemCrawled(
        string Nome,
        string Codigo,
        string Quantidade,
        string Unidade,
        string ValorUnitario,
        string ValorTotal
    );
}

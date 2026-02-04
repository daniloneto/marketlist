using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MarketList.Infrastructure.Services;

/// <summary>
/// Implementação do adapter para consulta de preços no serviço "Preço da Hora" da Bahia
/// </summary>
public class PrecoDaHoraPriceLookupService : IPriceLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PrecoDaHoraPriceLookupService> _logger;
    private const string BaseUrl = "https://precodahora.ba.gov.br";
    private const string ProductsEndpoint = "/produtos/";

    public PrecoDaHoraPriceLookupService(
        HttpClient httpClient,
        ILogger<PrecoDaHoraPriceLookupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configurar HttpClient
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    public async Task<PriceLookupResult> GetLatestPriceAsync(
        string productNameOrGtin,
        double latitude,
        double longitude,
        int hours = 24)
    {
        try
        {
            _logger.LogInformation(
                "Iniciando busca de preços para '{Product}' em lat={Lat}, lon={Lon}",
                productNameOrGtin, latitude, longitude);

            // Passo 1: Obter sessão e CSRF token
            var (csrfToken, cookies) = await ObterSessaoECsrfTokenAsync();
            
            if (string.IsNullOrEmpty(csrfToken))
            {
                _logger.LogWarning("Não foi possível obter o CSRF token");
                return new PriceLookupResult { Found = false };
            }

            // Passo 2: Consultar preços
            var htmlResponse = await ConsultarPrecosAsync(
                productNameOrGtin,
                latitude,
                longitude,
                hours,
                csrfToken,
                cookies);

            if (string.IsNullOrEmpty(htmlResponse))
            {
                _logger.LogWarning("Resposta vazia do servidor");
                return new PriceLookupResult { Found = false };
            }

            // Passo 3: Parsear HTML e extrair preços
            var resultado = ParsearResultados(htmlResponse);

            if (resultado.Found)
            {
                _logger.LogInformation(
                    "Preço encontrado: R$ {Price} em {Store}",
                    resultado.Price, resultado.StoreName ?? "estabelecimento não identificado");
            }
            else
            {
                _logger.LogInformation("Nenhum preço encontrado para '{Product}'", productNameOrGtin);
            }

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Erro ao buscar preços para '{Product}'", productNameOrGtin);
            
            // Nunca lançar exceção - retornar resultado negativo
            return new PriceLookupResult { Found = false };
        }
    }

    /// <summary>
    /// Obtém a sessão e o CSRF token fazendo GET na página de produtos
    /// </summary>
    private async Task<(string? CsrfToken, string? Cookies)> ObterSessaoECsrfTokenAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(ProductsEndpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao obter sessão: {StatusCode}", response.StatusCode);
                return (null, null);
            }

            // Capturar cookies de sessão
            var cookies = ExtrairCookies(response);

            // Parsear HTML para extrair CSRF token
            var html = await response.Content.ReadAsStringAsync();
            var csrfToken = ExtrairCsrfToken(html);

            return (csrfToken, cookies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter sessão e CSRF token");
            return (null, null);
        }
    }

    /// <summary>
    /// Extrai cookies da resposta HTTP
    /// </summary>
    private string? ExtrairCookies(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var cookieList = cookies
                .Select(c => c.Split(';')[0]) // Pega apenas nome=valor
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();
            
            return cookieList.Any() ? string.Join("; ", cookieList) : null;
        }

        return null;
    }

    /// <summary>
    /// Extrai o CSRF token do HTML
    /// Token está no atributo data-id do elemento com id="validate"
    /// </summary>
    private string? ExtrairCsrfToken(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var validateElement = doc.GetElementbyId("validate");
            if (validateElement != null)
            {
                var csrfToken = validateElement.GetAttributeValue("data-id", string.Empty);
                return !string.IsNullOrEmpty(csrfToken) ? csrfToken : null;
            }

            _logger.LogWarning("Elemento #validate não encontrado no HTML");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao extrair CSRF token do HTML");
            return null;
        }
    }

    /// <summary>
    /// Faz POST para consultar preços com os parâmetros especificados
    /// </summary>
    private async Task<string?> ConsultarPrecosAsync(
        string productNameOrGtin,
        double latitude,
        double longitude,
        int hours,
        string csrfToken,
        string? cookies)
    {
        try
        {
            // Preparar parâmetros para form-urlencoded
            var parameters = new Dictionary<string, string>
            {
                { "termo", productNameOrGtin ?? string.Empty },
                { "gtin", string.Empty },
                { "cnpj", string.Empty },
                { "horas", hours.ToString() },
                { "anp", string.Empty },
                { "codmun", string.Empty },
                { "latitude", latitude.ToString("F6", CultureInfo.InvariantCulture) },
                { "longitude", longitude.ToString("F6", CultureInfo.InvariantCulture) },
                { "raio", "15" },
                { "precomax", "0" },
                { "precomin", "0" },
                { "pagina", "1" },
                { "ordenar", "preco.asc" },
                { "categorias", string.Empty },
                { "processo", "carregar" },
                { "totalCategorias", string.Empty },
                { "totalRegistros", "0" },
                { "totalPaginas", "0" },
                { "pageview", "lista" }
            };

            var content = new FormUrlEncodedContent(parameters);

            // Criar requisição com headers apropriados
            var request = new HttpRequestMessage(HttpMethod.Post, ProductsEndpoint)
            {
                Content = content
            };

            request.Headers.Add("X-CSRFToken", csrfToken);
            
            if (!string.IsNullOrEmpty(cookies))
            {
                request.Headers.Add("Cookie", cookies);
            }

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Falha na consulta de preços: {StatusCode}", 
                    response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar preços");
            return null;
        }
    }

    /// <summary>
    /// Parseia o HTML retornado e extrai os preços, retornando o menor preço encontrado
    /// </summary>
    private PriceLookupResult ParsearResultados(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var precos = new List<(decimal Preco, string? NomeLoja)>();

            // Tentar encontrar elementos de preço (ajustar seletores conforme estrutura real do HTML)
            // Padrões comuns: .price, .preco, [data-price], etc.
            var possiveisSeletores = new[]
            {
                "//span[contains(@class, 'price')]",
                "//div[contains(@class, 'preco')]",
                "//span[contains(@class, 'valor')]",
                "//div[contains(@class, 'price')]",
                "//*[contains(text(), 'R$')]"
            };

            foreach (var seletor in possiveisSeletores)
            {
                var nodes = doc.DocumentNode.SelectNodes(seletor);
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        var textoPreco = node.InnerText;
                        var preco = NormalizarPreco(textoPreco);

                        if (preco.HasValue)
                        {
                            // Tentar encontrar nome da loja (buscar em elemento pai)
                            var nomeLoja = TentarExtrairNomeLoja(node);
                            precos.Add((preco.Value, nomeLoja));
                        }
                    }
                }

                // Se encontrou preços, não precisa tentar outros seletores
                if (precos.Any())
                    break;
            }

            if (!precos.Any())
            {
                _logger.LogInformation("Nenhum preço válido encontrado no HTML");
                return new PriceLookupResult { Found = false };
            }

            // Selecionar o menor preço
            var menorPreco = precos.OrderBy(p => p.Preco).First();

            return new PriceLookupResult
            {
                Found = true,
                Price = menorPreco.Preco,
                Date = DateTime.UtcNow,
                StoreName = menorPreco.NomeLoja
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao parsear resultados do HTML");
            return new PriceLookupResult { Found = false };
        }
    }

    /// <summary>
    /// Normaliza texto de preço para decimal
    /// Exemplo: "R$ 5,99" -> 5.99
    /// </summary>
    private decimal? NormalizarPreco(string textoPreco)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(textoPreco))
                return null;

            // Remover "R$" e espaços
            var limpo = textoPreco
                .Replace("R$", string.Empty)
                .Replace(" ", string.Empty)
                .Trim();

            // Trocar vírgula por ponto
            limpo = limpo.Replace(",", ".");

            // Extrair apenas números e ponto decimal usando regex
            var match = Regex.Match(limpo, @"\d+\.?\d*");
            if (!match.Success)
                return null;

            if (decimal.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var preco))
            {
                // Validar que o preço é razoável (maior que zero e menor que 100000)
                if (preco > 0 && preco < 100000)
                    return preco;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Tenta extrair o nome da loja do elemento pai ou irmãos
    /// </summary>
    private string? TentarExtrairNomeLoja(HtmlNode nodePreco)
    {
        try
        {
            // Procurar em elementos pai
            var parent = nodePreco.ParentNode;
            for (int i = 0; i < 3 && parent != null; i++)
            {
                // Procurar por classes comuns de nome de loja
                var nomeNode = parent.SelectSingleNode(".//*[contains(@class, 'store') or contains(@class, 'loja') or contains(@class, 'estabelecimento')]");
                if (nomeNode != null)
                {
                    var nome = nomeNode.InnerText.Trim();
                    if (!string.IsNullOrEmpty(nome) && nome.Length < 100)
                        return nome;
                }

                parent = parent.ParentNode;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

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
    
    // Cache de sessão (reutilizar por um tempo para evitar rate limiting)
    private static string? _cachedCsrfToken;
    private static string? _cachedCookies;
    private static DateTime _sessionCacheExpiry = DateTime.MinValue;
    private static readonly SemaphoreSlim _sessionLock = new(1, 1);
    private const int SessionCacheMinutes = 10;

    public PrecoDaHoraPriceLookupService(
        HttpClient httpClient,
        ILogger<PrecoDaHoraPriceLookupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configurar HttpClient com headers realistas de navegador
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        // Headers que simulam um navegador real
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", 
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        _httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"");
        _httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
        _httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
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

            // Passo 1: Obter sessão e CSRF token (com cache)
            var (csrfToken, cookies) = await ObterSessaoECsrfTokenComCacheAsync();
            
            if (string.IsNullOrEmpty(csrfToken))
            {
                _logger.LogWarning("Não foi possível obter o CSRF token");
                return new PriceLookupResult { Found = false };
            }

            // Delay adicional entre requisições para evitar rate limiting
            await Task.Delay(1000);

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
    /// Obtém a sessão e CSRF token com cache para evitar rate limiting
    /// </summary>
    private async Task<(string? CsrfToken, string? Cookies)> ObterSessaoECsrfTokenComCacheAsync()
    {
        await _sessionLock.WaitAsync();
        try
        {
            // Verificar se o cache ainda é válido
            if (!string.IsNullOrEmpty(_cachedCsrfToken) && 
                !string.IsNullOrEmpty(_cachedCookies) && 
                DateTime.UtcNow < _sessionCacheExpiry)
            {
                _logger.LogDebug("Usando sessão em cache");
                return (_cachedCsrfToken, _cachedCookies);
            }

            // Cache expirado ou inexistente, obter nova sessão
            _logger.LogInformation("Obtendo nova sessão do servidor");
            var (token, cookies) = await ObterSessaoECsrfTokenAsync();
            
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(cookies))
            {
                _cachedCsrfToken = token;
                _cachedCookies = cookies;
                _sessionCacheExpiry = DateTime.UtcNow.AddMinutes(SessionCacheMinutes);
                _logger.LogInformation("Sessão cacheada por {Minutes} minutos", SessionCacheMinutes);
            }
            
            return (token, cookies);
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    /// <summary>
    /// Obtém a sessão e o CSRF token fazendo GET na página de produtos
    /// </summary>
    private async Task<(string? CsrfToken, string? Cookies)> ObterSessaoECsrfTokenAsync()
    {
        try
        {
            // Criar requisição com headers específicos para GET
            var request = new HttpRequestMessage(HttpMethod.Get, ProductsEndpoint);
            request.Headers.Add("Referer", BaseUrl + "/");
            request.Headers.Add("Cache-Control", "max-age=0");
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao obter sessão: {StatusCode}", response.StatusCode);
                return (null, null);
            }

            // Capturar cookies de sessão
            var cookies = ExtrairCookies(response);

            // Parsear HTML para extrair CSRF token
            var html = await response.Content.ReadAsStringAsync();
            
            // Log do HTML para debug (apenas primeiros 2000 caracteres)
            _logger.LogDebug("HTML recebido (primeiros 2000 chars): {Html}", 
                html.Length > 2000 ? html.Substring(0, 2000) : html);
            
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

            // Tentar múltiplos seletores para encontrar o CSRF token
            var validateElement = doc.GetElementbyId("validate");
            if (validateElement != null)
            {
                var csrfToken = validateElement.GetAttributeValue("data-id", string.Empty);
                if (!string.IsNullOrEmpty(csrfToken))
                {
                    _logger.LogInformation("CSRF token encontrado via #validate: {Token}", csrfToken.Substring(0, Math.Min(10, csrfToken.Length)) + "...");
                    return csrfToken;
                }
            }

            // Tentar encontrar no meta tag
            var metaCsrf = doc.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']");
            if (metaCsrf != null)
            {
                var csrfToken = metaCsrf.GetAttributeValue("content", string.Empty);
                if (!string.IsNullOrEmpty(csrfToken))
                {
                    _logger.LogInformation("CSRF token encontrado via meta tag: {Token}", csrfToken.Substring(0, Math.Min(10, csrfToken.Length)) + "...");
                    return csrfToken;
                }
            }

            // Tentar encontrar em input hidden
            var inputCsrf = doc.DocumentNode.SelectSingleNode("//input[@name='csrfmiddlewaretoken']");
            if (inputCsrf != null)
            {
                var csrfToken = inputCsrf.GetAttributeValue("value", string.Empty);
                if (!string.IsNullOrEmpty(csrfToken))
                {
                    _logger.LogInformation("CSRF token encontrado via input: {Token}", csrfToken.Substring(0, Math.Min(10, csrfToken.Length)) + "...");
                    return csrfToken;
                }
            }

            // Buscar em qualquer elemento com data-id
            var elementsWithDataId = doc.DocumentNode.SelectNodes("//*[@data-id]");
            if (elementsWithDataId != null && elementsWithDataId.Any())
            {
                _logger.LogInformation("Encontrados {Count} elementos com data-id", elementsWithDataId.Count);
                foreach (var elem in elementsWithDataId)
                {
                    var dataId = elem.GetAttributeValue("data-id", string.Empty);
                    if (!string.IsNullOrEmpty(dataId) && dataId.Length > 20)
                    {
                        _logger.LogInformation("Possível CSRF em elemento {Tag} id={Id}: {Token}", 
                            elem.Name, 
                            elem.GetAttributeValue("id", "sem-id"),
                            dataId.Substring(0, Math.Min(10, dataId.Length)) + "...");
                        return dataId;
                    }
                }
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

            // Criar requisição com headers apropriados que simulam navegador
            var request = new HttpRequestMessage(HttpMethod.Post, ProductsEndpoint)
            {
                Content = content
            };

            // Headers essenciais para POST
            request.Headers.Add("X-CSRFToken", csrfToken);
            request.Headers.Add("Origin", BaseUrl);
            request.Headers.Add("Referer", BaseUrl + ProductsEndpoint);
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            
            // Sobrescrever Accept para AJAX
            request.Headers.Remove("Accept");
            request.Headers.Add("Accept", "text/html, */*; q=0.01");
            
            // Modificar Sec-Fetch para AJAX
            request.Headers.Remove("Sec-Fetch-Dest");
            request.Headers.Remove("Sec-Fetch-Mode");
            request.Headers.Remove("Sec-Fetch-Site");
            request.Headers.Add("Sec-Fetch-Dest", "empty");
            request.Headers.Add("Sec-Fetch-Mode", "cors");
            request.Headers.Add("Sec-Fetch-Site", "same-origin");
            
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

using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;
using MarketList.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MarketList.Application.Services;

/// <summary>
/// Implementação do leitor de nota fiscal.
/// Interpreta o texto bruto da nota e extrai informações estruturadas dos itens.
/// </summary>
public class LeitorNotaFiscal : ILeitorNotaFiscal
{
    private readonly ILogger<LeitorNotaFiscal> _logger;

    public LeitorNotaFiscal(ILogger<LeitorNotaFiscal> logger)
    {
        _logger = logger;
    }

    public List<ItemNotaFiscalLidoDto> Ler(string textoBruto)
    {
        var itens = new List<ItemNotaFiscalLidoDto>();

        if (string.IsNullOrWhiteSpace(textoBruto))
        {
            _logger.LogWarning("Texto da nota fiscal está vazio");
            return itens;
        }

        try
        {
            var linhas = textoBruto.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToList();

            for (int i = 0; i < linhas.Count; i++)
            {
                try
                {
                    // Verifica se a linha contém um produto com código
                    // Exemplo: BANANA TERRA (Código: AR004808)
                    var matchProduto = Regex.Match(linhas[i], @"^(.+?)\s*\(Código:\s*([A-Z0-9]+)\)\s*$", RegexOptions.IgnoreCase);
                    
                    if (matchProduto.Success && i + 1 < linhas.Count)
                    {
                        var nomeProduto = matchProduto.Groups[1].Value.Trim();
                        var codigoLoja = matchProduto.Groups[2].Value.Trim();
                        var linhaDetalhes = linhas[i + 1];

                        // Próxima linha deve conter: Qtde.:1,915   UN: KG9   Vl. Unit.: 6,99
                        var matchDetalhes = Regex.Match(linhaDetalhes, 
                            @"Qtde\.:\s*([0-9,\.]+)\s+UN:\s*([A-Z0-9]+)\s+Vl\.\s*Unit\.:\s*([0-9,\.]+)",
                            RegexOptions.IgnoreCase);

                        if (matchDetalhes.Success)
                        {
                            // Procura o valor total na próxima linha ou na mesma
                            decimal valorTotal = 0;
                            if (i + 2 < linhas.Count)
                            {
                                // Tenta na linha seguinte
                                var linhaValorTotal = linhas[i + 2];
                                if (decimal.TryParse(NormalizarNumero(linhaValorTotal.Trim()), 
                                    NumberStyles.Any, CultureInfo.InvariantCulture, out var total))
                                {
                                    valorTotal = total;
                                }
                            }

                            // Se não encontrou na linha seguinte, tenta extrair da mesma linha
                            if (valorTotal == 0)
                            {
                                var matchTotal = Regex.Match(linhaDetalhes, @"Vl\.\s*Total\s*([0-9,\.]+)", RegexOptions.IgnoreCase);
                                if (matchTotal.Success)
                                {
                                    decimal.TryParse(NormalizarNumero(matchTotal.Groups[1].Value), 
                                        NumberStyles.Any, CultureInfo.InvariantCulture, out valorTotal);
                                }
                            }

                            var quantidade = decimal.Parse(NormalizarNumero(matchDetalhes.Groups[1].Value), 
                                CultureInfo.InvariantCulture);
                            var unidadeTexto = matchDetalhes.Groups[2].Value.ToUpper();
                            var precoUnitario = decimal.Parse(NormalizarNumero(matchDetalhes.Groups[3].Value), 
                                CultureInfo.InvariantCulture);

                            var unidade = MapearUnidadeDeMedida(unidadeTexto);

                            // Se não conseguiu extrair o valor total, calcula
                            if (valorTotal == 0)
                            {
                                valorTotal = quantidade * precoUnitario;
                            }

                            var textoOriginal = $"{linhas[i]}\n{linhaDetalhes}";
                            if (i + 2 < linhas.Count && decimal.TryParse(NormalizarNumero(linhas[i + 2].Trim()), 
                                NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                            {
                                textoOriginal += $"\n{linhas[i + 2]}";
                            }

                            var item = new ItemNotaFiscalLidoDto(
                                NomeProduto: nomeProduto,
                                CodigoLoja: codigoLoja,
                                Quantidade: quantidade,
                                UnidadeDeMedida: unidade,
                                PrecoUnitario: precoUnitario,
                                PrecoTotal: valorTotal,
                                TextoOriginal: textoOriginal
                            );

                            itens.Add(item);
                            _logger.LogDebug("Item lido: {Nome} - {Qtd} {Unidade} - R$ {Preco}", 
                                nomeProduto, quantidade, unidade, precoUnitario);

                            // Pula as linhas já processadas
                            i += 2;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao processar linha {Linha}: {Texto}", i, linhas[i]);
                    // Continua processando os demais itens (tolerante a falhas)
                }
            }

            _logger.LogInformation("Nota fiscal processada: {TotalItens} itens identificados", itens.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao ler nota fiscal");
        }

        return itens;
    }

    /// <summary>
    /// Normaliza números com vírgula para ponto decimal
    /// </summary>
    private string NormalizarNumero(string texto)
    {
        return texto.Replace(",", ".");
    }

    /// <summary>
    /// Mapeia o código de unidade da nota fiscal para o enum UnidadeDeMedida
    /// </summary>
    private UnidadeDeMedida MapearUnidadeDeMedida(string codigoUnidade)
    {
        // Remove números do final (KG9 -> KG, UND9 -> UND, etc)
        var codigo = Regex.Replace(codigoUnidade, @"\d+$", "").ToUpper();

        return codigo switch
        {
            "KG" => UnidadeDeMedida.Quilograma,
            "UND" or "UN" => UnidadeDeMedida.Unidade,
            "PCT" => UnidadeDeMedida.Pacote,
            "BDJ" => UnidadeDeMedida.Bandeja,
            "MCO" => UnidadeDeMedida.Maco,
            "FRC" => UnidadeDeMedida.Frasco,
            "L" => UnidadeDeMedida.Litro,
            "G" => UnidadeDeMedida.Grama,
            "CX" => UnidadeDeMedida.Caixa,
            _ => UnidadeDeMedida.Unidade // Fallback
        };
    }
}

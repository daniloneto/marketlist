using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MarketList.Application.Interfaces;

namespace MarketList.Application.Services;

public class TextoNormalizacaoService : ITextoNormalizacaoService
{
    public string Normalizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        // 1. Converter para uppercase
        texto = texto.ToUpperInvariant();

        // 2. Remover acentos
        texto = RemoverAcentos(texto);

        // 3. Preservar números decimais (vírgula ou ponto entre dígitos)
        // Marca temporariamente para não perder
        texto = Regex.Replace(texto, @"(\d)[,.](\d)", "$1<<<DECIMAL>>>$2");

        // 4. Substituir pontuação por espaços (caracteres especiais que não são letras/números/espaços)
        texto = Regex.Replace(texto, @"[^A-Z0-9\s]", " ");

        // 5. Restaurar os decimais (usando vírgula como padrão brasileiro)
        texto = texto.Replace("<<<DECIMAL>>>", ",");

        // 6. Remover espaços duplicados e trim
        texto = Regex.Replace(texto, @"\s+", " ").Trim();

        return texto;
    }

    private string RemoverAcentos(string texto)
    {
        var normalizedString = texto.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}

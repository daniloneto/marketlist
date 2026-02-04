using System.Text.RegularExpressions;
using MarketList.Application.DTOs;
using MarketList.Application.Interfaces;

namespace MarketList.Application.Services;

public partial class AnalisadorTextoService : IAnalisadorTextoService
{
    // Mapeamento de palavras-chave para categorias
    private static readonly Dictionary<string, string[]> CategoriasKeywords = new()
    {
        ["Laticínios"] = ["leite", "queijo", "iogurte", "manteiga", "requeijão", "cream cheese", "nata", "creme de leite", "mussarela", "mozzarella", "parmesão", "coalho"],
        ["Carnes"] = ["carne", "frango", "peixe", "bife", "filé", "linguiça", "salsicha", "bacon", "presunto", "peito", "coxa", "asa", "costela", "picanha", "alcatra", "calabresa", "charque", "carne moída", "patinho", "carne do sol", "espinhaço", "porco"],
        ["Frutas"] = ["maçã", "banana", "laranja", "limão", "uva", "morango", "melancia", "melão", "abacaxi", "manga", "pera", "kiwi", "mamão", "caqui", "goiaba", "maracujá", "abacate", "coco"],
        ["Verduras e Legumes"] = ["alface", "tomate", "cebola", "alho", "batata", "cenoura", "brócolis", "couve", "espinafre", "pepino", "pimentão", "abobrinha", "berinjela", "quiabo", "abóbora", "chuchu", "jilo", "jiló", "maxixe", "aipim", "mandioca", "macaxeira", "batata doce", "coentro", "cebolinha"],
        ["Grãos e Cereais"] = ["arroz", "feijão", "lentilha", "grão de bico", "aveia", "granola", "milho", "fubá", "farinha", "amido", "maizena"],
        ["Massas"] = ["macarrão", "espaguete", "lasanha", "talharim", "penne", "fusilli", "pizza", "massa"],
        ["Pães e Padaria"] = ["pão", "bolo", "biscoito", "bolacha", "torrada", "croissant"],
        ["Bebidas"] = ["água", "suco", "refrigerante", "cerveja", "vinho", "café", "chá", "energético"],
        ["Limpeza"] = ["detergente", "sabão", "desinfetante", "água sanitária", "sanitaria", "sanitária", "esponja", "papel toalha", "limpador", "veja", "multiuso", "vanish"],
        ["Higiene"] = ["sabonete", "shampoo", "condicionador", "pasta de dente", "escova", "desodorante", "papel higiênico"],
        ["Temperos"] = ["sal", "açúcar", "pimenta", "orégano", "manjericão", "cominho", "colorau", "azeite", "vinagre", "molho de tomate", "molho tomate", "ketchup", "mostarda", "maionese", "barbecue", "shoyo", "soja"],
        ["Congelados"] = ["pizza", "lasanha congelada", "hambúrguer", "nuggets", "sorvete", "lasanha sem gluten", "lasanha sem glutem"],
        ["Frios"] = ["presunto", "mortadela", "peito de peru", "salame"],
        ["Utensílios"] = ["papel filme", "filme", "peneira", "colher", "frigideira", "panela", "utensílio", "forma", "assadeira"]
    };

    public List<ItemAnalisadoDto> AnalisarTexto(string textoOriginal)
    {
        var itens = new List<ItemAnalisadoDto>();
        
        if (string.IsNullOrWhiteSpace(textoOriginal))
            return itens;

        var linhas = textoOriginal
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l));

        foreach (var linha in linhas)
        {
            var item = AnalisarLinha(linha);
            if (item != null)
            {
                itens.Add(item);
            }
        }

        return itens;
    }

    private static ItemAnalisadoDto? AnalisarLinha(string linha)
    {
        if (string.IsNullOrWhiteSpace(linha))
            return null;

        // Normaliza a linha removendo espaços extras
        linha = NormalizarLinha(linha);

        // Regex para detectar quantidade no início: "2 leite", "3x macarrão", "2kg arroz", "1,5 kg carne"
        var regexInicio = QuantidadeInicioRegex();
        var matchInicio = regexInicio.Match(linha);

        if (matchInicio.Success)
        {
            var quantidadeStr = matchInicio.Groups["qtd"].Value.Replace(',', '.');
            var quantidade = decimal.Parse(quantidadeStr, System.Globalization.CultureInfo.InvariantCulture);
            var unidade = matchInicio.Groups["unidade"].Value;
            var nomeProduto = matchInicio.Groups["produto"].Value.Trim();

            // Remove textos extras como "(maior)" ou "sem glutem"
            nomeProduto = LimparNomeProduto(nomeProduto);

            return new ItemAnalisadoDto(
                TextoOriginal: linha,
                NomeProduto: nomeProduto,
                Quantidade: quantidade,
                Unidade: string.IsNullOrWhiteSpace(unidade) ? null : unidade.ToLower()
            );
        }

        // Regex para detectar quantidade no meio/final: "Carne do sol 1,5 ou 2kg", "leite 2", "arroz 2kg"
        var regexFinal = QuantidadeFinalRegex();
        var matchFinal = regexFinal.Match(linha);

        if (matchFinal.Success)
        {
            var nomeProduto = matchFinal.Groups["produto"].Value.Trim();
            var quantidadeStr = matchFinal.Groups["qtd"].Value.Replace(',', '.');
            var quantidade = decimal.Parse(quantidadeStr, System.Globalization.CultureInfo.InvariantCulture);
            var unidade = matchFinal.Groups["unidade"].Value;

            // Remove textos extras
            nomeProduto = LimparNomeProduto(nomeProduto);

            return new ItemAnalisadoDto(
                TextoOriginal: linha,
                NomeProduto: nomeProduto,
                Quantidade: quantidade,
                Unidade: string.IsNullOrWhiteSpace(unidade) ? null : unidade.ToLower()
            );
        }

        // Se não encontrou quantidade, assume 1 unidade
        var nomeSimples = LimparNomeProduto(linha.Trim());
        return new ItemAnalisadoDto(
            TextoOriginal: linha,
            NomeProduto: nomeSimples,
            Quantidade: 1,
            Unidade: null
        );
    }

    private static string NormalizarLinha(string linha)
    {
        // Remove espaços múltiplos
        linha = System.Text.RegularExpressions.Regex.Replace(linha, @"\s+", " ");
        return linha.Trim();
    }

    private static string LimparNomeProduto(string nome)
    {
        // Remove informações entre parênteses: "Frigideira beiju (maior)" -> "Frigideira beiju"
        nome = System.Text.RegularExpressions.Regex.Replace(nome, @"\([^)]*\)", "");
        
        // Remove palavras de descrição comuns no final
        nome = System.Text.RegularExpressions.Regex.Replace(nome, @"\b(sem gluten|sem glutem|grande|media|medio|pequeno|pequena)\b", "", RegexOptions.IgnoreCase);
        
        // Remove "ou" seguido de quantidade alternativa: "1,5 ou 2kg" -> mantém apenas o nome
        nome = System.Text.RegularExpressions.Regex.Replace(nome, @"\s+ou\s+\d+[.,]?\d*\s*(kg|g|l|ml|un|unidade|unidades|pacote|pacotes|lata|latas)?", "", RegexOptions.IgnoreCase);
        
        return nome.Trim();
    }

    public string DetectarCategoria(string nomeProduto)
    {
        var nomeLower = nomeProduto.ToLower();

        foreach (var categoria in CategoriasKeywords)
        {
            foreach (var keyword in categoria.Value)
            {
                if (nomeLower.Contains(keyword))
                {
                    return categoria.Key;
                }
            }
        }

        return "Outros";
    }

    [GeneratedRegex(@"^(?<qtd>\d+(?:[.,]\d+)?)\s*(?<unidade>kg|g|l|ml|un|unidade|unidades|pacote|pacotes|lata|latas|x)?\s+(?<produto>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex QuantidadeInicioRegex();

    [GeneratedRegex(@"^(?<produto>.+?)\s+(?<qtd>\d+(?:[.,]\d+)?)\s*(?<unidade>kg|g|l|ml|un|unidade|unidades|pacote|pacotes|lata|latas)?$", RegexOptions.IgnoreCase)]
    private static partial Regex QuantidadeFinalRegex();
}

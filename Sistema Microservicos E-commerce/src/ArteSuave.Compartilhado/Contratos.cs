namespace ArteSuave.Compartilhado;

// Equivalentes às structs Kimono, Equipamento e Venda do monólito.
// Aqui viram records imutáveis, serializados em JSON entre os serviços.

public record Kimono(string Nome, string Cor, string Tamanho, int Quantidade, double Preco)
{
    public override string ToString() =>
        $"[KIMONO] Nome: {Nome} | Cor: {Cor} | Tamanho: {Tamanho} | Qtd: {Quantidade} | Preço: R$ {Preco:F2}";
}

public record Equipamento(string Nome, string Categoria, int Quantidade, double Preco)
{
    public override string ToString() =>
        $"[EQUIPAMENTO] Nome: {Nome} | Cat: {Categoria} | Qtd: {Quantidade} | Preço: R$ {Preco:F2}";
}

public record Venda(
    Guid Id,
    DateTime Data,
    string NomeProduto,
    string Categoria,
    int QuantidadeVendida,
    double PrecoUnitario,
    double ValorTotal)
{
    public override string ToString() =>
        $"[VENDA] Produto: {NomeProduto} ({Categoria}) | Qtd: {QuantidadeVendida} | Unit.: R$ {PrecoUnitario:F2} | Total: R$ {ValorTotal:F2}";
}

/// <summary>Tipos de produto aceitos pelo Catálogo.</summary>
public static class TiposProduto
{
    public const string Kimono = "kimono";
    public const string Equipamento = "equipamento";
}

/// <summary>Estoque completo devolvido pelo Catálogo.</summary>
public record Estoque(IReadOnlyList<Kimono> Kimonos, IReadOnlyList<Equipamento> Equipamentos);

/// <summary>Resultado da busca por nome: só um dos dois campos vem preenchido.</summary>
public record ProdutoEncontrado(string Tipo, Kimono? Kimono, Equipamento? Equipamento);

/// <summary>Pedido de venda enviado pelo cliente ao serviço de Vendas.</summary>
public record PedidoVenda(string Tipo, string Nome, int Quantidade);

/// <summary>Baixa ou reposição de estoque (Vendas → Catálogo).</summary>
public record MovimentoEstoque(string Tipo, string Nome, int Quantidade);

/// <summary>O que o Catálogo devolve ao baixar o estoque de um item.</summary>
public record ItemBaixado(string Nome, string Categoria, double PrecoUnitario, int EstoqueRestante);

public record TotalCategoria(string Categoria, double Total);

public record RelatorioVendas(
    IReadOnlyList<Venda> Vendas,
    IReadOnlyList<TotalCategoria> TotaisPorCategoria,
    double TotalGeral);

public record StatusServico(string Servico, string Url, bool Online);

/// <summary>Formato padrão de erro de todas as APIs.</summary>
public record Erro(string Mensagem);

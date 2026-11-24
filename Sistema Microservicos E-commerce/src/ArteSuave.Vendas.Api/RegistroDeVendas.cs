using ArteSuave.Compartilhado;
using ArteSuave.Infra;

namespace ArteSuave.Vendas.Api;

/// <summary>
/// Regra da venda, distribuída entre dois serviços (padrão Saga com compensação):
/// 1. pede ao Catálogo a baixa do estoque (ele valida se o produto existe e se há quantidade);
/// 2. grava a venda;
/// 3. se a gravação falhar, pede ao Catálogo para repor o estoque baixado.
/// </summary>
public sealed class RegistroDeVendas
{
    private readonly RepositorioVendas _vendas;
    private readonly ClienteServico _catalogo;

    public RegistroDeVendas(RepositorioVendas vendas, ClienteServico catalogo)
    {
        _vendas = vendas;
        _catalogo = catalogo;
    }

    public async Task<Venda> RealizarAsync(PedidoVenda pedido)
    {
        if (pedido.Quantidade <= 0)
            throw ErroNegocio.DadoInvalido("A quantidade deve ser um número inteiro maior que zero.");

        var movimento = new MovimentoEstoque(pedido.Tipo, pedido.Nome, pedido.Quantidade);

        // 1. Baixa no Catálogo. Erros dele (404, 409, 400) voltam iguais para o cliente.
        Resposta<ItemBaixado> baixa = await _catalogo.PostAsync<ItemBaixado>("estoque/baixa", movimento);
        if (!baixa.Ok || baixa.Valor is null)
            throw new ErroNegocio(baixa.Status, baixa.Mensagem ?? "Não foi possível baixar o estoque.");

        ItemBaixado item = baixa.Valor;
        var venda = new Venda(
            Id: Guid.NewGuid(),
            Data: DateTime.Now,
            NomeProduto: item.Nome,
            Categoria: item.Categoria,
            QuantidadeVendida: pedido.Quantidade,
            PrecoUnitario: item.PrecoUnitario,
            ValorTotal: item.PrecoUnitario * pedido.Quantidade);

        // 2. Grava a venda.
        try
        {
            _vendas.Adicionar(venda);
            return venda;
        }
        catch (Exception erro) when (erro is IOException or UnauthorizedAccessException)
        {
            // 3. Compensação: devolve o estoque, para não sumir produto sem venda registrada.
            Resposta<object> estorno = await _catalogo.PostAsync<object>("estoque/reposicao", movimento);
            throw new ErroNegocio(500, estorno.Ok
                ? "Não foi possível gravar a venda. O estoque foi devolvido ao Catálogo."
                : "Não foi possível gravar a venda nem devolver o estoque. Confira o estoque no Catálogo.");
        }
    }
}

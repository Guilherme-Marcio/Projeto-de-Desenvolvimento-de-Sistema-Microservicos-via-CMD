using ArteSuave.Compartilhado;
using ArteSuave.Infra;

// Serviço de Relatórios (porta 5103)
// Substitui, no monólito: o cálculo de ImprimirRelatorioVendas (total por categoria e total geral).
// Não tem arquivo próprio: lê as vendas do serviço de Vendas a cada pedido.

var builder = Servico.CriarBuilder(args, "Relatórios", Enderecos.Relatorios);
builder.Services.AddSingleton(new ClienteServico("Vendas", Enderecos.Vendas));

var app = builder.Build();
app.UsarPadrao("Relatórios");

app.MapGet("/relatorios/vendas", async (ClienteServico vendas) =>
{
    Resposta<List<Venda>> resposta = await vendas.GetAsync<List<Venda>>("vendas");
    if (!resposta.Ok)
        throw new ErroNegocio(resposta.Status, resposta.Mensagem ?? "Não foi possível consultar as vendas.");

    List<Venda> lista = resposta.Valor ?? new List<Venda>();

    // Soma por categoria mantendo a ordem em que cada categoria apareceu, como no monólito.
    List<TotalCategoria> totais = lista
        .GroupBy(v => v.Categoria)
        .Select(g => new TotalCategoria(g.Key, g.Sum(v => v.ValorTotal)))
        .ToList();

    return new RelatorioVendas(lista, totais, totais.Sum(t => t.Total));
});

app.Run();

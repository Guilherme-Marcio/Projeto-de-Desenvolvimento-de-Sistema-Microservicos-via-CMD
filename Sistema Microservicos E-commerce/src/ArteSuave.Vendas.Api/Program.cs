using ArteSuave.Compartilhado;
using ArteSuave.Infra;
using ArteSuave.Vendas.Api;

// Serviço de Vendas (porta 5102)
// Substitui, no monólito: listaVendas, Salvar/CarregarVendas, RealizarVendaKimono e
// RealizarVendaEquipamento. O estoque não é mais acessado direto: é pedido ao Catálogo.

var builder = Servico.CriarBuilder(args, "Vendas", Enderecos.Vendas);
builder.Services.AddSingleton(new RepositorioVendas(Servico.PastaDados("vendas")));
builder.Services.AddSingleton(new ClienteServico("Catálogo", Enderecos.Catalogo));
builder.Services.AddSingleton<RegistroDeVendas>();

var app = builder.Build();
app.UsarPadrao("Vendas");

app.MapGet("/vendas", (RepositorioVendas vendas) => vendas.Listar());

app.MapPost("/vendas", async (PedidoVenda pedido, RegistroDeVendas registro) =>
{
    Venda venda = await registro.RealizarAsync(pedido);
    return Results.Created($"/vendas/{venda.Id}", venda);
});

app.Run();

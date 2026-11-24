using ArteSuave.Catalogo.Api;
using ArteSuave.Compartilhado;
using ArteSuave.Infra;

// Serviço de Catálogo (porta 5101)
// Substitui, no monólito: listaKimonos, listaEquipamentos, Salvar/Carregar*, cadastro,
// consulta/exclusão e o controle de estoque usado pela venda.

var builder = Servico.CriarBuilder(args, "Catálogo", Enderecos.Catalogo);
builder.Services.AddSingleton(new RepositorioCatalogo(Servico.PastaDados("catalogo")));

var app = builder.Build();
app.UsarPadrao("Catálogo");

app.MapGet("/estoque", (RepositorioCatalogo catalogo) => catalogo.ObterEstoque());

app.MapGet("/kimonos", (RepositorioCatalogo catalogo) => catalogo.ObterEstoque().Kimonos);

app.MapPost("/kimonos", (Kimono kimono, RepositorioCatalogo catalogo) =>
{
    Kimono salvo = catalogo.CadastrarKimono(kimono);
    return Results.Created($"/kimonos/{Uri.EscapeDataString(salvo.Nome)}", salvo);
});

app.MapDelete("/kimonos/{nome}", (string nome, RepositorioCatalogo catalogo) =>
{
    catalogo.ExcluirKimono(nome);
    return Results.NoContent();
});

app.MapGet("/equipamentos", (RepositorioCatalogo catalogo) => catalogo.ObterEstoque().Equipamentos);

app.MapPost("/equipamentos", (Equipamento equipamento, RepositorioCatalogo catalogo) =>
{
    Equipamento salvo = catalogo.CadastrarEquipamento(equipamento);
    return Results.Created($"/equipamentos/{Uri.EscapeDataString(salvo.Nome)}", salvo);
});

app.MapDelete("/equipamentos/{nome}", (string nome, RepositorioCatalogo catalogo) =>
{
    catalogo.ExcluirEquipamento(nome);
    return Results.NoContent();
});

// Busca por nome em kimonos e equipamentos (opção "Consultar / Excluir" do menu)
app.MapGet("/produtos/{nome}", (string nome, RepositorioCatalogo catalogo) => catalogo.Buscar(nome));

// Usadas pelo serviço de Vendas
app.MapPost("/estoque/baixa", (MovimentoEstoque movimento, RepositorioCatalogo catalogo) => catalogo.Baixar(movimento));

app.MapPost("/estoque/reposicao", (MovimentoEstoque movimento, RepositorioCatalogo catalogo) =>
{
    catalogo.Repor(movimento);
    return Results.NoContent();
});

app.Run();

using ArteSuave.Compartilhado;
using ArteSuave.Infra;

// API Gateway (porta 5100)
// O cliente só conhece este endereço. Cada chamada /api/<recurso>/... é repassada ao serviço
// dono daquele recurso, com o mesmo método, corpo e status de resposta.

var builder = Servico.CriarBuilder(args, "Gateway", Enderecos.Gateway);
var app = builder.Build();
app.UsarPadrao("Gateway");

var catalogo = new Destino("Catálogo", Enderecos.Catalogo);
var vendas = new Destino("Vendas", Enderecos.Vendas);
var relatorios = new Destino("Relatórios", Enderecos.Relatorios);

// Tabela de roteamento: primeiro segmento depois de /api → serviço
var rotas = new Dictionary<string, Destino>(StringComparer.OrdinalIgnoreCase)
{
    ["kimonos"] = catalogo,
    ["equipamentos"] = catalogo,
    ["produtos"] = catalogo,
    ["estoque"] = catalogo,
    ["vendas"] = vendas,
    ["relatorios"] = relatorios
};

var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

// Saúde agregada: consulta o /saude de cada serviço em paralelo.
app.MapGet("/api/saude", async () =>
{
    var verificacoes = new[] { catalogo, vendas, relatorios }.Select(async destino =>
    {
        using var cancelamento = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            using HttpResponseMessage resposta = await http.GetAsync(destino.Url + "/saude", cancelamento.Token);
            return new StatusServico(destino.Nome, destino.Url, resposta.IsSuccessStatusCode);
        }
        catch (Exception erro) when (erro is HttpRequestException or TaskCanceledException)
        {
            return new StatusServico(destino.Nome, destino.Url, false);
        }
    });

    return await Task.WhenAll(verificacoes);
});

// Proxy reverso para todo o resto de /api/...
app.Map("/api/{**caminho}", async (HttpContext contexto) =>
{
    string caminho = contexto.Request.Path.ToUriComponent().Substring("/api".Length); // ex.: /kimonos/Faixa%20Preta
    string recurso = caminho.TrimStart('/').Split('/')[0];

    if (!rotas.TryGetValue(recurso, out Destino? destino))
        throw ErroNegocio.NaoEncontrado($"Rota '/api{caminho}' não existe no gateway.");

    using var requisicao = new HttpRequestMessage(new HttpMethod(contexto.Request.Method), destino.Url + caminho + contexto.Request.QueryString);

    if (contexto.Request.ContentLength > 0 || contexto.Request.Headers.TransferEncoding.Count > 0)
    {
        requisicao.Content = new StreamContent(contexto.Request.Body);
        if (contexto.Request.ContentType is { } tipo)
            requisicao.Content.Headers.TryAddWithoutValidation("Content-Type", tipo);
    }

    HttpResponseMessage resposta;
    try
    {
        resposta = await http.SendAsync(requisicao, HttpCompletionOption.ResponseHeadersRead, contexto.RequestAborted);
    }
    catch (HttpRequestException)
    {
        throw new ErroNegocio(503, $"Serviço {destino.Nome} indisponível. Verifique se ele está em execução.");
    }
    catch (TaskCanceledException) when (!contexto.RequestAborted.IsCancellationRequested)
    {
        throw new ErroNegocio(504, $"Serviço {destino.Nome} não respondeu a tempo.");
    }

    using (resposta)
    {
        contexto.Response.StatusCode = (int)resposta.StatusCode;

        if (resposta.Content.Headers.ContentType is { } tipoResposta)
            contexto.Response.ContentType = tipoResposta.ToString();

        // Location do serviço ("/kimonos/x") vira o endereço público ("/api/kimonos/x")
        if (resposta.Headers.Location is { } local && !local.IsAbsoluteUri)
            contexto.Response.Headers.Location = "/api" + local.OriginalString;

        await resposta.Content.CopyToAsync(contexto.Response.Body);
    }
});

app.Run();

internal sealed record Destino(string Nome, string Url);

using System.Diagnostics;
using ArteSuave.Compartilhado;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArteSuave.Infra;

/// <summary>Configuração que todos os microserviços compartilham.</summary>
public static class Servico
{
    /// <summary>Cria o host web do serviço já ouvindo no endereço informado.</summary>
    public static WebApplicationBuilder CriarBuilder(string[] args, string nome, string url)
    {
        try { Console.Title = $"Arte Suave | {nome}"; } catch (IOException) { /* sem console */ }
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls(url);

        // Deixa só os avisos do ASP.NET; as requisições são registradas pelo nosso middleware.
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(o => o.SingleLine = true);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // JSON malformado vira exceção, para responder com a mensagem padrão em vez de 400 vazio.
        builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);

        return builder;
    }

    /// <summary>
    /// Pasta de dados do serviço. Cada serviço tem a sua e nenhum outro abre esses arquivos:
    /// quem precisa do dado pede por HTTP.
    /// </summary>
    public static string PastaDados(string servico)
    {
        string? raiz = Environment.GetEnvironmentVariable("ARTESUAVE_DADOS");
        if (string.IsNullOrWhiteSpace(raiz))
            raiz = Path.Combine(AppContext.BaseDirectory, "dados");

        return Path.Combine(raiz, servico);
    }

    /// <summary>Log de requisições, conversão de erros em JSON e rota GET /saude.</summary>
    public static WebApplication UsarPadrao(this WebApplication app, string nome)
    {
        app.Use(async (contexto, proximo) =>
        {
            var cronometro = Stopwatch.StartNew();
            try
            {
                await proximo(contexto);
            }
            catch (ErroNegocio erro)
            {
                await EscreverErro(contexto, erro.Status, erro.Message);
            }
            catch (BadHttpRequestException)
            {
                await EscreverErro(contexto, 400, "Requisição inválida. Confira o JSON enviado.");
            }
            catch (Exception erro)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERRO] {erro}");
                Console.ResetColor();
                await EscreverErro(contexto, 500, "Erro interno no serviço " + nome + ".");
            }

            Registrar(contexto, cronometro.ElapsedMilliseconds);
        });

        app.MapGet("/saude", () => Results.Ok(new { servico = nome, status = "ok" }));

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"=== Serviço {nome} no ar em {string.Join(", ", app.Urls)} ===");
            Console.ResetColor();
        });

        return app;
    }

    /// <summary>Resposta de erro no formato padrão.</summary>
    public static IResult Erro(int status, string mensagem) =>
        Results.Json(new Erro(mensagem), ClienteServico.Json, statusCode: status);

    private static async Task EscreverErro(HttpContext contexto, int status, string mensagem)
    {
        if (contexto.Response.HasStarted)
            return;

        contexto.Response.Clear();
        contexto.Response.StatusCode = status;
        await contexto.Response.WriteAsJsonAsync(new Erro(mensagem), ClienteServico.Json);
    }

    private static void Registrar(HttpContext contexto, long milissegundos)
    {
        int status = contexto.Response.StatusCode;
        Console.ForegroundColor = status >= 500 ? ConsoleColor.Red : status >= 400 ? ConsoleColor.Yellow : ConsoleColor.Gray;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {contexto.Request.Method,-6} {contexto.Request.Path}{contexto.Request.QueryString} -> {status} ({milissegundos} ms)");
        Console.ResetColor();
    }
}

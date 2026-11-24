using System.Net.Http.Json;
using System.Text.Json;

namespace ArteSuave.Compartilhado;

/// <summary>Resposta de uma chamada HTTP a outro serviço, já sem exceções.</summary>
public sealed record Resposta<T>(bool Ok, int Status, T? Valor, string? Mensagem);

/// <summary>
/// Cliente HTTP/JSON usado por um serviço para falar com outro (e pelo CMD para falar com o Gateway).
/// Converte falhas de rede em respostas 503/504 com mensagem legível, para quem chama não precisar
/// tratar exceção.
/// </summary>
public sealed class ClienteServico
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly string _nome;

    public ClienteServico(string nome, string urlBase, TimeSpan? timeout = null)
    {
        _nome = nome;
        _http = new HttpClient
        {
            BaseAddress = new Uri(urlBase.TrimEnd('/') + "/"),
            Timeout = timeout ?? TimeSpan.FromSeconds(5)
        };
    }

    public Task<Resposta<T>> GetAsync<T>(string rota) =>
        EnviarAsync<T>(new HttpRequestMessage(HttpMethod.Get, Rota(rota)));

    public Task<Resposta<T>> PostAsync<T>(string rota, object corpo) =>
        EnviarAsync<T>(new HttpRequestMessage(HttpMethod.Post, Rota(rota))
        {
            Content = JsonContent.Create(corpo, corpo.GetType(), options: Json)
        });

    public Task<Resposta<object>> DeleteAsync(string rota) =>
        EnviarAsync<object>(new HttpRequestMessage(HttpMethod.Delete, Rota(rota)));

    /// <summary>Escapa um nome para usar como segmento da URL (ex.: "Faixa Preta" → "Faixa%20Preta").</summary>
    public static string Segmento(string valor) => Uri.EscapeDataString(valor);

    private static string Rota(string rota) => rota.TrimStart('/');

    private async Task<Resposta<T>> EnviarAsync<T>(HttpRequestMessage requisicao)
    {
        try
        {
            using (requisicao)
            using (HttpResponseMessage resposta = await _http.SendAsync(requisicao))
            {
                string texto = await resposta.Content.ReadAsStringAsync();
                int status = (int)resposta.StatusCode;

                if (resposta.IsSuccessStatusCode)
                {
                    T? valor = string.IsNullOrWhiteSpace(texto) ? default : JsonSerializer.Deserialize<T>(texto, Json);
                    return new Resposta<T>(true, status, valor, null);
                }

                return new Resposta<T>(false, status, default, ExtrairMensagem(texto) ?? $"{_nome} respondeu com erro {status}.");
            }
        }
        catch (HttpRequestException)
        {
            return new Resposta<T>(false, 503, default, $"Serviço {_nome} indisponível. Verifique se ele está em execução.");
        }
        catch (TaskCanceledException)
        {
            return new Resposta<T>(false, 504, default, $"Serviço {_nome} não respondeu a tempo.");
        }
        catch (JsonException)
        {
            return new Resposta<T>(false, 502, default, $"Serviço {_nome} devolveu uma resposta inválida.");
        }
    }

    private static string? ExtrairMensagem(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Erro>(texto, Json)?.Mensagem;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

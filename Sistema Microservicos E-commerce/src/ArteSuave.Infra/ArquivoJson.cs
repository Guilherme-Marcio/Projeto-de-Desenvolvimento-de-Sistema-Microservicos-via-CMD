using System.Text.Json;
using ArteSuave.Compartilhado;

namespace ArteSuave.Infra;

/// <summary>
/// Persistência em arquivo JSON, no lugar dos .txt separados por "|" do monólito.
/// A gravação vai para um arquivo temporário e depois substitui o original, para não
/// corromper os dados se o processo cair no meio da escrita.
/// </summary>
public sealed class ArquivoJson<T> where T : class
{
    private static readonly JsonSerializerOptions Opcoes = new(ClienteServico.Json) { WriteIndented = true };

    public string Caminho { get; }

    public ArquivoJson(string caminho)
    {
        Caminho = caminho;
        Directory.CreateDirectory(Path.GetDirectoryName(Caminho)!);
    }

    public T? Ler()
    {
        if (!File.Exists(Caminho))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(Caminho), Opcoes);
        }
        catch (JsonException)
        {
            // Arquivo corrompido: guarda uma cópia para análise e começa do zero.
            File.Copy(Caminho, Caminho + ".corrompido", overwrite: true);
            return null;
        }
    }

    public void Gravar(T dados)
    {
        string temporario = Caminho + ".tmp";
        File.WriteAllText(temporario, JsonSerializer.Serialize(dados, Opcoes));
        File.Move(temporario, Caminho, overwrite: true);
    }
}

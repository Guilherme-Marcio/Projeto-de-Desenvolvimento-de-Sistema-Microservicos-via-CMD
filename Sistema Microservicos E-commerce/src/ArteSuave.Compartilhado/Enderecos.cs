namespace ArteSuave.Compartilhado;

/// <summary>
/// Endereço de cada serviço. O padrão é localhost, mas cada um pode ser trocado por
/// variável de ambiente, o que permite rodar os serviços em máquinas diferentes.
/// </summary>
public static class Enderecos
{
    public static string Gateway => Ler("ARTESUAVE_GATEWAY_URL", "http://localhost:5100");
    public static string Catalogo => Ler("ARTESUAVE_CATALOGO_URL", "http://localhost:5101");
    public static string Vendas => Ler("ARTESUAVE_VENDAS_URL", "http://localhost:5102");
    public static string Relatorios => Ler("ARTESUAVE_RELATORIOS_URL", "http://localhost:5103");

    private static string Ler(string variavel, string padrao)
    {
        string? valor = Environment.GetEnvironmentVariable(variavel);
        return (string.IsNullOrWhiteSpace(valor) ? padrao : valor).TrimEnd('/');
    }
}

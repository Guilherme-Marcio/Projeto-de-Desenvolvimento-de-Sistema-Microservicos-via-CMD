namespace ArteSuave.Infra;

/// <summary>
/// Erro de regra de negócio com o status HTTP que deve voltar para quem chamou.
/// É convertido em <c>{"mensagem": "..."}</c> pelo middleware de <see cref="Servico"/>.
/// </summary>
public sealed class ErroNegocio : Exception
{
    public int Status { get; }

    public ErroNegocio(int status, string mensagem) : base(mensagem)
    {
        Status = status;
    }

    public static ErroNegocio DadoInvalido(string mensagem) => new(400, mensagem);
    public static ErroNegocio NaoEncontrado(string mensagem) => new(404, mensagem);
    public static ErroNegocio Conflito(string mensagem) => new(409, mensagem);
}

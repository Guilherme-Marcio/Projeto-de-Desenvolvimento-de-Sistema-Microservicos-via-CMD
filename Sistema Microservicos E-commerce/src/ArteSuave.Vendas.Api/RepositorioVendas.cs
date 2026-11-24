using ArteSuave.Compartilhado;
using ArteSuave.Infra;

namespace ArteSuave.Vendas.Api;

/// <summary>Vendas registradas, gravadas em <c>vendas.json</c>. Só este serviço escreve nesse arquivo.</summary>
public sealed class RepositorioVendas
{
    private readonly object _trava = new();
    private readonly ArquivoJson<List<Venda>> _arquivo;
    private List<Venda> _vendas;

    public RepositorioVendas(string pasta)
    {
        _arquivo = new ArquivoJson<List<Venda>>(Path.Combine(pasta, "vendas.json"));
        _vendas = _arquivo.Ler() ?? new List<Venda>();
    }

    public IReadOnlyList<Venda> Listar()
    {
        lock (_trava)
            return _vendas.ToList();
    }

    public void Adicionar(Venda venda)
    {
        lock (_trava)
        {
            var nova = new List<Venda>(_vendas) { venda };
            _arquivo.Gravar(nova);
            _vendas = nova;
        }
    }
}

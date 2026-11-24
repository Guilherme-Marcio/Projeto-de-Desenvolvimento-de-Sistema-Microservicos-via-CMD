using ArteSuave.Compartilhado;
using ArteSuave.Infra;

namespace ArteSuave.Catalogo.Api;

/// <summary>
/// Kimonos e equipamentos em memória, gravados em <c>kimonos.json</c> e <c>equipamentos.json</c>.
/// O serviço atende requisições em paralelo, então todo acesso às listas passa pela trava.
/// As alterações são feitas numa cópia da lista: ela só substitui a original depois que o
/// arquivo foi gravado, para memória e disco nunca ficarem diferentes.
/// </summary>
public sealed class RepositorioCatalogo
{
    private readonly object _trava = new();
    private readonly ArquivoJson<List<Kimono>> _arquivoKimonos;
    private readonly ArquivoJson<List<Equipamento>> _arquivoEquipamentos;
    private List<Kimono> _kimonos;
    private List<Equipamento> _equipamentos;

    public RepositorioCatalogo(string pasta)
    {
        _arquivoKimonos = new ArquivoJson<List<Kimono>>(Path.Combine(pasta, "kimonos.json"));
        _arquivoEquipamentos = new ArquivoJson<List<Equipamento>>(Path.Combine(pasta, "equipamentos.json"));
        _kimonos = _arquivoKimonos.Ler() ?? new List<Kimono>();
        _equipamentos = _arquivoEquipamentos.Ler() ?? new List<Equipamento>();
    }

    public Estoque ObterEstoque()
    {
        lock (_trava)
            return new Estoque(_kimonos.ToList(), _equipamentos.ToList());
    }

    public Kimono CadastrarKimono(Kimono kimono)
    {
        kimono = kimono with
        {
            Nome = Obrigatorio(kimono.Nome, "Nome"),
            Cor = Obrigatorio(kimono.Cor, "Cor"),
            Tamanho = Obrigatorio(kimono.Tamanho, "Tamanho")
        };
        ValidarQuantidadeEPreco(kimono.Quantidade, kimono.Preco);

        lock (_trava)
        {
            GarantirNomeLivre(kimono.Nome);
            AtualizarKimonos(new List<Kimono>(_kimonos) { kimono });
        }

        return kimono;
    }

    public Equipamento CadastrarEquipamento(Equipamento equipamento)
    {
        equipamento = equipamento with
        {
            Nome = Obrigatorio(equipamento.Nome, "Nome"),
            Categoria = Obrigatorio(equipamento.Categoria, "Categoria")
        };
        ValidarQuantidadeEPreco(equipamento.Quantidade, equipamento.Preco);

        lock (_trava)
        {
            GarantirNomeLivre(equipamento.Nome);
            AtualizarEquipamentos(new List<Equipamento>(_equipamentos) { equipamento });
        }

        return equipamento;
    }

    public ProdutoEncontrado Buscar(string nome)
    {
        nome = nome.Trim();

        lock (_trava)
        {
            Kimono? kimono = _kimonos.Find(k => MesmoNome(k.Nome, nome));
            if (kimono is not null)
                return new ProdutoEncontrado(TiposProduto.Kimono, kimono, null);

            Equipamento? equipamento = _equipamentos.Find(e => MesmoNome(e.Nome, nome));
            if (equipamento is not null)
                return new ProdutoEncontrado(TiposProduto.Equipamento, null, equipamento);
        }

        throw ErroNegocio.NaoEncontrado("Nenhum produto cadastrado com esse nome.");
    }

    public void ExcluirKimono(string nome)
    {
        lock (_trava)
        {
            int indice = IndiceKimono(nome.Trim());
            var nova = new List<Kimono>(_kimonos);
            nova.RemoveAt(indice);
            AtualizarKimonos(nova);
        }
    }

    public void ExcluirEquipamento(string nome)
    {
        lock (_trava)
        {
            int indice = IndiceEquipamento(nome.Trim());
            var nova = new List<Equipamento>(_equipamentos);
            nova.RemoveAt(indice);
            AtualizarEquipamentos(nova);
        }
    }

    /// <summary>Tira do estoque a quantidade vendida. Devolve nome, categoria e preço para a venda.</summary>
    public ItemBaixado Baixar(MovimentoEstoque movimento)
    {
        (string tipo, string nome, int quantidade) = ValidarMovimento(movimento);

        lock (_trava)
        {
            if (tipo == TiposProduto.Kimono)
            {
                int i = IndiceKimono(nome);
                Kimono kimono = _kimonos[i];
                GarantirEstoque(kimono.Quantidade, quantidade);

                var nova = new List<Kimono>(_kimonos);
                nova[i] = kimono with { Quantidade = kimono.Quantidade - quantidade };
                AtualizarKimonos(nova);

                return new ItemBaixado(kimono.Nome, "Kimono", kimono.Preco, nova[i].Quantidade);
            }
            else
            {
                int i = IndiceEquipamento(nome);
                Equipamento equipamento = _equipamentos[i];
                GarantirEstoque(equipamento.Quantidade, quantidade);

                var nova = new List<Equipamento>(_equipamentos);
                nova[i] = equipamento with { Quantidade = equipamento.Quantidade - quantidade };
                AtualizarEquipamentos(nova);

                return new ItemBaixado(equipamento.Nome, equipamento.Categoria, equipamento.Preco, nova[i].Quantidade);
            }
        }
    }

    /// <summary>Devolve ao estoque uma quantidade baixada (compensação quando a venda não é gravada).</summary>
    public void Repor(MovimentoEstoque movimento)
    {
        (string tipo, string nome, int quantidade) = ValidarMovimento(movimento);

        lock (_trava)
        {
            if (tipo == TiposProduto.Kimono)
            {
                int i = IndiceKimono(nome);
                var nova = new List<Kimono>(_kimonos);
                nova[i] = nova[i] with { Quantidade = nova[i].Quantidade + quantidade };
                AtualizarKimonos(nova);
            }
            else
            {
                int i = IndiceEquipamento(nome);
                var nova = new List<Equipamento>(_equipamentos);
                nova[i] = nova[i] with { Quantidade = nova[i].Quantidade + quantidade };
                AtualizarEquipamentos(nova);
            }
        }
    }

    // ---------- auxiliares (chamados sempre dentro da trava) ----------

    private void AtualizarKimonos(List<Kimono> nova)
    {
        _arquivoKimonos.Gravar(nova);
        _kimonos = nova;
    }

    private void AtualizarEquipamentos(List<Equipamento> nova)
    {
        _arquivoEquipamentos.Gravar(nova);
        _equipamentos = nova;
    }

    private int IndiceKimono(string nome)
    {
        int i = _kimonos.FindIndex(k => MesmoNome(k.Nome, nome));
        return i >= 0 ? i : throw ErroNegocio.NaoEncontrado("Kimono não encontrado no estoque!");
    }

    private int IndiceEquipamento(string nome)
    {
        int i = _equipamentos.FindIndex(e => MesmoNome(e.Nome, nome));
        return i >= 0 ? i : throw ErroNegocio.NaoEncontrado("Equipamento não encontrado no estoque!");
    }

    private void GarantirNomeLivre(string nome)
    {
        // No monólito era possível cadastrar dois produtos com o mesmo nome, mas a venda e a
        // exclusão sempre pegavam o primeiro. Aqui o nome identifica o produto, então é único.
        if (_kimonos.Exists(k => MesmoNome(k.Nome, nome)) || _equipamentos.Exists(e => MesmoNome(e.Nome, nome)))
            throw ErroNegocio.Conflito($"Já existe um produto cadastrado com o nome '{nome}'.");
    }

    private static void GarantirEstoque(int disponivel, int pedido)
    {
        if (pedido > disponivel)
            throw ErroNegocio.Conflito("Estoque insuficiente para realizar esta venda!");
    }

    private static (string Tipo, string Nome, int Quantidade) ValidarMovimento(MovimentoEstoque movimento)
    {
        string tipo = (movimento.Tipo ?? string.Empty).Trim().ToLowerInvariant();
        if (tipo != TiposProduto.Kimono && tipo != TiposProduto.Equipamento)
            throw ErroNegocio.DadoInvalido("Tipo de produto inválido. Use 'kimono' ou 'equipamento'.");

        if (movimento.Quantidade <= 0)
            throw ErroNegocio.DadoInvalido("A quantidade deve ser um número inteiro maior que zero.");

        return (tipo, Obrigatorio(movimento.Nome, "Nome"), movimento.Quantidade);
    }

    private static void ValidarQuantidadeEPreco(int quantidade, double preco)
    {
        if (quantidade <= 0)
            throw ErroNegocio.DadoInvalido("A quantidade deve ser um número inteiro maior que zero.");

        if (preco <= 0 || double.IsNaN(preco) || double.IsInfinity(preco))
            throw ErroNegocio.DadoInvalido("O preço deve ser um número maior que zero.");
    }

    private static string Obrigatorio(string? valor, string campo) =>
        string.IsNullOrWhiteSpace(valor)
            ? throw ErroNegocio.DadoInvalido($"O campo '{campo}' é obrigatório.")
            : valor.Trim();

    private static bool MesmoNome(string a, string b) => a.Equals(b, StringComparison.OrdinalIgnoreCase);
}

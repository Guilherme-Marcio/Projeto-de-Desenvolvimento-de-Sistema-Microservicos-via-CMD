using System.Text;
using ArteSuave.Compartilhado;

namespace ArteSuave.Cliente.Cmd;

/// <summary>
/// Menu da loja no CMD. É o mesmo fluxo do monólito, mas aqui não existe lista nem arquivo:
/// cadastro, venda, consulta e relatórios são pedidos ao API Gateway, que repassa ao serviço certo.
/// </summary>
internal static class Program
{
    private static readonly ClienteServico Gateway = new("API Gateway", Enderecos.Gateway + "/api");

    private static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        try { Console.Title = "Arte Suave | Cliente CMD"; } catch (IOException) { }

        bool executando = true;
        while (executando)
        {
            LimparTela();
            ExibirMenuPrincipal();

            Console.Write(" Escolha uma opção: ");
            string? opcao = Console.ReadLine();

            switch (opcao)
            {
                case "1":
                    await MenuCadastrarProduto();
                    break;
                case "2":
                    await MenuRealizarVenda();
                    break;
                case "3":
                    await MenuConsultarExcluir();
                    break;
                case "4":
                    await ImprimirEstoque();
                    break;
                case "5":
                    await ImprimirRelatorioVendas();
                    break;
                case "6":
                    await ExibirStatusServicos();
                    break;
                case "0":
                case null: // fim da entrada (Ctrl+Z)
                    executando = false;
                    Console.WriteLine("\nSaindo do sistema... Oss!");
                    break;
                default:
                    ExibirMensagemErro("Opção inválida! Pressione ENTER para tentar novamente.");
                    break;
            }
        }
    }

    // ===================== MENU E TELA =====================

    private static void ExibirMenuPrincipal()
    {
        const int largura = 46;
        const string titulo = "ARTE SUAVE STORE | JIU-JITSU GEAR";

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("╔" + new string('═', largura) + "╗");
        Console.WriteLine("║" + CentralizarTexto(titulo, largura) + "║");
        Console.WriteLine("║" + CentralizarTexto("versão microserviços", largura) + "║");
        Console.WriteLine("╠" + new string('═', largura) + "╣");
        Console.ResetColor();

        ExibirOpcaoMenu("1", "Cadastrar Produto", ConsoleColor.Cyan, largura);
        ExibirOpcaoMenu("2", "Realizar Venda", ConsoleColor.Cyan, largura);
        ExibirOpcaoMenu("3", "Consultar / Excluir Produto", ConsoleColor.Cyan, largura);
        ExibirOpcaoMenu("4", "Imprimir Estoque", ConsoleColor.Cyan, largura);
        ExibirOpcaoMenu("5", "Imprimir Relatório de Vendas", ConsoleColor.Cyan, largura);
        ExibirOpcaoMenu("6", "Status dos Serviços", ConsoleColor.Cyan, largura);

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("╠" + new string('═', largura) + "╣");
        Console.ResetColor();

        ExibirOpcaoMenu("0", "Sair", ConsoleColor.Red, largura);

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("╚" + new string('═', largura) + "╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void ExibirOpcaoMenu(string codigo, string descricao, ConsoleColor cor, int largura)
    {
        string linha = $" [{codigo}] {descricao}";

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write("║");
        Console.ForegroundColor = cor;
        Console.Write(linha.PadRight(largura));
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("║");
        Console.ResetColor();
    }

    private static string CentralizarTexto(string texto, int largura)
    {
        if (texto.Length >= largura) return texto.Substring(0, largura);

        int espacosTotais = largura - texto.Length;
        int esquerda = espacosTotais / 2;
        int direita = espacosTotais - esquerda;
        return new string(' ', esquerda) + texto + new string(' ', direita);
    }

    // Console.Clear falha quando a saída está redirecionada (ex.: testes com pipe)
    private static void LimparTela()
    {
        if (!Console.IsOutputRedirected)
            Console.Clear();
    }

    private static void ExibirCabecalho(string titulo, ConsoleColor cor)
    {
        Console.ForegroundColor = cor;
        Console.WriteLine(titulo);
        Console.WriteLine(new string('-', titulo.Length));
        Console.ResetColor();
        Console.WriteLine();
    }

    // ===================== LEITURA E MENSAGENS =====================

    // Repete a pergunta até o usuário digitar algum texto
    private static string LerTextoObrigatorio(string mensagem)
    {
        while (true)
        {
            Console.Write(mensagem);
            string texto = (Console.ReadLine() ?? string.Empty).Trim();
            if (texto.Length > 0)
                return texto;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Campo obrigatório! Digite um valor.");
            Console.ResetColor();
        }
    }

    // Repete a pergunta até o usuário digitar um número inteiro maior que zero
    private static int LerInteiroPositivo(string mensagem)
    {
        while (true)
        {
            Console.Write(mensagem);
            if (int.TryParse(Console.ReadLine(), out int valor) && valor > 0)
                return valor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Valor inválido! Digite um número inteiro maior que zero.");
            Console.ResetColor();
        }
    }

    // Repete a pergunta até o usuário digitar um número maior que zero (aceita vírgula)
    private static double LerDoublePositivo(string mensagem)
    {
        while (true)
        {
            Console.Write(mensagem);
            if (double.TryParse(Console.ReadLine(), out double valor) && valor > 0)
                return valor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Valor inválido! Digite um número maior que zero (ex: 199,90).");
            Console.ResetColor();
        }
    }

    private static void ExibirMensagemErro(string mensagem)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n[ERRO] " + mensagem);
        Console.ResetColor();
        Console.WriteLine("Pressione ENTER para continuar...");
        Console.ReadLine();
    }

    private static void ExibirMensagemSucesso(string mensagem)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n[OK] " + mensagem);
        Console.ResetColor();
        Console.WriteLine("Pressione ENTER para continuar...");
        Console.ReadLine();
    }

    private static void AguardarEnter()
    {
        Console.WriteLine("\nPressione ENTER para voltar...");
        Console.ReadLine();
    }

    // ===================== CADASTRO (serviço de Catálogo) =====================

    private static async Task MenuCadastrarProduto()
    {
        LimparTela();
        ExibirCabecalho("--- CADASTRO DE PRODUTOS ---", ConsoleColor.Green);
        Console.WriteLine("1 - Cadastrar Kimono");
        Console.WriteLine("2 - Cadastrar Equipamento/Acessório");
        Console.WriteLine("0 - Voltar");
        Console.Write("\nOpção: ");
        string? op = Console.ReadLine();

        if (op == "1") await CadastrarKimono();
        else if (op == "2") await CadastrarEquipamento();
    }

    private static async Task CadastrarKimono()
    {
        LimparTela();
        ExibirCabecalho("--- CADASTRO DE KIMONO ---", ConsoleColor.Green);

        var kimono = new Kimono(
            Nome: LerTextoObrigatorio("Nome / Modelo (ex: Kimono Atama Mundial): "),
            Cor: LerTextoObrigatorio("Cor (ex: Azul, Branco, Preto): "),
            Tamanho: LerTextoObrigatorio("Tamanho (ex: A1, A2, A3): "),
            Quantidade: LerInteiroPositivo("Quantidade em Estoque: "),
            Preco: LerDoublePositivo("Preço Unitário (R$): "));

        var resposta = await Gateway.PostAsync<Kimono>("kimonos", kimono);

        if (resposta.Ok) ExibirMensagemSucesso("Kimono cadastrado e salvo com sucesso!");
        else ExibirMensagemErro(resposta.Mensagem!);
    }

    private static async Task CadastrarEquipamento()
    {
        LimparTela();
        ExibirCabecalho("--- CADASTRO DE EQUIPAMENTO / ACESSÓRIO ---", ConsoleColor.Green);

        var equipamento = new Equipamento(
            Nome: LerTextoObrigatorio("Nome do Produto (ex: Faixa Preta Especial): "),
            Categoria: LerTextoObrigatorio("Categoria (ex: Faixa, Rashguard, Protetor Bucal): "),
            Quantidade: LerInteiroPositivo("Quantidade em Estoque: "),
            Preco: LerDoublePositivo("Preço Unitário (R$): "));

        var resposta = await Gateway.PostAsync<Equipamento>("equipamentos", equipamento);

        if (resposta.Ok) ExibirMensagemSucesso("Equipamento cadastrado e salvo com sucesso!");
        else ExibirMensagemErro(resposta.Mensagem!);
    }

    // ===================== VENDA (serviço de Vendas) =====================

    private static async Task MenuRealizarVenda()
    {
        LimparTela();
        ExibirCabecalho("--- REALIZAR VENDA ---", ConsoleColor.Magenta);
        Console.WriteLine("1 - Vender Kimono");
        Console.WriteLine("2 - Vender Equipamento/Acessório");
        Console.WriteLine("0 - Voltar");
        Console.Write("\nOpção: ");
        string? op = Console.ReadLine();

        if (op == "1") await RealizarVenda(TiposProduto.Kimono, "--- VENDA DE KIMONO ---", "Digite o Nome/Modelo do Kimono: ", "Kimono não encontrado no estoque!");
        else if (op == "2") await RealizarVenda(TiposProduto.Equipamento, "--- VENDA DE EQUIPAMENTO ---", "Digite o Nome do Equipamento: ", "Equipamento não encontrado no estoque!");
    }

    private static async Task RealizarVenda(string tipo, string titulo, string pergunta, string naoEncontrado)
    {
        LimparTela();
        ExibirCabecalho(titulo, ConsoleColor.Magenta);

        Console.Write(pergunta);
        string nome = (Console.ReadLine() ?? string.Empty).Trim();

        // Consulta o estoque atual no Catálogo, para mostrar antes de pedir a quantidade
        var busca = await Gateway.GetAsync<ProdutoEncontrado>("produtos/" + ClienteServico.Segmento(nome));
        if (!busca.Ok && busca.Status != 404)
        {
            ExibirMensagemErro(busca.Mensagem!);
            return;
        }

        ProdutoEncontrado? produto = busca.Valor;
        if (produto is null || produto.Tipo != tipo)
        {
            ExibirMensagemErro(naoEncontrado);
            return;
        }

        string nomeCadastrado = produto.Kimono?.Nome ?? produto.Equipamento!.Nome;
        int estoque = produto.Kimono?.Quantidade ?? produto.Equipamento!.Quantidade;
        Console.WriteLine($"Estoque atual de '{nomeCadastrado}': {estoque} unidade(s).");

        int quantidade = LerInteiroPositivo("Quantidade a vender: ");
        if (quantidade > estoque)
        {
            ExibirMensagemErro("Estoque insuficiente para realizar esta venda!");
            return;
        }

        // O serviço de Vendas confirma o estoque de novo no Catálogo (outro cliente pode ter vendido antes)
        var venda = await Gateway.PostAsync<Venda>("vendas", new PedidoVenda(tipo, nomeCadastrado, quantidade));

        if (venda.Ok) ExibirMensagemSucesso($"Venda realizada! Total: R$ {venda.Valor!.ValorTotal:F2}");
        else ExibirMensagemErro(venda.Mensagem!);
    }

    // ===================== CONSULTA / EXCLUSÃO (serviço de Catálogo) =====================

    private static async Task MenuConsultarExcluir()
    {
        LimparTela();
        ExibirCabecalho("--- CONSULTAR / EXCLUIR PRODUTO ---", ConsoleColor.Yellow);
        Console.Write("Digite o nome do produto que deseja pesquisar: ");
        string busca = (Console.ReadLine() ?? string.Empty).Trim();

        if (busca.Length == 0)
        {
            ExibirMensagemErro("Nenhum produto cadastrado com esse nome.");
            return;
        }

        var resposta = await Gateway.GetAsync<ProdutoEncontrado>("produtos/" + ClienteServico.Segmento(busca));
        if (!resposta.Ok || resposta.Valor is null)
        {
            ExibirMensagemErro(resposta.Mensagem ?? "Nenhum produto cadastrado com esse nome.");
            return;
        }

        ProdutoEncontrado produto = resposta.Valor;
        bool ehKimono = produto.Tipo == TiposProduto.Kimono;
        string nome = ehKimono ? produto.Kimono!.Nome : produto.Equipamento!.Nome;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n[PRODUTO ENCONTRADO]");
        Console.WriteLine(ehKimono ? produto.Kimono!.ToString() : produto.Equipamento!.ToString());
        Console.ResetColor();

        Console.Write($"\nDeseja EXCLUIR este {(ehKimono ? "Kimono" : "Equipamento")}? (S/N): ");
        if ((Console.ReadLine() ?? string.Empty).Trim().ToUpper() != "S")
            return;

        string rota = (ehKimono ? "kimonos/" : "equipamentos/") + ClienteServico.Segmento(nome);
        var exclusao = await Gateway.DeleteAsync(rota);

        if (exclusao.Ok) ExibirMensagemSucesso($"{(ehKimono ? "Kimono" : "Equipamento")} excluído do estoque com sucesso!");
        else ExibirMensagemErro(exclusao.Mensagem!);
    }

    // ===================== ESTOQUE (serviço de Catálogo) =====================

    private static async Task ImprimirEstoque()
    {
        LimparTela();
        ExibirCabecalho("--- RELATÓRIO DE ESTOQUE COMPLETO ---", ConsoleColor.Blue);

        var resposta = await Gateway.GetAsync<Estoque>("estoque");
        if (!resposta.Ok || resposta.Valor is null)
        {
            ExibirMensagemErro(resposta.Mensagem ?? "Não foi possível consultar o estoque.");
            return;
        }

        Estoque estoque = resposta.Valor;

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("=== KIMONOS ===");
        Console.ResetColor();
        if (estoque.Kimonos.Count == 0) Console.WriteLine("Nenhum kimono cadastrado.");
        else
        {
            foreach (Kimono k in estoque.Kimonos)
                Console.WriteLine(k);
        }

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("\n=== EQUIPAMENTOS / ACESSÓRIOS ===");
        Console.ResetColor();
        if (estoque.Equipamentos.Count == 0) Console.WriteLine("Nenhum equipamento cadastrado.");
        else
        {
            foreach (Equipamento eq in estoque.Equipamentos)
                Console.WriteLine(eq);
        }

        AguardarEnter();
    }

    // ===================== RELATÓRIO (serviço de Relatórios) =====================

    private static async Task ImprimirRelatorioVendas()
    {
        LimparTela();
        ExibirCabecalho("--- RELATÓRIO DE VENDAS ---", ConsoleColor.Blue);

        var resposta = await Gateway.GetAsync<RelatorioVendas>("relatorios/vendas");
        if (!resposta.Ok || resposta.Valor is null)
        {
            ExibirMensagemErro(resposta.Mensagem ?? "Não foi possível gerar o relatório.");
            return;
        }

        RelatorioVendas relatorio = resposta.Valor;

        if (relatorio.Vendas.Count == 0)
        {
            Console.WriteLine("Nenhuma venda registrada.");
            AguardarEnter();
            return;
        }

        foreach (Venda v in relatorio.Vendas)
            Console.WriteLine(v);

        Console.WriteLine("\n--- TOTAL POR CATEGORIA ---");
        foreach (TotalCategoria total in relatorio.TotaisPorCategoria)
            Console.WriteLine($"{total.Categoria}: R$ {total.Total:F2}");

        Console.WriteLine($"\nTOTAL GERAL: R$ {relatorio.TotalGeral:F2}");

        AguardarEnter();
    }

    // ===================== STATUS (API Gateway) =====================

    private static async Task ExibirStatusServicos()
    {
        LimparTela();
        ExibirCabecalho("--- STATUS DOS SERVIÇOS ---", ConsoleColor.Blue);

        var resposta = await Gateway.GetAsync<List<StatusServico>>("saude");
        if (!resposta.Ok || resposta.Valor is null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"● API Gateway ({Enderecos.Gateway}): fora do ar");
            Console.ResetColor();
            ExibirMensagemErro(resposta.Mensagem ?? "Gateway indisponível.");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"● API Gateway ({Enderecos.Gateway}): no ar");

        foreach (StatusServico servico in resposta.Valor)
        {
            Console.ForegroundColor = servico.Online ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"● {servico.Servico} ({servico.Url}): {(servico.Online ? "no ar" : "fora do ar")}");
        }

        Console.ResetColor();
        AguardarEnter();
    }
}

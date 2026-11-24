# Arte Suave Store: do monólito aos microserviços

Sistema de uma loja de artigos de Jiu-Jitsu (kimonos, faixas, rashguards e acessórios) que roda no CMD, desenvolvido para a disciplina de Projeto de Software (4º período). O mesmo sistema foi feito em duas arquiteturas:

| Pasta | Arquitetura | Tecnologia |
|---|---|---|
| [`Sistema Monolitico E-commerce/`](Sistema%20Monolitico%20E-commerce/) | **Monolítica**: um único executável com menu, regras de negócio e arquivos `.txt` | C# / .NET Framework 4.7.2 |
| [`Sistema Microservicos E-commerce/`](Sistema%20Microservicos%20E-commerce/) | **Microserviços**: API Gateway + serviços de Catálogo, Vendas e Relatórios, cada um com seus dados, e o menu como cliente HTTP | C# / .NET 9 / ASP.NET Core Minimal APIs |

## Funcionalidades (nas duas versões)

1. Cadastrar produto (kimono ou equipamento/acessório)
2. Realizar venda, com baixa automática do estoque
3. Consultar e excluir produto pelo nome
4. Imprimir o estoque completo
5. Imprimir o relatório de vendas com total por categoria e total geral

## Como rodar

- **Monólito:** abra `Sistema Monolitico E-commerce/Sistema Monolitico E-commerce.sln` no Visual Studio e execute.
- **Microserviços:** dê dois cliques em `Sistema Microservicos E-commerce/iniciar.bat`. O [README da versão em microserviços](Sistema%20Microservicos%20E-commerce/README.md) explica a arquitetura, a API e o que mudou em relação ao monólito.

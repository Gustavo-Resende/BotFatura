# PRD: Cobertura de Testes Automatizados (BotFatura)

## Overview
O objetivo deste ciclo é elevar a confiabilidade do sistema através da criação de testes unitários para componentes críticos que atualmente possuem 0% de cobertura.

## Task 1: Testes Unitários do AuthService
Garantir que a segurança (JWT) esteja funcionando conforme o esperado.
- Criar `AuthServiceTests.cs` em `tests/BotFatura.UnitTests/Application/Common/Services`.
- Testar cenário de **Sucesso**: Credenciais corretas (lidas do IConfiguration) devem retornar um token JWT válido.
- Testar cenário de **Falha**: Credenciais incorretas (email ou senha) devem retornar `null`.
- Testar se o token gerado contém as **Claims** esperadas (Email e Role: Admin).

## Task 2: Testes de Validação de Cliente (Domain)
Reforçar as regras de negócio do domínio.
- Expandir `ClienteTests.cs`.
- Adicionar testes para garantir que CPFs/CNPJs inválidos ou nomes vazios lancem exceções de domínio (usando Ardalis.GuardClauses se aplicável).
- Testar transições de estado do Cliente (Ativo/Inativo).

## Task 3: Testes de Handlers de Clientes (Application)
Testar a orquestração de comandos.
- Criar `CadastrarClienteCommandHandlerTests.cs`.
- Usar NSubstitute para mockar o `IClienteRepository`.
- Validar se o repositório é chamado com os dados corretos e se o ID do novo cliente é retornado.

## Task 4: Testes de Handlers de Faturas (Application)
- Criar `CancelarFaturaCommandHandlerTests.cs`.
- Garantir que apenas faturas pendentes possam ser canceladas.
- Validar se o status da fatura muda para `Cancelada` após a execução.

## Task 5: Refatoração de UnitTest1 legada
- Remover o arquivo `UnitTest1.cs` que contém apenas um teste de exemplo vazio.
- Garantir que todos os 19 testes existentes + os novos estejam passando com `dotnet test`.

# Ralph Loop Completion Marker
ralph-done-test-2026

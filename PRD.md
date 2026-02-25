# PRD: Performance e Otimização de Consultas (BotFatura)

## Overview
O objetivo deste ciclo é otimizar a performance do sistema através da indexação de colunas críticas para o Worker e a consolidação de múltiplas chamadas ao banco no Dashboard em uma única consulta.

**⚠️ NOTA IMPORTANTE:** O agente **NÃO deve realizar commits**. O trabalho consiste apenas na alteração dos arquivos e execução de comandos necessários. A organização de PRs e commits será feita manualmente após a conclusão das tarefas.

## Task 1: Índices de Banco de Dados para Faturas
Melhorar a performance de varredura do `FaturaReminderWorker` e das consultas de filtro.
- Alterar `src/BotFatura.Infrastructure/Data/Configurations/FaturaConfiguration.cs`.
- Adicionar índices explícitos para as colunas `DataVencimento` e `Status`.
- Gerar uma nova migration: `dotnet ef migrations add AddIndexesToFaturas --project src/BotFatura.Infrastructure --startup-project src/BotFatura.Api`.

## Task 2: Refatoração do Repositório de Faturas
Preparar o repositório para fornecer dados consolidados.
- Adicionar o método `ObterDadosConsolidadosDashboardAsync` na interface `IFaturaRepository` em `src/BotFatura.Domain/Interfaces`.
- Implementar o método em `src/BotFatura.Infrastructure/Repositories/EntityRepositories.cs`.
- O método deve retornar um objeto contendo: Total Pendente, Total Vencendo Hoje, Total Pago, Total Atrasado e Contagens.
- Utilizar uma única query LINQ com projeção (`.Select`) para que o EF Core gere um SQL único com as agregações necessárias.

## Task 3: Otimização do Handler de Dashboard
Reduzir Roundtrips no banco de dados para o endpoint de Resumo.
- Alterar `src/BotFatura.Application/Dashboard/Queries/ObterResumoDashboard/ObterResumoDashboardQueryHandler.cs`.
- Substituir as múltiplas chamadas individuais ao `_faturaRepository` pela nova chamada única consolidada.
- Garantir que todos os valores do `DashboardResumoDto` sejam preenchidos corretamente com a nova fonte de dados.

# Ralph Loop Completion Marker
ralph-done-perf-2026

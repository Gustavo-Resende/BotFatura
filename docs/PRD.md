# BotFatura - Product Requirements Document (PRD)

## Objetivo
Criar um MVP (Minimum Viable Product) de um sistema automatizado para envio de mensagens de cobrança via WhatsApp para clientes recorrentes. O foco é solucionar a dor de uma agência que precisa cobrar de 3 a 5 clientes todos os meses. Inicialmente focado na construção de um Back-End robusto.

## Arquitetura e Tecnologias
- **Linguagem/Framework:** C# .NET 8 (Web API).
- **Arquitetura de Software:** Clean Architecture com CQRS.
  - *Domain:* Entidades ricas, Interfaces de Repositório, Especificações (Ardalis.Specification).
  - *Application:* Casos de Uso orquestrados via **MediatR**, separando Commands de Queries. Uso de DTOs para entrada/saída.
  - *Infrastructure:* EF Core (PostgreSQL) com suporte a Legacy Timestamp para compatibilidade de datas.
  - *Presentation (API):* **Minimal APIs** utilizando a biblioteca **Carter** para organização de rotas. Documentação automática via Swagger com XML Comments.
- **Banco de Dados:** PostgreSQL 16 (Docker).
- **Mensageria:** Evolution API v2 (Baileys) rodando via Docker.

## Premissas de Funcionamento
1. A API C# roda localmente acessando o Docker via porta mapeada (`localhost:5433`).
2. O sistema gerencia automaticamente a criação de instâncias na Evolution API caso não existam.

## Casos de Uso Principais (MVP)
1. **Cadastrar Cliente:** Nome completo e número de WhatsApp.
2. **Configurar Cobrança:** Definir valor e vencimento vinculados ao cliente.
3. **Configurar Mensagem Template:** Cadastrar o texto base com variáveis (`{NomeCliente}`, `{Valor}`, `{Vencimento}`).
4. **Gerenciamento de Instância:** Dashboard simplificado para conexão via QR Code com auto-refresh.
5. **Disparo Automático:** Worker em background (`FaturaReminderWorker`) para varredura diária.
6. **Disparo Manual:** Rota para envio imediato de cobrança de uma fatura específica.

## Fases de Desenvolvimento (Back-End)
- [x] **Fase 1:** Setup da documentação base, regras e Docker-Compose.
- [x] **Fase 2:** Estruturação da Solução C# (Clean Architecture).
- [x] **Fase 3:** Setup de Bibliotecas Core (MediatR, CQRS, Carter, EF Core).
- [x] **Fase 4:** Camada de Domínio (Entities e Interfaces).
- [x] **Fase 5:** Camada de Infraestrutura e Application (Handlers, Repositórios).
- [ ] **Fase 6:** Testes de Unidade (Foco em cenários USB).
- [x] **Fase 7:** Presentation (Minimal APIs endpoints).
- [x] **Fase 8:** Worker e Integração com Evolution API (Conexão e Disparo).
- [x] **Fase 9:** Ajustes de Usabilidade (QR Code Refresh e Documentação Swagger).

> **Consulta Frequente:** Este arquivo deve ser relido frequentemente pelo Assistente de IA assim que o contexto ou janelas forem resetadas, mantendo em mente as fases restantes e o foco de Clean Architecture do MVP.

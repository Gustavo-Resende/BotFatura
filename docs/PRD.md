# BotFatura - Product Requirements Document (PRD)

## Objetivo
Criar um MVP (Minimum Viable Product) de um sistema automatizado para envio de mensagens de cobrança via WhatsApp para clientes recorrentes. O foco é solucionar a dor de uma agência que precisa cobrar de 3 a 5 clientes todos os meses. Inicialmente focado na construção de um Back-End robusto.

## Arquitetura e Tecnologias
- **Linguagem/Framework:** C# .NET 10 (Web API).
- **Arquitetura de Software:** Clean Architecture com princípios de CQRS.
  - *Domain:* Entidades ricas, Interfaces de Repositório, Especificações (Ardalis.Specification) e validações com Ardalis.GuardClauses.
  - *Application:* Casos de Uso orquestrados via **MediatR**, separando clamente Comandos (Escrita) de Consultas (Leitura). Uso extensivo de DTOs para entrada/saída.
  - *Infrastructure:* Persistência (Entity Framework Core) e integrações externas.
  - *Presentation (API):* **Minimal APIs** expondo os Casos de Uso. Proibida qualquer regra de negócio nesta camada.
- **Banco de Dados:** PostgreSQL rodando em container Docker.
- **Painel BD:** pgAdmin rodando em container Docker.
- **Mensageria:** Evolution API (será dockerizado em etapa posterior).

## Premissas de Funcionamento
1. A API C# rodará localmente (na máquina de desenvolvimento do host) no primeiro momento, acessando o banco de dados no Docker via porta mapeada (ex: `localhost:5432`).
2. A solução futuramente será desacoplada (Back-end focado na API e Front-end em projeto separado que consome a API).

## Casos de Uso Principais (MVP)
1. **Cadastrar Cliente:** Nome completo e número de WhatsApp.
2. **Configurar Cobrança:** Definir datas de cobrança/vencimento e o cliente vinculado.
3. **Configurar Mensagem Template:** Cadastrar o texto base de cobrança, permitindo variáveis (ex: {{Nome}}, {{Valor}}).
4. **Disparo Automático:** Um worker em background varre o banco diariamente e coordena o envio de mensagens (usando as opções anti-ban futuramente programadas).

## Fases de Desenvolvimento (Back-End)
- [x] **Fase 1:** Setup da documentação base, regras e Docker-Compose (Banco e PgAdmin).
- [ ] **Fase 2:** Estruturação da Solução C# usando os princípios da Clean Architecture (Criação de Projetos e Referências).
- [ ] **Fase 3:** Setup de Bibliotecas Core (MediatR, CQRS, Ardalis, EF Core) e Injeção de Dependências Base nas Camadas.
- [ ] **Fase 4:** Camada de Domínio (Entities, GuardClauses e Interfaces do repositório/Specifications).
- [ ] **Fase 5:** Camada de Infraestrutura e Application (MediatR Handlers, DTOs e Repositórios EF Core).
- [ ] **Fase 6:** Testes de Unidade (Foco em cenários USB - Usuário Super Burro - violações de negócios extremas e edge cases).
- [ ] **Fase 7:** Presentation (Minimal APIs endpoints).
- [ ] **Fase 8:** Worker/Background Service diário e integração com a Evolution API.

> **Consulta Frequente:** Este arquivo deve ser relido frequentemente pelo Assistente de IA assim que o contexto ou janelas forem resetadas, mantendo em mente as fases restantes e o foco de Clean Architecture do MVP.

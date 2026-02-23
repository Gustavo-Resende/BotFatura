# BotFatura - Product Requirements Document (PRD)

## Objetivo
Criar um sistema de gestão de cobranças via WhatsApp focado em agilidade e conformidade. O sistema automatiza o envio de lembretes de faturas para clientes recorrentes, garantindo segurança no acesso e auditoria completa de todas as notificações enviadas. O foco é um MVP funcional onde a baixa de pagamentos permanece manual (conferência direta no extrato), mas o fluxo de comunicação é 100% automatizado e auditável.

## Arquitetura e Tecnologias
- **Sessão Tecnológica:** C# .NET 8 (Clean Architecture + CQRS).
- **Persistência:** PostgreSQL 16 via EF Core.
- **WhatsApp:** Evolution API v2 (Integração Baileys).
- **Segurança:** ASP.NET Core Identity + JWT Bearer.
- **Auditoria:** Registro em banco de dados para cada mensagem processada.

## Casos de Uso (Final de MVP)

### 🔐 Segurança (Acesso Restrito)
1. **Login de Admin:** Apenas usuários autenticados via JWT podem acessar as funções de Dashboard, Clientes e Faturas.

### 📢 Notificações Automatizadas
1. **Lembrete Antecipado:** Envio automático de mensagem 3 dias antes do vencimento.
2. **Cobrança no Dia:** Envio automático no dia do vencimento.
3. **Disparo Manual:** Possibilidade de reenviar uma fatura específica a qualquer momento.
4. **Proteção Anti-Ban:** Delays inteligentes para mimetizar comportamento humano.

### 📋 Auditoria e Gestão
1. **Log de Envios:** Registro histórico de cada tentativa de envio (Data, Hora, Status, Conteúdo).
2. **Gestão Manual:** Marcar faturas como "Pagas" ou "Canceladas" manualmente via interface.

## Roadmap de Lançamento (Finalização do Backend)

- [x] **Fase 1:** Core do Sistema (Clientes, Faturas, Templates).
- [x] **Fase 2:** Integração WhatsApp e Worker de Envio.
- [x] **Fase 3:** Refatoração de Rotas e Dashboard.
- [ ] **Fase 4: Segurança (JWT):** Implementar login e proteção de endpoints.
- [ ] **Fase 5: Auditoria:** Criar histórico de disparos no banco de dados.
- [ ] **Fase 6: Lembrete Inteligente:** Implementar o envio automático antecipado (3 dias).
- [ ] **Fase 7: Polimento Final:** XML documentation completa e limpeza de código.

---
> **Foco:** Simplicidade, Segurança e Prova de Envio.

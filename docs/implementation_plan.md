# Plano de Implementação: Backend MVP (Finalização)

Este plano foca na entrega rápida e segura do BotFatura, priorizando a segurança dos dados, auditoria dos envios e informações de pagamento simplificadas (PIX).

## 🎯 Próximas Implementações (Features Finais)

### 1. Sistema de Segurança (JWT)
- **O que é:** Proteção da API para que apenas o dono do sistema acesse os dados.
- **Implementação:**
  - Configurar ASP.NET Core Identity.
  - Endpoint `POST /api/auth/login`.
  - Atributo `[Authorize]` em todas as rotas sensíveis.

### 2. Histórico de Auditoria (Logs de Envio)
- **O que é:** Uma "prova" de que o sistema disparou a mensagem.
- **Implementação:**
  - Tabela `LogNotificacao` vinculada à Fatura.
  - Registro automático contendo: Data/Hora, Destinatário e Mensagem completa.
  - Exposição desses logs no `GET /api/faturas/{id}`.

### 3. Lembrete Antecipado (3 dias antes)
- **O que é:** Um aviso prévio para o cliente se preparar para o pagamento.
- **Implementação:**
  - Atualização do Worker para identificar faturas vencendo em 3 dias.
  - Flag para evitar envios duplicados do mesmo lembrete.

---

## ✅ Implementado Recentemente (Pronto para Uso)
- **Segurança (JWT):** Sistema de login implementado. Todas as rotas da API agora exigem o Token `Bearer`.
- **Dono do Sistema:** Usuário administrador padrão criado (`admin@botfatura.com.br` / `Admin@123`).
- **Dados de PIX:** Criada sessão de configurações globais para Chave PIX e Nome do Titular.
- **Hierarquia de Mensagens:** Centralizada a lógica de formatação de mensagens com suporte às novas variáveis `{NomeDono}` e `{ChavePix}`.
- **Lembrete Inteligente (3 dias):** O robô agora monitora faturas e avisa automaticamente 3 dias antes e no dia do vencimento.
- **Auditoria de Envios:** Toda mensagem (automática ou manual) agora gera um log de "Prova de Envio" no banco de dados.
- **Bug Fix:** Corrigido erro de mapeamento de banco de dados (`ClienteId1`) e estabilizada a visualização das rotas no Swagger.

---

## 🚀 Ordem de Execução (Próximos Passos)

| Passo | Task | Impacto |
| :--- | :--- | :--- |
| **01** | **Segurança (JWT)** | Protege os dados contra acessos externos. |
| **02** | **Tabela de Auditoria** | Garante a "prova de envio" solicitada pelo cliente. |
| **03** | **Ajuste no Worker (Régua)** | Ativa o envio antecipado (3 dias antes). |
| **04** | **Revisão Final** | Garante que o Swagger está 100% legível para o Front-end. |

---
**Foco Total: MVP Pronto para uso real.**

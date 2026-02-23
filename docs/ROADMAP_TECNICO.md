# Planejamento Técnico: Próximas Implementações

Este documento detalha o "como" as novas funcionalidades serão construídas, as tecnologias envolvidas e o impacto no sistema atual.

## 1. Segurança e Autenticação (JWT)
*   **Tecnologia:** `Microsoft.AspNetCore.Authentication.JwtBearer` e `Microsoft.AspNetCore.Identity`.
*   **O que muda:**
    1.  Criação da tabela de `Usuarios` (Admin).
    2.  As rotas da API agora exigirão o Header `Authorization: Bearer <token>`.
    3.  Criação de um endpoint `/api/auth/login` que retorna o token JWT.
*   **Fluxo:** O Front-end envia e-mail/senha -> Backend valida -> Gera token com tempo de expiração -> Front-end usa esse token em todas as chamadas.

## 2. Auditoria de Notificações (Logs)
*   **Tecnologia:** SQL (Nova Tabela) + Enums de Status.
*   **Estrutura:** Nova entidade `LogNotificacao` vinculada à `Fatura`.
    *   Campos: `DataEnvio`, `Tipo` (Manual/Automático), `Destinatario`, `ConteudoMensagem`, `Sucesso` (bool), `ErroMensagem` (string).
*   **Fluxo:** Dentro do `FaturaReminderWorker` e do `EnviarFaturaWhatsAppCommandHandler`, logo após chamar a API do WhatsApp, salvamos o registro no banco. Isso permite saber exatamente o que o cliente recebeu.

## 3. Mensagens Dinâmicas e Customização
*   **O que muda:** 
    1.  A entidade `Fatura` ganha a propriedade `string? MensagemCustomizada`.
    2.  O `MensagemTemplate` padrão ganha uma versão "Fallback".
*   **Fluxo de Decisão (no Worker):**
    ```csharp
    string mensagemFinal = fatura.MensagemCustomizada 
                         ?? templatePadrao.SubstituirVariaveis(fatura, cliente);
    ```
*   **Edição:** Rota `PATCH /api/faturas/{id}/mensagem` para o usuário escrever algo específico (Ex: "Oi Fulano, te dou 10% de desconto se pagar hoje!").

## 4. Régua de Cobrança (Auto-Reminders)
*   **Lógica:** Introduzir o campo `DataProximaTentativa` na Fatura.
*   **Configuração:** Permitir configurar gatilhos (Triggers).
    *   *Trigger A:* Dia do Vencimento - 3 dias.
    *   *Trigger B:* Dia do Vencimento.
    *   *Trigger C:* Dia do Vencimento + 2 dias (Se status ainda for Pendente).
*   **Worker:** O Worker filtrará faturas onde `DataProximaTentativa <= Hoje`.

## 5. Integração Mercado Pago (Financeiro)
*   **Tecnologia:** NuGet `MercadoPago.NetCore`.
*   **Processo de Geração:**
    1.  Ao criar a fatura, acionamos `PaymentClient.CreateAsync`.
    2.  Enviamos o `transaction_amount` e o e-mail do cliente.
    3.  Recebemos o `point_of_interaction.transaction_data.qr_code` (PIX Copia e Cola).
*   **Fluxo de Webhook:**
    1.  Criamos a rota `POST /api/webhooks/mercadopago`.
    2.  O Mercado Pago envia um JSON contendo o `data.id`.
    3.  Nosso sistema consulta o status desse ID via API.
    4.  Se for `approved`, mudamos a fatura local para `Paga`.

---

### Estimativa de Complexidade
- **Segurança:** Baixa/Média (Padrão .NET).
- **Logs e Customização:** Baixa.
- **Régua de Cobrança:** Média (exige cuidado com lógica de datas).
- **Mercado Pago:** Média/Alta (exige um túnel tipo Ngrok para testar webhooks locais).

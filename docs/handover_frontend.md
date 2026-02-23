# 📄 Guia de Integração: Relatório para o Front-End

Este documento descreve como o Front-End deve interagir com o Backend do BotFatura.

---

## 🔐 1. Autenticação (JWT)
O sistema exige autenticação para todos os endpoints (exceto o login).

- **Endpoint:** `POST /api/auth/login`
- **Payload:** `{ "email": "admin@botfatura.com.br", "password": "BF_P@ss_9932_*xZ" }`
- **Retorno:** Você receberá um `accessToken` e um `refreshToken`.
- **Como usar:** Envie o token em todas as requisições no Header: `Authorization: Bearer {token}`.

---

## 📱 2. Fluxo do WhatsApp (QR Code)
O Front-end deve gerenciar a conexão da instância.

- **Verificar Status:** `GET /api/whatsapp/status`
- **Conectar/Gerar QR Code:** `GET /api/whatsapp/conectar`
  - Se estiver desconectado, a API retorna um `qrcodeBase64`.
  - O Front-end deve exibir a imagem e fazer *polling* (reconsultar) a cada 10-20 segundos para ver se o status mudou para `open`.

---

## 💰 3. Gestão de Faturas e Régua
A baixa de pagamento e o cancelamento são manuais nesta fase:

- **Listar Faturas:** `GET /api/faturas` (Suporta filtro por status na query string).
- **Registrar Pagamento:** `POST /api/faturas/{id}/pagar` (Altera status para `Paga`).
- **Cancelar Fatura:** `POST /api/faturas/{id}/cancelar` (Altera status para `Cancelada`).
- **Disparar Manualmente:** `POST /api/faturas/{id}/enviar-whatsapp` (Força o envio fora da régua).

---

## 📜 4. Auditoria (Provas de Envio)
Para cada fatura, você pode exibir o histórico de mensagens enviadas.

- **Endpoint Sugerido:** `GET /api/faturas/{id}` 
  - *Nota:* O DTO de retorno da fatura contém o histórico de auditoria vinculado.

---

## ⚙️ 5. Configurações Globais (PIX)
É vital permitir que o usuário defina a chave PIX, senão as mensagens sairão com placeholders.

- **Salvar PIX:** `POST /api/configuracoes`
  - Payload: `{ "chavePix": "...", "nomeTitularPix": "..." }`
- **Consultar:** `GET /api/configuracoes`

---

## 📊 6. Dashboard
Use estes endpoints para montar os cards de resumo e tabelas de alerta.

- **Resumo:** `GET /api/dashboard/resumo` (Retorna contagem de Pendentes, Pagas e Atrasadas).
- **Clientes Críticos:** `GET /api/dashboard/atrasados` (Lista quem já passou do vencimento).

---

## 💡 Dicas de Implementação
1. **Interceptadores:** Use um interceptador HTTP (no Axios/Fetch) para adicionar o token automaticamente.
2. **Re-autenticação:** Se receber um `401 Unauthorized`, redirecione para a tela de `/login`.
3. **Variáveis de Template:** Ao editar templates de mensagem, informe ao usuário que ele pode usar as tags: `{NomeCliente}`, `{Valor}`, `{Vencimento}`, `{NomeDono}` e `{ChavePix}`.

---
**Documentação Swagger completa em:** `/swagger`

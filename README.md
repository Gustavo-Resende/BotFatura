# BotFatura 🤖💰

> **Status do Projeto: 🚀 Finalizando MVP (Backend)**

Sistema automatizado de gestão de cobranças via WhatsApp. O **BotFatura** conecta sua empresa aos seus clientes da forma mais simples possível: avisando sobre faturas, lembrando ganhos de prazo e auditando cada conversa.

## ✨ Funcionalidades Principais
- **Segurança Total:** Acesso restrito via autenticação JWT.
- **Régua de Cobrança:**
  - Lembrete Amigável (3 dias antes).
  - Cobrança Direta (No dia do vencimento).
- **Prova de Envio:** Auditoria completa e logs de cada mensagem disparada.
- **Dashboard:** Visão rápida de faturas pendentes, pagas e atrasadas.
- **Conexão Simples:** Gerenciamento de WhatsApp via QR Code direto na API.

## 🏗️ Arquitetura
Projeto em **.NET 8** seguindo **Clean Architecture**, garantindo código limpo e fácil manutenção.

## 🛠️ Tecnologias
- **PostgreSQL** para persistência de dados.
- **Evolution API** para integração com WhatsApp.
- **MediatR** para organização de comandos e consultas.

## 🚀 Como Executar
1. Configure seu `.env` com as chaves necessárias.
2. Suba o ambiente: `docker-compose up -d`.
3. Rode a aplicação: `dotnet watch --project src/BotFatura.Api`.
4. Acesse `/swagger` para gerenciar seus clientes e faturas.

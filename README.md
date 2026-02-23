# BotFatura 🤖💰

> **Gestão Inteligente de Cobranças via WhatsApp com Auditoria e Régua de Pagamento.**

O **BotFatura** é uma solução robusta de Backend construída em .NET 8, projetada para automatizar o ciclo de vida de cobranças recorrentes. Ele utiliza a **Evolution API** para transformar o WhatsApp em um canal oficial de comunicação, garantindo que o cliente receba lembretes amigáveis e que a empresa tenha provas auditáveis de cada interação.

---

## 🔥 Funcionalidades Principais

### 1. 🛡️ Segurança de Nível Empresarial
- **Autenticação JWT:** Proteção de todos os endpoints via ASP.NET Core Identity.
- **Controle de Acesso:** Apenas administradores autenticados gerenciam a base de clientes e configurações.

### 2. 📅 Régua de Cobrança Proativa (Set-and-Forget)
- **Lembrete Antecipado:** Disparo automático 3 dias antes do vencimento.
- **Aviso de Vencimento:** Notificação no dia exato do pagamento.
- **Inteligência Anti-Duplicidade:** Travas que garantem que o cliente não receba a mesma mensagem repetida.
- **Proteção Anti-Ban:** Delays inteligentes (mimetização humana) entre disparos.

### 3. 📜 Auditoria Completa (Prova de Envio)
- **Log de Notificações:** Registro histórico imutável de cada mensagem enviada (Automática ou Manual).
- **Relatórios Técnicos:** Captura de erros de envio e status da entrega para segurança jurídica.

### 4. 💸 Facilidade de Pagamento
- **Chave PIX Dinâmica:** Configuração global de dados de pagamento que são injetados automaticamente nos templates de mensagem.
- **Templates Customizáveis:** Gestão de textos base para as notificações.

---

## 🏗️ Arquitetura Técnica

A aplicação segue os princípios da **Clean Architecture**, garantindo testabilidade e desacoplamento:

- **Core:** .NET 8 (C#)
- **Engine de Automação:** Background Jobs para monitoramento de faturas.
- **Integração WhatsApp:** Evolution API v2 (Baileys Engine).
- **Banco de Dados:** PostgreSQL 16.
- **Mapeamento:** Entity Framework Core com Fluent API.
- **Comunicação:** MediatR (CQRS Pattern).

---

## 🚀 Como Iniciar

### Pré-requisitos
- .NET 8 SDK
- Docker & Docker Compose (para PostgreSQL e Evolution API)

### Configuração Rápida

1. **Clone o repositório:**
   ```bash
   git clone https://github.com/Gustavo-Resende/BotFatura.git
   ```

2. **Configure o Ambiente:**
   Crie um arquivo `appsettings.Local.json` na raiz da API ou edite as variáveis de ambiente:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=botfatura;Username=admin;Password=admin"
     },
     "EvolutionApi": {
       "BaseUrl": "https://sua-instancia.com",
       "ApiKey": "seu-token"
     }
   }
   ```

3. **Suba o Banco e Dependências:**
   ```bash
   docker-compose up -d
   ```

4. **Execute a Aplicação:**
   ```bash
   dotnet run --project src/BotFatura.Api
   ```

5. **Acesse o Swagger:**
   Abra `http://localhost:5188/swagger` para explorar a documentação interativa.

---

## 🔐 Acesso Padrão (Admin Inicial)
Após a primeira execução, o sistema cria automaticamente o administrador padrão para o primeiro acesso. 
- **Usuário:** `admin@botfatura.com.br`
- **Senha:** *(Consulte o arquivo Program.cs ou as variáveis de ambiente em produção)*

---

## 🛠️ Contribuição e Estrutura
- `src/BotFatura.Domain`: Entidades, Enums e Interfaces base.
- `src/BotFatura.Application`: Lógica de negócio, Commands, Queries e Validations.
- `src/BotFatura.Infrastructure`: Acesso a dados, Repositórios e Serviços Externos.
- `src/BotFatura.Api`: Endpoints Minimal APIs, Configurações Web e Workers.

---
> Desenvolvido com foco em escalabilidade e facilidade de uso para faturamentos modernos.

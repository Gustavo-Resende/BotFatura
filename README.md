# BotFatura 🤖💰

> **Status do Projeto: 🚧 Em Desenvolvimento Ativo (WIP)**
> 
> *Nota: Este projeto é um MVP funcional, mas possui uma margem ampla para alterações estruturais. Novas rotas, entidades e integrações estão sendo implementadas continuamente conforme a evolução das necessidades de negócio.*

O **BotFatura** é um sistema automatizado de cobrança e lembretes via WhatsApp. Ele monitora faturas pendentes e utiliza a **Evolution API** para disparar mensagens personalizadas aos clientes, garantindo que o ciclo de pagamento seja mantido de forma eficiente e modernizada.

## 🏗️ Arquitetura
O projeto foi construído seguindo os princípios da **Clean Architecture** (Arquitetura Limpa), visando desacoplamento, testabilidade e facilidade de manutenção:

- **Domain**: Entidades de negócio, Enums e interfaces base. (Independente de frameworks).
- **Application**: Lógica de aplicação, Casos de Uso (Commands/Queries) utilizando **MediatR**, validações com **FluentValidation** e mapeamento de dados.
- **Infrastructure**: Implementação de persistência com **EF Core**, integrações com APIs externas (Evolution API) e configurações de banco de dados (PostgreSQL).
- **Presentation (Web API)**: Endpoints desacoplados utilizando **Carter**, Background Workers para processamento em segundo plano e documentação com Swagger.

## 🛠️ Tecnologias Utilizadas
- **.NET 8** (C#)
- **PostgreSQL** (Banco de dados relacional)
- **Redis** (Cache e controle de sessão para o WhatsApp)
- **Docker & Docker Compose** (Orquestração de ambiente)
- **Evolution API** (Integração com WhatsApp)
- **MediatR** (Padrão CQRS)
- **FluentValidation** (Validação de entrada)
- **Ardalis.Specification** (Padrão de consulta)

## 🚀 Como Executar

### Pré-requisitos
- Docker Desktop instalado.
- SDK do .NET 8 instalado.

### Passo 1: Configurar Variáveis de Ambiente
1. Copie o arquivo de exemplo:
   ```bash
   cp .env.example .env
   ```
2. Edite o `.env` e defina suas senhas e chaves de API.

### Passo 2: Subir a Infraestrutura
Na raiz do projeto, execute:
```bash
docker-compose up -d
```
O Docker Compose lerá as variáveis automaticamente do seu arquivo `.env`.

### Passo 3: Configurar a Evolution API
1. Acesse o painel da sua Evolution API (porta 8080).
2. Crie uma instância chamada `BotFatura`.
3. Escaneie o QR Code com o WhatsApp que fará os disparos.

### Passo 4: Rodar o Backend
```bash
dotnet run --project src/BotFatura.Api/BotFatura.Api.csproj
```
A API estará disponível em `http://localhost:5188/swagger`.

## 📈 Roadmap / Próximos Passos
- [ ] Implementação de rotas para Dashboard financeiro.
- [ ] Integração com gateways de pagamento (Pix/Boleto).
- [ ] Sistema de Webhooks para confirmação de leitura.
- [ ] Expansão dos templates de mensagem dinâmicos.

## ⚖️ Licença
Este projeto é para fins de estudo e implementação de MVP. Sinta-se à vontade para contribuir ou sugerir alterações!

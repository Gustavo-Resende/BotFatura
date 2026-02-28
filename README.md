# BotFatura

> Sistema de Gestão Inteligente de Cobranças Recorrentes via WhatsApp com Auditoria Completa e Régua de Pagamento Automatizada

## Visão Geral

O **BotFatura** é uma solução robusta de backend desenvolvida em .NET 8 que automatiza o ciclo completo de gestão de cobranças recorrentes. Utilizando a **Evolution API** como ponte de comunicação com o WhatsApp, o sistema garante que clientes recebam lembretes proativos de pagamento enquanto mantém um registro auditável completo de todas as interações.

### Objetivo do Projeto

O BotFatura foi projetado para resolver problemas comuns em gestão de faturamento recorrente:

- **Automação Completa**: Elimina a necessidade de intervenção manual para envio de lembretes e cobranças
- **Rastreabilidade Jurídica**: Mantém logs imutáveis de todas as notificações enviadas, essencial para comprovação legal
- **Prevenção de Perdas**: Sistema proativo que reduz inadimplência através de lembretes antecipados
- **Escalabilidade**: Arquitetura preparada para crescer com o volume de transações

## Arquitetura e Stack Tecnológica

### Arquitetura

O projeto segue os princípios da **Clean Architecture**, garantindo separação de responsabilidades, testabilidade e desacoplamento:

```
BotFatura/
├── Domain/          # Entidades, Value Objects, Interfaces e Regras de Negócio
├── Application/     # Casos de Uso, Commands, Queries, Validações (CQRS)
├── Infrastructure/  # Implementações: Repositórios, Serviços Externos, EF Core
└── Api/            # Endpoints REST, Workers, Configurações Web
```

### Stack Tecnológica

#### Backend Core
- **.NET 8**: Framework moderno com performance otimizada e recursos avançados de async/await
- **C# 12**: Linguagem com recursos modernos como records, pattern matching e nullable reference types

#### Padrões de Design e Arquitetura
- **CQRS (Command Query Responsibility Segregation)**: Implementado via MediatR para separação clara entre operações de escrita e leitura
- **Repository Pattern**: Abstração de acesso a dados com Ardalis.Specification para queries complexas
- **Unit of Work**: Controle transacional explícito para operações que envolvem múltiplas entidades
- **Template Method Pattern**: Processamento padronizado de notificações (automáticas vs manuais)
- **Strategy Pattern**: Diferentes estratégias de notificação baseadas no tipo (lembrete, vencimento, atraso)
- **Factory Pattern**: Criação controlada de entidades de domínio

#### Persistência de Dados
- **PostgreSQL 16**: Banco de dados relacional robusto e open-source, escolhido por:
  - Suporte nativo a JSON e tipos avançados
  - Performance superior em operações complexas
  - Conformidade ACID completa
  - Escalabilidade horizontal
- **Entity Framework Core 8**: ORM com Fluent API para mapeamento e migrations
- **Ardalis.Specification**: Padrão Specification para queries reutilizáveis e testáveis

#### Integração Externa
- **Evolution API v2**: Gateway para WhatsApp usando Baileys Engine
  - Escolhida por ser open-source e permitir controle total da infraestrutura
  - Suporte completo a grupos, mídias e webhooks
  - Compatível com múltiplas instâncias

#### Comunicação e APIs
- **Carter**: Minimal APIs com organização modular para endpoints REST
- **MediatR**: Mediator pattern para desacoplamento entre handlers e controllers
- **FluentValidation**: Validação declarativa e reutilizável de comandos
- **Ardalis.Result**: Padrão Result para tratamento consistente de erros

#### Segurança
- **ASP.NET Core Identity + JWT**: Autenticação baseada em tokens
- **HTTPS**: Suporte obrigatório em produção (configurável em desenvolvimento)

#### Observabilidade
- **Health Checks**: Monitoramento de saúde do PostgreSQL e Evolution API
- **Structured Logging**: Logs estruturados para análise e correlação

#### Infraestrutura
- **Docker & Docker Compose**: Containerização para ambiente de desenvolvimento consistente
- **Polly**: Retry policies para resiliência em chamadas HTTP externas

## Funcionalidades Principais

### 1. Régua de Cobrança Automatizada

Sistema proativo que envia notificações em momentos estratégicos:

- **Lembrete Antecipado**: Configurável (padrão: 3 dias antes do vencimento)
- **Aviso de Vencimento**: Notificação no dia exato do pagamento
- **Cobrança de Atraso**: Após período configurável (padrão: 7 dias após vencimento)

**Características:**
- Processamento em batches para otimização de memória
- Prevenção de duplicidade através de flags na entidade Fatura
- Delays inteligentes entre envios para evitar bloqueios pelo WhatsApp
- Processamento assíncrono via Background Worker

### 2. Auditoria e Rastreabilidade

Registro completo e imutável de todas as operações:

- **Log de Notificações**: Histórico de cada mensagem enviada (automática ou manual)
  - Timestamp, destinatário, conteúdo, status de entrega
  - Captura de erros para análise e correção
- **Log de Comprovantes**: Rastreamento de comprovantes processados via IA
- **Relatórios Técnicos**: Dados estruturados para análise e compliance

### 3. Gestão de Contratos Recorrentes

- Criação de contratos com valores mensais fixos
- Geração automática de faturas com antecedência configurável
- Controle de vigência e encerramento
- Idempotência garantida (evita duplicação de faturas)

### 4. Processamento Inteligente de Comprovantes

- Integração com **Google Gemini AI** para extração de dados
- Matching automático de comprovantes com faturas pendentes
- Validação de valores e identificação de pagamentos
- Notificação automática para grupos de administradores

### 5. Templates Customizáveis

- Gestão de templates de mensagem por tipo de notificação
- Suporte a placeholders dinâmicos (nome, valor, vencimento, PIX)
- Preview de mensagens antes do envio
- Cache em memória para performance

## Pré-requisitos

Antes de iniciar, certifique-se de ter instalado:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (ou Docker + Docker Compose)
- [Git](https://git-scm.com/downloads)

## Configuração e Instalação

### 1. Clonar o Repositório

```bash
git clone https://github.com/Gustavo-Resende/BotFatura.git
cd BotFatura
```

### 2. Configurar Variáveis de Ambiente

Crie um arquivo `appsettings.Local.json` na pasta `src/BotFatura.Api/` (este arquivo não é versionado):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=botfatura_db;Username=admin;Password=SUA_SENHA_SEGURA"
  },
  "EvolutionApi": {
    "BaseUrl": "http://localhost:8080/",
    "InstanceName": "BotFatura",
    "ApiKey": "SUA_API_KEY_AQUI",
    "WebhookSecret": "SEU_WEBHOOK_SECRET"
  },
  "DefaultAdmin": {
    "Email": "admin@botfatura.com.br",
    "Password": "SUA_SENHA_ADMIN_SEGURA"
  },
  "JwtSettings": {
    "Secret": "SUA_CHAVE_SECRETA_JWT_MINIMO_32_CARACTERES",
    "Issuer": "BotFaturaApi",
    "Audience": "BotFaturaFrontend",
    "ExpiryInMinutes": 1440
  },
  "GeminiApi": {
    "ApiKey": "SUA_CHAVE_GEMINI_API",
    "Model": "gemini-2.5-flash",
    "MaxFileSizeMB": 6
  }
}
```

**⚠️ Importante**: 
- Use senhas fortes em produção
- A chave JWT deve ter no mínimo 32 caracteres
- Mantenha o arquivo `appsettings.Local.json` fora do controle de versão

### 3. Configurar Docker Compose

Crie um arquivo `.env` na raiz do projeto (ou configure as variáveis diretamente no `docker-compose.yml`):

```env
DB_USER=admin
DB_PASSWORD=SUA_SENHA_DB
DB_NAME=botfatura_db
DB_PORT_EXTERNAL=5433
PGADMIN_EMAIL=admin@admin.com
PGADMIN_PASSWORD=SUA_SENHA_PGADMIN
REDIS_PORT=6379
EVOLUTION_BASE_URL=http://localhost:8080
EVOLUTION_API_KEY=SUA_API_KEY_EVOLUTION
```

### 4. Iniciar Serviços com Docker

```bash
docker-compose up -d
```

Isso iniciará:
- **PostgreSQL 16**: Banco de dados na porta 5433
- **PgAdmin**: Interface web para gerenciamento do banco na porta 5050
- **Redis**: Cache distribuído na porta 6379
- **Evolution API**: Serviço de WhatsApp na porta 8080

### 5. Executar Migrações e Seed

As migrações são executadas automaticamente na inicialização da aplicação. O sistema criará:
- Estrutura de tabelas
- Templates padrão de mensagem
- Usuário administrador inicial

### 6. Executar a Aplicação

```bash
dotnet run --project src/BotFatura.Api
```

A API estará disponível em:
- **HTTP**: `http://localhost:5188`
- **HTTPS**: `https://localhost:7188`
- **Swagger UI**: `http://localhost:5188/swagger`
- **Health Checks**: 
  - `/health` - Status geral
  - `/health/ready` - Pronto para receber tráfego
  - `/health/live` - Aplicação viva

## Primeiro Acesso

Após a primeira execução, o sistema cria automaticamente um usuário administrador:

- **Email**: `admin@botfatura.com.br` (ou o valor configurado em `DefaultAdmin:Email`)
- **Senha**: O valor configurado em `DefaultAdmin:Password`

**⚠️ Segurança**: Altere a senha padrão imediatamente após o primeiro acesso em produção.

## Conectando o WhatsApp

1. Acesse o endpoint `/api/whatsapp/qrcode` via Swagger ou Postman
2. Escaneie o QR Code retornado com seu WhatsApp
3. Verifique o status em `/api/whatsapp/status`
4. Quando o status retornar `"open"`, o sistema está pronto para enviar mensagens

## Estrutura do Projeto

```
BotFatura/
├── src/
│   ├── BotFatura.Domain/          # Camada de Domínio
│   │   ├── Entities/              # Entidades de negócio
│   │   ├── Enums/                 # Enumerações
│   │   ├── Interfaces/            # Contratos de repositórios e serviços
│   │   └── Factories/             # Factories para criação de entidades
│   │
│   ├── BotFatura.Application/     # Camada de Aplicação
│   │   ├── Clientes/              # Casos de uso de clientes
│   │   ├── Faturas/               # Casos de uso de faturas
│   │   ├── Contratos/             # Casos de uso de contratos
│   │   ├── Templates/            # Casos de uso de templates
│   │   ├── Configuracoes/        # Casos de uso de configurações
│   │   ├── Dashboard/            # Queries de dashboard
│   │   ├── Comprovantes/         # Processamento de comprovantes
│   │   └── Common/               # Serviços e interfaces compartilhadas
│   │
│   ├── BotFatura.Infrastructure/  # Camada de Infraestrutura
│   │   ├── Data/                 # DbContext, Migrations, UnitOfWork
│   │   ├── Repositories/         # Implementações de repositórios
│   │   └── Services/              # Clientes HTTP, serviços externos
│   │
│   └── BotFatura.Api/            # Camada de Apresentação
│       ├── Endpoints/            # Endpoints REST (Carter)
│       ├── Workers/              # Background Services
│       ├── Services/             # Implementações de serviços da API
│       └── HealthChecks/         # Health checks customizados
│
├── tests/
│   └── BotFatura.UnitTests/     # Testes unitários
│
├── docs/                         # Documentação adicional
├── docker-compose.yml           # Orquestração Docker
└── README.md                    # Este arquivo
```

## Endpoints Principais

### Autenticação
- `POST /api/auth/login` - Autenticação e obtenção de token JWT

### Clientes
- `GET /api/clientes` - Listar clientes
- `POST /api/clientes` - Cadastrar cliente
- `PUT /api/clientes/{id}` - Atualizar cliente
- `DELETE /api/clientes/{id}` - Excluir cliente

### Faturas
- `GET /api/faturas` - Listar faturas (filtro opcional por status)
- `POST /api/faturas` - Criar nova fatura
- `PUT /api/faturas/{id}` - Atualizar fatura
- `POST /api/faturas/{id}/pagar` - Registrar pagamento
- `POST /api/faturas/{id}/cancelar` - Cancelar fatura
- `POST /api/faturas/{id}/enviar` - Enviar fatura manualmente via WhatsApp

### Contratos
- `GET /api/contratos` - Listar contratos
- `POST /api/contratos` - Criar contrato recorrente
- `POST /api/contratos/{id}/encerrar` - Encerrar contrato

### Templates
- `GET /api/templates` - Listar templates
- `PUT /api/templates/{id}` - Atualizar template
- `POST /api/templates/preview` - Preview de mensagem

### Configurações
- `GET /api/configuracao` - Obter configuração global
- `PUT /api/configuracao` - Atualizar configuração (PIX, dias de antecedência, etc.)

### WhatsApp
- `GET /api/whatsapp/status` - Status da conexão
- `GET /api/whatsapp/qrcode` - Gerar QR Code para conexão
- `POST /api/whatsapp/criar-instancia` - Criar instância
- `GET /api/whatsapp/grupos` - Listar grupos

### Dashboard
- `GET /api/dashboard/resumo` - Resumo consolidado
- `GET /api/dashboard/clientes-atrasados` - Clientes com faturas atrasadas
- `GET /api/dashboard/historico-pagamentos` - Histórico de pagamentos

## Desenvolvimento

### Executando Testes

```bash
dotnet test
```

### Criando Migrações

```bash
dotnet ef migrations add NomeDaMigracao --project src/BotFatura.Infrastructure --startup-project src/BotFatura.Api
```

### Aplicando Migrações

```bash
dotnet ef database update --project src/BotFatura.Infrastructure --startup-project src/BotFatura.Api
```

## Monitoramento e Saúde

O sistema expõe endpoints de health check:

- **`/health`**: Status geral do sistema
- **`/health/ready`**: Verifica se o sistema está pronto (PostgreSQL conectado)
- **`/health/live`**: Verifica se a aplicação está viva

Use esses endpoints para integração com orquestradores como Kubernetes ou Docker Swarm.

## Troubleshooting

### Problemas Comuns

#### WhatsApp não conecta
1. Verifique se a Evolution API está rodando: `docker ps`
2. Verifique os logs: `docker logs botfatura-evolution`
3. Confirme que a API Key está correta no `appsettings.Local.json`

#### Erro de conexão com PostgreSQL
1. Verifique se o container está rodando: `docker ps`
2. Confirme a porta e credenciais no `appsettings.Local.json`
3. Verifique os logs: `docker logs botfatura-postgres`

#### Migrações não aplicam
1. Verifique a connection string
2. Confirme permissões do usuário do banco
3. Execute manualmente: `dotnet ef database update`

## Contribuindo

Contribuições são bem-vindas! Por favor:

1. Faça um fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## Licença

Este projeto está sob a licença MIT. Veja o arquivo `LICENSE` para mais detalhes.

## Suporte

Para questões, problemas ou sugestões, abra uma issue no repositório GitHub.

---

**Desenvolvido com foco em escalabilidade, manutenibilidade e facilidade de uso para sistemas modernos de faturamento.**

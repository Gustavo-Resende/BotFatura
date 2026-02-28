# Variáveis
PROJECT_PATH=src/BotFatura.Api/BotFatura.Api.csproj
TEST_PATH=tests/BotFatura.UnitTests/BotFatura.UnitTests.csproj
DOCKER_COMPOSE=docker-compose.yml

.PHONY: build run watch clean restore test MakeBuild MakeRun docker-up docker-down docker-restart docker-logs docker-ps docker-clean

# Comando para compilar o projeto
build:
	dotnet build $(PROJECT_PATH)

MakeBuild: build

# Comando para rodar o projeto
run:
	dotnet run --project $(PROJECT_PATH)

MakeRun: run

# Comando para rodar os testes
test:
	dotnet test $(TEST_PATH)

# Comando para rodar com watch (hot reload)
watch:
	dotnet watch --project $(PROJECT_PATH)

# Limpar artefatos de build
clean:
	dotnet clean

# Restaurar dependências
restore:
	dotnet restore

# ==========================================
# Comandos Docker
# ==========================================

# Subir todos os containers do docker-compose
docker-up:
	docker-compose -f $(DOCKER_COMPOSE) up -d

# Subir containers e mostrar logs em tempo real
docker-up-logs:
	docker-compose -f $(DOCKER_COMPOSE) up

# Derrubar todos os containers
docker-down:
	docker-compose -f $(DOCKER_COMPOSE) down

# Reiniciar todos os containers
docker-restart:
	docker-compose -f $(DOCKER_COMPOSE) restart

# Reiniciar apenas a API do BotFatura
docker-restart-api:
	docker-compose -f $(DOCKER_COMPOSE) restart botfatura-api

# Ver logs de todos os containers
docker-logs:
	docker-compose -f $(DOCKER_COMPOSE) logs -f

# Ver logs apenas da API
docker-logs-api:
	docker-compose -f $(DOCKER_COMPOSE) logs -f botfatura-api

# Ver logs apenas da Evolution API
docker-logs-evolution:
	docker-compose -f $(DOCKER_COMPOSE) logs -f evolution-api

# Ver logs apenas do PostgreSQL
docker-logs-db:
	docker-compose -f $(DOCKER_COMPOSE) logs -f postgres

# Listar containers em execução
docker-ps:
	docker-compose -f $(DOCKER_COMPOSE) ps

# Derrubar containers e remover volumes (⚠️ LIMPA O BANCO DE DADOS)
docker-clean:
	docker-compose -f $(DOCKER_COMPOSE) down -v

# Reconstruir e subir containers (útil após mudanças no Dockerfile)
docker-rebuild:
	docker-compose -f $(DOCKER_COMPOSE) up -d --build

# Entrar no shell do container da API
docker-shell-api:
	docker exec -it botfatura-api /bin/bash

# Entrar no shell do PostgreSQL
docker-shell-db:
	docker exec -it botfatura-postgres psql -U ${DB_USER} -d ${DB_NAME}

# Backup do banco de dados
docker-backup-db:
	docker exec botfatura-postgres pg_dump -U ${DB_USER} ${DB_NAME} > backup_$(shell date +%Y%m%d_%H%M%S).sql

# Ajuda - mostra todos os comandos disponíveis
help:
	@echo "========================================="
	@echo "BotFatura - Makefile"
	@echo "========================================="
	@echo ""
	@echo "Comandos de Desenvolvimento:"
	@echo "  make build              - Compila o projeto"
	@echo "  make run                - Executa o projeto localmente"
	@echo "  make watch              - Executa com hot reload"
	@echo "  make test               - Executa os testes"
	@echo "  make clean              - Limpa artefatos de build"
	@echo "  make restore            - Restaura dependências"
	@echo ""
	@echo "Comandos Docker:"
	@echo "  make docker-up          - Sobe todos os containers"
	@echo "  make docker-up-logs     - Sobe containers com logs"
	@echo "  make docker-down        - Derruba todos os containers"
	@echo "  make docker-restart     - Reinicia todos os containers"
	@echo "  make docker-restart-api - Reinicia apenas a API"
	@echo "  make docker-logs        - Mostra logs de todos os containers"
	@echo "  make docker-logs-api    - Mostra logs da API"
	@echo "  make docker-logs-evolution - Mostra logs da Evolution API"
	@echo "  make docker-logs-db     - Mostra logs do PostgreSQL"
	@echo "  make docker-ps          - Lista containers em execução"
	@echo "  make docker-clean       - Remove containers e volumes (⚠️ limpa BD)"
	@echo "  make docker-rebuild     - Reconstroi e sobe containers"
	@echo "  make docker-shell-api   - Acessa shell do container da API"
	@echo "  make docker-shell-db    - Acessa shell do PostgreSQL"
	@echo "  make docker-backup-db   - Faz backup do banco de dados"
	@echo ""
	@echo "Para mais informações, consulte o README.md"
	@echo "========================================="


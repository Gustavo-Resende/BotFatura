# Variáveis
PROJECT_PATH=src/BotFatura.Api/BotFatura.Api.csproj
TEST_PATH=tests/BotFatura.UnitTests/BotFatura.UnitTests.csproj

.PHONY: build run watch clean restore test MakeBuild MakeRun

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

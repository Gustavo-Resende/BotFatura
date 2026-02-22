# Regras de Desenvolvimento (Rules)

Este arquivo define as diretrizes obrigatórias para o desenvolvimento do **BotFatura**. O Assistente de IA deve consultar e obedecer a estas regras em todas as interações.

## 1. Planejamento Prévio (Obrigatoriedade)
- **Regra:** *Toda* nova task ou ação técnica significativa deve ser precedida de um **Plano de Implementação** detalhado. 
- **Ação:** O Assistente deve apresentar o plano e aguardar a aprovação expressa do usuário antes de iniciar a codificação ou alterar a infraestrutura.

## 2. Foco no MVP (Simplicidade e Objetividade)
- **Regra:** Mantenha a simplicidade. O sistema deve fazer o suficiente para resolver o problema atual (MVP para 3 a 5 clientes mensais do amigo).
- **Ação:** Evite códigos excessivamente grandes ou over-engineering. Faça apenas o que precisa ser feito, de maneira enxuta.

## 3. Qualidade do Código e Idiomatismo
- **Regra:** O código deve possuir excelente legibilidade, sendo altamente idiomático para o ecossistema C# / .NET.
- **Ação:** Siga as convenções de nomenclatura da Microsoft (PascalCase para métodos/classes/propriedades, camelCase para parâmetros e variáveis locais, interfaces prefixadas com `I`).

## 4. Nomenclatura de Variáveis
- **Regra:** As variáveis devem possuir nomes completos, expressivos e bem cuidados.
- **Ação:** É **estritamente proibido** o uso de variáveis com nomes abreviados, siglas indecifráveis ou genéricos (ex: `cli`, `vlr`, `data1`). Use nomes como `clienteId`, `valorFatura`, `dataVencimento`.

## 5. Arquitetura e Engenharia de Software
- **Regra:** O projeto deve seguir fundamentalmente a **Clean Architecture + CQRS + DDD (Domain-Driven Design)**. As responsabilidades devem ser divididas em:
  - *Domain:* Entidades ricas, Core logic e Interfaces do núcleo do projeto (sem dependências). **Regra específica do DDD:** Trate o Domínio como o coração do software. O estado interno das entidades deve ser protegido (Encapsulamento agressivo). Alterações de estado só devem ocorrer através de métodos de negócios expressivos (ex: `fatura.MarcarComoPaga()`), jamais mediante *"setters"* expostos. Usar `Ardalis.GuardClauses` e `Ardalis.Specification` para validações e consultas enxutas.
  - *Application:* Fluxos de Caso de Uso e regras orquestradas. **Regra específica:** Utilizar `MediatR` separando estritamente os manipuladores de escrita (Commands) dos de leitura (Queries). O tráfego de dados ocorrerá exclusivamente via `DTOs`.
  - *Infrastructure:* Persistência de dados (Banco via EF Core), integrações e repassadores (ex: Evolution API).
  - *Presentation (API):* **Obrigatório** o uso de `Minimal APIs` para gerir os endpoints de forma leve e enxuta. **NUNCA** adicione lógica de negócio nesta camada.
- **Regra:** Implemente **Design Patterns** de maneira cirúrgica onde forem necessários (ex: Specification Pattern, Mediator Pattern, Repository Pattern).

## 6. Comentários e Documentação de Código
- *Preciso que faça a documentação no Swagger e que seja bem escrita e bem documentada*
- *[Apenas comente partes complexas da lógica de negócio. Use o português para alinhamento da documentação.]*

## 7. Testes de Unidade (Mentalidade USB)
- **Regra:** Todos os componentes cruciais (Domain, Application) possuirão *Testes de Unidade*. **O foco é testar falhas de negócio profundas e edge-cases**, abraçando a filosofia "USB - Usuário Super Burro".
- **Ação:** Evite testes óbvios e inúteis (ex: testar se um setter funciona). Crie testes considerando que usuários enviarão dados maliciosos, inconsistentes ou agirão da forma menos intuitiva possível.

## 8. Banco de Dados e Containers
- **Regra:** A API .NET inicial **não** será dockerizada neste momento. Ela rodará local e acessará os serviços via porta exposta.
- **Regra:** O Banco de Dados (PostgreSQL) e o visualizador (pgAdmin) — além da futura integração Evolution API — **devem obrigatoriamente** rodar em containers pelo `docker-compose`.

> **Nota ao Usuário:** Sinta-se completamente livre para adicionar novas regras e formatar este arquivo conforme encontrar necessário ao longo do projeto.


### 10. Uso do Terminal (PowerShell vs Bash)
- **Windows System**: O ecossistema está rodando no Windows OS com o terminal padrão PowerShell.
- **Comandos Linux**: Em caso de erros com scripts de terminal Linux (ex: `cat << EOF`), abra um novo terminal em `pwsh` ou utilize a ferramenta de escrita de arquivos nativa em vez de tentar contornar deletando arquivos precipitadamente. Nunca dependa do PowerShell clássico para operadores de redirecionamento multinha do Unix.

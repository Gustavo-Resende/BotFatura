# PRD: BotFatura Web (Frontend Premium)

## Overview
Este projeto consiste no desenvolvimento do front-end do sistema BotFatura. O objetivo é criar uma interface administrativa de alto nível (Premium) para gestão de clientes, faturas e automação de cobranças via WhatsApp, seguindo a estética **Oripi/Monetra Style**.

**⚠️ Regra de Operação:** O agente **NÃO deve realizar commits**. O trabalho consiste apenas na alteração de arquivos e execução de comandos.

---

## Feature 1: Foundation & Design System
Estabelecer a base técnica e a identidade visual "Neon" do projeto.

### Task 1.1: Instalação de Dependências
- **Nota:** O comando `npx create-vite` já foi executado manualmente.
- Instalar dependências necessárias via npm: `axios`, `lucide-react`, `react-router-dom` e `@tanstack/react-query`.
- Instalar Tailwind CSS e inicializar as configurações.
- Configurar `src/api/client.ts` com base no `api-spec.json` para chamadas tipadas ao backend.

### Task 1.2: Design System (Shadcn + Tailwind)
- Instalar e configurar componentes básicos do Shadcn/UI.
- Configurar `tailwind.config.js` com a cor Primary `#ADFF41` e Background `#F4F7F2`.
- Definir o arredondamento de bordas global (`radius`) para `24px`.
- Configurar a tipografia principal como 'Inter' ou 'Geist Sans' no CSS global.

---

## Feature 2: Autenticação & Segurança
Implementar o fluxo de acesso seguro e proteção de rotas.

### Task 2.1: Login Side (UI & Logic)
- Criar página de Login fiel à `inspiracao1.jpg`.
- Integrar com o endpoint `/api/auth/login`.
- Gerenciar o estado do JWT e persistência no `localStorage`.

### Task 2.2: Roteamento Protegido
- Implementar o componente de Layout principal (Sidebar + Content).
- Criar guardas de rota que redirecionam para `/login` se o token estiver ausente ou expirado.

---

## Feature 3: Interface Mestra (AppShell)
A estrutura visual flutuante que define a experiência premium.

### Task 3.1: Floating Sidebar (Cápsula)
- Desenvolver a sidebar lateral esquerda conforme `inspiracao5.jpg`.
- Deve possuir fundo branco, bordas arredondadas e ícones minimalistas.

### Task 3.2: Header & Navigation
- Implementar cabeçalho com Breadcrumbs e área de Perfil.
- Garantir que a estrutura seja 100% responsiva (Mobile-first).

---

## Feature 4: Dashboard Centralizado
Visualização rápida dos dados de faturamento extraídos do backend.

### Task 4.1: Resumo em Cards
- Criar os 4 cards de topo (Pendente, Hoje, Pago, Atrasado).
- Usar ícones com a cor secundária `#4193FF`.

### Task 4.2: Gráfico de Cash Flow
- Implementar gráfico de barras usando `Recharts`.
- Aplicar o estilo gradiente Neon conforme referências visuais.

---

## Feature 5: CRM & Gestão de Pagamentos
Módulos de gerenciamento operacional.

### Task 5.1: Listagem e Cadastro de Clientes
- Implementar tabela paginada de clientes.
- Criar formulário de cadastro com validação via Zod (regex de WhatsApp).

### Task 5.2: Gestão de Faturas
- Listar faturas com badges de status coloridas.
- Implementar botões de ação para "Cancelar" e "Registrar Pagamento".

---

## Feature 6: Controle de Automação (WhatsApp)
Interface para monitoramento térmico da Evolution API.

### Task 6.1: WhatsApp Connection Panel
- Exibir status da conexão em tempo real.
- Criar renderizador de QR Code que converte o Base64 da API em imagem visual.

---

# Ralph Loop Completion Marker
ralph-done-web-2026

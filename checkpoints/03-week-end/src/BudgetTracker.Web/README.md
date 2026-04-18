# Budget Tracker Web Application

A React + TypeScript + Vite frontend for personal finance tracking.

## Features

- User authentication (registration, login, logout)
- Transaction management with CSV import
- Auto-detection of bank formats
- AI-powered transaction categorization
- Semantic search over transactions

## Tech Stack

- React 18 with TypeScript (strict mode)
- React Router v7
- Tailwind CSS
- Axios for API communication
- Vite for build tooling

## Prerequisites

- Node.js 18+
- npm 9+
- Backend API running (see below)

## Development Setup

1. **Start the backend API first:**
   ```bash
   # From the repository root
   cd docker && docker compose up -d  # Start PostgreSQL
   cd ../src/BudgetTracker.Api && dotnet run  # Start API on port 5295
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Start the development server:**
   ```bash
   npm run dev
   ```

   The app will be available at http://localhost:5173

## Available Scripts

```bash
npm run dev      # Start development server
npm run build    # Build for production
npm run lint     # Run ESLint
npm run preview  # Preview production build
```

## Environment Configuration

The application uses `VITE_API_BASE_URL` to configure the API endpoint.

### Default Behavior

| Environment | Default Value |
|-------------|---------------|
| Development | `http://localhost:5295/api` |
| Production  | `/api` (relative to current domain) |

### Setting the Environment Variable

**Linux/macOS:**
```bash
VITE_API_BASE_URL=http://localhost:5295/api npm run dev
```

**Windows PowerShell:**
```powershell
$env:VITE_API_BASE_URL="http://localhost:5295/api"; npm run dev
```

**Windows Command Prompt:**
```cmd
set VITE_API_BASE_URL=http://localhost:5295/api && npm run dev
```

### Environment Files

Create these files in the project root (git-ignored):

```bash
# .env.local - Local development overrides
VITE_API_BASE_URL=http://localhost:5295/api

# .env.production - Production defaults
VITE_API_BASE_URL=/api
```

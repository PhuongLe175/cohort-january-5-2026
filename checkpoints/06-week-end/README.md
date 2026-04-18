# Budget Tracker

> **This is the companion repository for the [AI-Powered .NET](https://aipowereddotnet.school/) cohort** — a 7-week hands-on program where .NET developers learn to ship AI features in production.

A full-stack budget tracking application built with ASP.NET Core 10 and React. Throughout the cohort, you'll extend this foundation with AI-powered features: CSV import with intelligent parsing, transaction auto-categorization, and natural language queries using RAG.

**Built by [Gui Ferreira](https://github.com/gsferreira)** — Microsoft MVP, .NET educator, and Dometrain author.

> **Note**: This repository contains example credentials (database passwords, API keys) for demo purposes only. These are intentionally simple values for local development. Never use these in production.

## 🏗️ Architecture Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  React Frontend │    │  ASP.NET Core   │    │   PostgreSQL    │
│   (Port 5173)   │◄──►│   Web API       │◄──►│   Database      │
│                 │    │   (Port 5295)   │    │   (Port 5432)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Technology Stack

**Backend:**
- ASP.NET Core 10 (Minimal APIs)
- Entity Framework Core
- PostgreSQL 17
- ASP.NET Core Identity for authentication
- Static API Key authentication support
- Swagger UI for API documentation
- xUnit v3 for testing

**Frontend:**
- React 18 with TypeScript
- Vite (build tool and dev server)
- React Router v7
- Tailwind CSS for styling
- Axios for API communication
- date-fns for date formatting

**Infrastructure:**
- Docker Compose for PostgreSQL database
- Testcontainers for integration testing
- Cross-platform development support

## 📋 Prerequisites

Before you begin, ensure you have the following tools installed:

### Required Tools

| Tool | Version | Download Link |
|------|---------|---------------|
| **.NET 10 SDK** | 10.0+ | [Download .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) |
| **Node.js** | 18+ | [Download Node.js](https://nodejs.org/) |
| **Docker Desktop** | Latest | [Download Docker](https://www.docker.com/products/docker-desktop) |
| **Git** | Latest | [Download Git](https://git-scm.com/) |

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone <repository-url>
cd cohort-january-5-2026
```

### 2. Environment Setup

Copy the environment template and configure your settings:

```bash
cp .env.example .env
```

Edit `.env` with your preferred settings (optional - defaults work for local development).

### 3. Database Setup

**Option A: Database Only (Recommended for Development)**

Start just the PostgreSQL database using Docker:

#### macOS/Linux:
```bash
cd docker
docker compose up -d
```

#### Windows:
```powershell
cd docker
docker compose up -d
```

**Option B: Full Stack with Docker**

Alternatively, you can run the entire stack (database, API, and web) using Docker:

```bash
# From project root
docker compose up -d
```

This will start:
- **Database:** PostgreSQL on port 5432
- **API:** ASP.NET Core on port 5295
- **Web:** React app on port 5173

**Database Configuration:**
- **Host:** localhost
- **Port:** 5432
- **Database:** budgettracker
- **Username:** budgetuser
- **Password:** budgetpass123

### 4. Backend Setup (Skip if using Option B above)

Build and run the ASP.NET Core API:

```bash
# Build the solution
dotnet build

# Run the API
cd src/BudgetTracker.Api
dotnet run
```

The API will be available at:
- **API Base URL:** http://localhost:5295
- **Swagger UI:** http://localhost:5295/swagger
- **API Status:** http://localhost:5295/ (returns "API")

### 5. Frontend Setup (Skip if using Option B above)

Install dependencies and start the React development server:

```bash
cd src/BudgetTracker.Web
npm install
npm run dev
```

The React application will be available at: http://localhost:5173

## 📖 Detailed Setup Instructions

### macOS Setup

1. **Install Homebrew** (if not already installed):
   ```bash
   /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
   ```

2. **Install .NET 10 SDK**:
   ```bash
   brew install --cask dotnet-sdk
   ```

3. **Install Node.js**:
   ```bash
   brew install node
   ```

4. **Install Docker Desktop**:
   ```bash
   brew install --cask docker
   ```

5. **Verify installations**:
   ```bash
   dotnet --version    # Should show 10.x.x
   node --version      # Should show 18.x.x or higher
   docker --version    # Should show Docker version
   ```

### Windows Setup

1. **Install .NET 10 SDK**:
   - Download from [Microsoft's website](https://dotnet.microsoft.com/download/dotnet/10.0)
   - Run the installer and follow the setup wizard

2. **Install Node.js**:
   - Download from [nodejs.org](https://nodejs.org/)
   - Choose the LTS version
   - Run the installer with default settings

3. **Install Docker Desktop**:
   - Download from [Docker's website](https://www.docker.com/products/docker-desktop)
   - Enable WSL 2 integration during installation
   - Restart your computer after installation

4. **Verify installations** (in PowerShell):
   ```powershell
   dotnet --version    # Should show 10.x.x
   node --version      # Should show 18.x.x or higher
   docker --version    # Should show Docker version
   ```

## 🔧 Development Workflow

### Running the Full Stack

**Option A: Individual Services (Recommended for Development)**

1. **Start the database**:
   ```bash
   cd docker && docker compose up -d
   ```

2. **Start the backend** (in a new terminal):
   ```bash
   cd src/BudgetTracker.Api && dotnet run
   ```

3. **Start the frontend** (in another terminal):
   ```bash
   cd src/BudgetTracker.Web && npm run dev
   ```

**Option B: Full Docker Stack**

```bash
# From project root
docker compose up -d
```

Access the applications:
- **Frontend:** http://localhost:5173
- **API:** http://localhost:5295
- **Database:** localhost:5432

### Database Migrations

When you make changes to the data model:

```bash
cd src/BudgetTracker.Api
dotnet ef migrations add MigrationName -o Infrastructure/Migrations
dotnet ef database update
```

## 🐛 Troubleshooting

### Common Issues

#### Database Connection Issues

**Problem:** Cannot connect to PostgreSQL database
**Solutions:**
1. Ensure Docker is running: `docker ps`
2. Check if database container is up: `docker compose ps`
3. Restart database: `docker compose down && docker compose up -d`
4. Verify connection string in `appsettings.json`

#### Port Conflicts

**Problem:** Port 5295 or 5173 already in use
**Solutions:**
1. Find process using port: `lsof -i :5295` (macOS/Linux) or `netstat -ano | findstr :5295` (Windows)
2. Kill the process or change ports in configuration
3. For API: Update `launchSettings.json`
4. For frontend: Update `vite.config.ts`

#### CORS Issues

**Problem:** Frontend cannot connect to API
**Solutions:**
1. Verify API is running on port 5295
2. Check CORS configuration in `Program.cs`
3. Ensure frontend is running on port 5173
4. Check browser console for specific error messages

#### Build Failures

**Problem:** `dotnet build` fails
**Solutions:**
1. Clear NuGet cache: `dotnet nuget locals all --clear`
2. Restore packages: `dotnet restore`
3. Clean and rebuild: `dotnet clean && dotnet build`

**Problem:** `npm install` fails
**Solutions:**
1. Clear npm cache: `npm cache clean --force`
2. Delete `node_modules` and `package-lock.json`
3. Reinstall: `npm install`


## 📁 Project Structure

```
cohort-january-5-2026/
├── src/
│   ├── BudgetTracker.Api/              # ASP.NET Core Web API
│   │   ├── Auth/                       # Authentication endpoints and models
│   │   ├── AntiForgery/                # Anti-forgery token endpoints
│   │   ├── Infrastructure/             # Entity Framework DbContext & migrations
│   │   ├── BudgetTracker.Api.csproj    # Project file
│   │   └── Program.cs                  # Application entry point
│   └── BudgetTracker.Web/              # React frontend
│       ├── src/
│       │   ├── components/             # React components
│       │   ├── services/               # API service layer
│       │   ├── types/                  # TypeScript type definitions
│       │   └── routes/                 # React Router components
│       ├── public/                     # Static assets
│       ├── package.json                # Node.js dependencies
│       ├── tailwind.config.js          # Tailwind CSS configuration
│       └── vite.config.ts              # Vite configuration
├── tests/
│   └── BudgetTracker.Api.Tests/        # Unit and integration tests
│       ├── Auth/                       # Authentication endpoint tests
│       ├── AntiForgery/                # Anti-forgery endpoint tests
│       ├── Extensions/                 # Test helper extensions
│       ├── Fixtures/                   # Test fixtures and setup
│       └── BudgetTracker.Api.Tests.csproj
├── docker/                             # Docker configuration
│   └── docker-compose.yml              # PostgreSQL database setup
├── .gitignore                          # Git ignore rules
├── BudgetTracker.sln                   # .NET Solution file
└── README.md                           # This file
```

## 🤝 Contributing

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Make your changes**
4. **Run tests**: `dotnet test` and `npm test`
5. **Commit your changes**: `git commit -m 'Add amazing feature'`
6. **Push to the branch**: `git push origin feature/amazing-feature`
7. **Open a Pull Request**

### Development Guidelines

- Follow C# coding conventions for backend code
- Use TypeScript strict mode for frontend code
- Write tests for new functionality
- Update documentation for API changes
- Use conventional commit messages

## 🆘 Support

If you encounter issues:

1. Review the [API documentation](http://localhost:5295/swagger) when running locally
2. Check the test files for usage examples
3. Open an issue on GitHub with detailed error information

## 🔑 API Authentication

The API supports two authentication methods:

### 1. Static API Key (for testing)
- **Header:** `X-API-Key`
- **Development Key:** `bt_dev_key_admin`
- **Usage:** Add the API key header to your requests or use the "Authorize" button in Swagger UI

### 2. Identity Cookie Authentication (for frontend)
- Register/login through the `/api/users/register` and `/api/users/login` endpoints
- Session-based authentication for web applications

### Available Endpoints:
- `POST /api/users/register` - User registration
- `POST /api/users/login` - User login
- `POST /api/users/logout` - User logout
- `GET /api/users/me` - Get current user info
- `GET /api/antiforgery/token` - Get anti-forgery token

## 🔗 Useful Links

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [React Documentation](https://reactjs.org/docs)
- [TypeScript Documentation](https://www.typescriptlang.org/docs/)
- [Tailwind CSS Documentation](https://tailwindcss.com/docs)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

---

## 🎓 Learn to Build This

This repository is part of the **AI-Powered .NET** cohort — a 7-week program for .NET developers ready to ship AI features.

**What you'll learn:**
- AI-assisted development practices
- LLM fundamentals and Azure AI integration
- Building RAG systems for natural language queries
- Agentic workflows with tool calling

**[Join the next cohort →](https://aipowereddotnet.school/)**
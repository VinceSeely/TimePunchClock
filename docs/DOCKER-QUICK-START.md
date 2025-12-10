# Docker Quick Start Guide - TimePunchClock

## TL;DR - Docker Commands

### Local Development

```bash
# Start everything (recommended for local dev)
docker-compose up

# Build and start in background
docker-compose up -d

# Rebuild after code changes
docker-compose up --build

# Stop everything
docker-compose down

# View logs
docker-compose logs -f
```

### Build Individual Services

```bash
# Backend API
docker build -t timeapi:local -f src/TimeApi/Dockerfile ./src

# Frontend UI
docker build -t timeclockui:local -f src/TimeClockUI/Dockerfile ./src

# IMPORTANT: Build context must be ./src (not . or root)
```

## Project Structure

```
TimePunchClock/
├── src/
│   ├── TimeApi/
│   │   ├── Dockerfile              # Backend container
│   │   └── TimeApi.csproj
│   ├── TimeClock.client/
│   │   └── TimeClock.client.csproj # Shared client library
│   └── TimeClockUI/
│       ├── Dockerfile              # Frontend container (nginx + WASM)
│       └── TimeClockUI.csproj
├── docker-compose.yml              # Local development orchestration
└── .github/workflows/
    ├── pr-validation.yml           # PR testing (builds, doesn't publish)
    ├── backend-build.yml           # Main branch (builds + publishes)
    └── backend-deploy-*.yml        # Deployment workflows
```

## Common Scenarios

### 1. First Time Setup

```bash
# Clone the repo
git clone <repo-url>
cd TimePunchClock

# Start all services (will build automatically)
docker-compose up

# Wait for services to start (may take 2-3 minutes first time)
# Backend:  http://localhost:5000
# Frontend: http://localhost:5001
# Database: localhost:1439
```

### 2. Making Code Changes

```bash
# After changing backend code
docker-compose up --build timeapi

# After changing frontend code
docker-compose up --build timeclockui

# Rebuild everything
docker-compose up --build
```

### 3. Testing Before PR

```bash
# Build backend (same as CI/CD)
docker build -t timeapi:test -f src/TimeApi/Dockerfile ./src

# Build frontend (same as CI/CD)
docker build -t timeclockui:test -f src/TimeClockUI/Dockerfile ./src

# If these succeed, CI/CD will succeed too!
```

### 4. Debugging Issues

```bash
# Check service status
docker-compose ps

# View logs for specific service
docker-compose logs timeapi
docker-compose logs timeclockui
docker-compose logs mssql

# Follow logs in real-time
docker-compose logs -f timeapi

# Restart a single service
docker-compose restart timeapi

# Get into a running container
docker-compose exec timeapi bash
docker-compose exec timeclockui sh
```

### 5. Clean Slate

```bash
# Stop and remove everything
docker-compose down

# Remove volumes too (database data will be lost!)
docker-compose down -v

# Clean up unused images
docker system prune -a

# Fresh start
docker-compose up --build
```

## Environment Variables

### Backend (TimeApi)

```yaml
# In docker-compose.yml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ASPNETCORE_URLS=http://+:80
  - Authentication__Enabled=false
  - ConnectionStrings__DefaultConnection=Server=mssql;...
```

### Frontend (TimeClockUI)

```yaml
# In docker-compose.yml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - Authentication__Enabled=false
  - TimeClientBaseUrl=http://localhost:5000
```

## Troubleshooting

### Problem: "Cannot find file TimeApi/TimeApi.csproj"

**Solution**: You're using wrong build context!

```bash
# WRONG (will fail)
docker build -f src/TimeApi/Dockerfile .

# CORRECT (will work)
docker build -f src/TimeApi/Dockerfile ./src
```

### Problem: "Port already in use"

**Solution**: Another service is using the port

```bash
# Find what's using port 5000
netstat -ano | findstr :5000  # Windows
lsof -i :5000                 # Mac/Linux

# Use different ports in docker-compose.yml
ports:
  - "5002:80"  # Change 5000 to 5002
```

### Problem: Database connection fails

**Solution**: Wait for SQL Server to be ready

```bash
# Check if SQL is healthy
docker-compose ps

# SQL Server takes 30-60 seconds to start first time
# Watch logs to see when it's ready
docker-compose logs -f mssql

# Look for: "SQL Server is now ready for client connections"
```

### Problem: Frontend can't reach backend

**Solution**: Check CORS and URLs

```bash
# Verify backend is running
curl http://localhost:5000/health

# Check TimeClientBaseUrl matches
# Frontend must use: http://localhost:5000
# Not: http://timeapi or internal Docker names
```

## CI/CD Pipeline Behavior

### Pull Request (PR)
- **Workflow**: `pr-validation.yml`
- **Actions**:
  - Builds .NET projects
  - Runs unit + integration tests
  - Builds Docker images (validates they work)
  - Runs security scans (Trivy)
- **Does NOT**: Push images to any registry

### Main Branch (after merge)
- **Workflow**: `backend-build.yml`
- **Actions**:
  - Builds .NET projects
  - Runs tests
  - Builds Docker images
  - Pushes to GitHub Container Registry (GHCR)
  - Saves artifact for deployment
  - Runs security scans

### Deployment
- **Workflows**: `backend-deploy-dev.yml`, `backend-deploy-prod.yml`
- **Actions**:
  - Loads pre-built image OR rebuilds
  - Pushes to GHCR
  - Deploys to Azure Container Apps
  - Runs health checks

## Best Practices

### 1. Always Test Locally First
```bash
# Before creating PR
docker-compose up --build

# Verify both services work
curl http://localhost:5000/health
curl http://localhost:5001
```

### 2. Use Correct Build Context
```bash
# Always use ./src as context
docker build -f src/<service>/Dockerfile ./src

# This matches CI/CD and docker-compose
```

### 3. Keep Images Small
- Use multi-stage builds (already implemented)
- Don't install unnecessary packages
- Use .dockerignore to exclude files

### 4. Monitor Security Scans
- Check GitHub Security tab after PR
- Address CRITICAL and HIGH vulnerabilities
- Update base images regularly

### 5. Clean Up Regularly
```bash
# Remove stopped containers
docker container prune

# Remove unused images
docker image prune

# Remove unused volumes
docker volume prune

# Or do it all at once
docker system prune -a
```

## Quick Reference Card

| Task | Command |
|------|---------|
| Start all services | `docker-compose up` |
| Start in background | `docker-compose up -d` |
| Rebuild after changes | `docker-compose up --build` |
| Stop all services | `docker-compose down` |
| View logs | `docker-compose logs -f` |
| Test backend build | `docker build -f src/TimeApi/Dockerfile ./src` |
| Test frontend build | `docker build -f src/TimeClockUI/Dockerfile ./src` |
| Clean everything | `docker-compose down -v && docker system prune -a` |

## Service URLs

| Service | Local URL | Docker Network |
|---------|-----------|----------------|
| Backend API | http://localhost:5000 | http://timeapi:80 |
| Frontend UI | http://localhost:5001 | http://timeclockui:80 |
| SQL Server | localhost:1439 | mssql:1433 |

## Database Connection Strings

### From Host Machine
```
Server=localhost,1439;Database=TimeClockDB;User Id=sa;Password=Your_Strong_Password123!;TrustServerCertificate=True;
```

### From Docker Container
```
Server=mssql;Database=TimeClockDB;User Id=sa;Password=Your_Strong_Password123!;TrustServerCertificate=True;
```

## Need Help?

1. Check logs: `docker-compose logs -f <service-name>`
2. Verify services are running: `docker-compose ps`
3. Review CI/CD logs in GitHub Actions
4. See detailed fix documentation: `docs/CICD-DOCKER-FIX.md`

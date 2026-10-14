# TaskFlow — C# / ASP.NET Core Task Manager

A lightweight full-stack task manager built with **C#**, **ASP.NET Core 10 Minimal APIs**, and **vanilla JavaScript**. Features a responsive UI and file-backed task storage for local use.

## Features
- Create tasks with validation (1–100 characters)
- View all tasks and active task count
- Mark tasks as done / undone
- Delete tasks
- RESTful endpoints with appropriate HTTP status codes
- JSON-file storage with a semaphore for safe concurrent access inside one app instance
- Health-check endpoint and Docker deployment

## Tech Stack
C# · .NET 10 (LTS) · ASP.NET Core Minimal API · HTML/CSS/JS · Docker

## Run locally
Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), then run:

```bash
dotnet restore
dotnet run --urls http://localhost:5080
```

Visit **http://localhost:5080**.

## API
| Method | Path | Description |
| --- | --- | --- |
| GET | `/api/health` | Server health |
| GET | `/api/tasks` | List tasks |
| POST | `/api/tasks` | Create task |
| PUT | `/api/tasks/{id}` | Edit title and completion |
| DELETE | `/api/tasks/{id}` | Delete task |

Example POST JSON: `{ "title": "Learn ASP.NET Core" }`

Example PUT JSON: `{ "title": "Learn ASP.NET Core", "isCompleted": true }`

## Run with Docker
```bash
docker build -t taskflow .
docker run --rm -p 8080:8080 taskflow
```
Visit **http://localhost:8080**.

## Deploy to Render
1. Push this entire folder to a GitHub repository (with `Dockerfile` at repository root).
2. In Render, select **New > Web Service** and connect the repository.
3. Set **Language/Runtime: Docker**, and deploy. App listens on port **8080**.
4. Open the Render service URL. `/api/health` is the health-check route.

**Data warning:** Render and many other platforms use ephemeral filesystems by default. Tasks saved under `data/tasks.json` can disappear on restart/redeploy. For a demo this is acceptable. For lasting production storage, attach a persistent disk, set `TASK_DATA_DIR` to its mounted path, or replace the JSON store with PostgreSQL. JSON-file storage is only intended for one application instance.

## Architecture
- `Program.cs`: HTTP routes, input checks, and in-process storage service.
- `wwwroot/index.html`: Responsive UI that calls only same-origin `/api` endpoints.
- `data/tasks.json`: Generated automatically at runtime; ignored by Git.

## Future improvements
- Authentication and per-user tasks
- PostgreSQL + Entity Framework Core
- Automated integration tests and CI
- Pagination and filtering

## Interview summary
"I built TaskFlow, a task-management web app using C# and ASP.NET Core Minimal APIs. I implemented CRUD endpoints, input validation, asynchronous file I/O, and thread-safe access to a JSON data store. I also built a responsive JavaScript UI consuming these endpoints and containerized the app with Docker for deployment. For production scale, I'd move persistence to PostgreSQL and add authentication."

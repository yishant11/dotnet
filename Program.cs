using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TaskStore>();
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", service = "TaskFlow API" }));
app.MapGet("/api/tasks", async (TaskStore store) => Results.Ok(await store.GetAllAsync()));
app.MapPost("/api/tasks", async (TaskInput input, TaskStore store) =>
{
    var error = Validate(input.Title);
    if (error is not null) return Results.BadRequest(new { error });
    var task = await store.CreateAsync(input.Title!.Trim());
    return Results.Created($"/api/tasks/{task.Id}", task);
});
app.MapPut("/api/tasks/{id:guid}", async (Guid id, TaskUpdate input, TaskStore store) =>
{
    var error = Validate(input.Title);
    if (error is not null) return Results.BadRequest(new { error });
    var task = await store.UpdateAsync(id, input.Title!.Trim(), input.IsCompleted);
    return task is null ? Results.NotFound(new { error = "Task not found" }) : Results.Ok(task);
});
app.MapDelete("/api/tasks/{id:guid}", async (Guid id, TaskStore store) =>
    await store.DeleteAsync(id) ? Results.NoContent() : Results.NotFound(new { error = "Task not found" }));

app.Run();

static string? Validate(string? title) => string.IsNullOrWhiteSpace(title) ? "Title is required" : title.Trim().Length > 100 ? "Title must be at most 100 characters" : null;

record TaskInput(string? Title);
record TaskUpdate(string? Title, bool IsCompleted);
record TodoTask(Guid Id, string Title, bool IsCompleted, DateTimeOffset CreatedAt);

sealed class TaskStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public TaskStore(IConfiguration configuration)
    {
        var folder = configuration["TASK_DATA_DIR"] ?? Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "tasks.json");
    }

    private async Task<List<TodoTask>> LoadAsync()
    {
        if (!File.Exists(_path)) return new();
        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<List<TodoTask>>(stream, Options) ?? new();
    }

    private async Task SaveAsync(List<TodoTask> tasks)
    {
        var temp = _path + ".tmp";
        await using (var stream = File.Create(temp))
            await JsonSerializer.SerializeAsync(stream, tasks, Options);
        File.Move(temp, _path, true);
    }

    public async Task<List<TodoTask>> GetAllAsync()
    {
        await _gate.WaitAsync();
        try { return (await LoadAsync()).OrderByDescending(t => t.CreatedAt).ToList(); }
        finally { _gate.Release(); }
    }

    public async Task<TodoTask> CreateAsync(string title)
    {
        await _gate.WaitAsync();
        try
        {
            var tasks = await LoadAsync();
            var task = new TodoTask(Guid.NewGuid(), title, false, DateTimeOffset.UtcNow);
            tasks.Add(task);
            await SaveAsync(tasks);
            return task;
        }
        finally { _gate.Release(); }
    }

    public async Task<TodoTask?> UpdateAsync(Guid id, string title, bool isCompleted)
    {
        await _gate.WaitAsync();
        try
        {
            var tasks = await LoadAsync();
            var index = tasks.FindIndex(t => t.Id == id);
            if (index < 0) return null;
            tasks[index] = tasks[index] with { Title = title, IsCompleted = isCompleted };
            await SaveAsync(tasks);
            return tasks[index];
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _gate.WaitAsync();
        try
        {
            var tasks = await LoadAsync();
            if (tasks.RemoveAll(t => t.Id == id) == 0) return false;
            await SaveAsync(tasks);
            return true;
        }
        finally { _gate.Release(); }
    }
}

# MonadicSharp + AI Code Assistants

> Teach your AI code assistant to write clean, Railway-Oriented C# — automatically.

MonadicSharp comes with ready-made configuration files for the most popular AI coding tools.
Drop one file in your project and your AI will generate `Result<T>` pipelines instead of try/catch spaghetti.

---

## Quick start — pick your tool

### Cursor
Copy `.cursorrules` to the root of your project:
```bash
curl -o .cursorrules https://raw.githubusercontent.com/Danny4897/MonadicSharp/main/.cursorrules
```
Cursor automatically reads `.cursorrules` and applies the patterns to all C# suggestions.

---

### GitHub Copilot
Copy `copilot-instructions.md` to `.github/`:
```bash
mkdir -p .github
curl -o .github/copilot-instructions.md \
  https://raw.githubusercontent.com/Danny4897/MonadicSharp/main/.github/copilot-instructions.md
```
Copilot reads `.github/copilot-instructions.md` as project-level context for all suggestions.

---

### Claude (claude.ai Projects)
1. Open your Claude Project → **Project Instructions**
2. Paste the contents of `claude-system-prompt.md`
3. Every conversation in that project will now suggest MonadicSharp patterns

Or use it in API calls:
```csharp
var response = await client.Messages.CreateAsync(new()
{
    Model   = "claude-sonnet-4-6",
    System  = File.ReadAllText("claude-system-prompt.md"),
    Messages = [new() { Role = "user", Content = userMessage }]
});
```

---

### MCP Server (Cursor, Claude Desktop, any MCP-compatible tool)
MonadicSharp ships an MCP server that provides code generation tools and pattern resources directly to your AI assistant.

Install globally:
```bash
npm install -g monadic-sharp-mcp
```

Add to your MCP config (e.g. `~/.cursor/mcp.json` or Claude Desktop settings):
```json
{
  "mcpServers": {
    "monadic-sharp": {
      "command": "monadic-sharp-mcp"
    }
  }
}
```

Available MCP tools after connecting:
- `generate_monadic_snippet` — generates a MonadicSharp pattern for a given scenario
- Resources: `monadic-sharp://rop-basics`, `monadic-sharp://ai-patterns`

---

## What changes after setup

**Before** (what your AI generates without MonadicSharp context):
```csharp
public async Task<User> CreateUserAsync(string name, string email)
{
    if (string.IsNullOrEmpty(name))
        throw new ArgumentException("Name is required");

    var existing = await _repo.FindByEmailAsync(email);
    if (existing != null)
        throw new InvalidOperationException("Email already taken");

    var user = new User(name, email);
    await _repo.SaveAsync(user);
    return user;
}
```

**After** (what your AI generates with MonadicSharp):
```csharp
public async Task<Result<User>> CreateUserAsync(string name, string email)
    => await ValidateName(name)
        .Bind(_ => ValidateEmail(email))
        .Bind(_ => CheckEmailAvailableAsync(email))
        .Map(_ => new User(name, email))
        .Bind(user => _repo.SaveAsync(user));
```

Same logic. Zero try/catch. Honest types. Errors propagate automatically.

---

## For AI agent / LLM projects

If you're building AI agents in C#, the `MonadicSharp.AI` patterns in these files also teach your assistant to:

- Return `AiError` instead of catching `HttpRequestException`
- Use `.WithRetry()` for resilient LLM calls with exponential backoff
- Structure multi-step pipelines with `AgentResult`
- Write self-healing pipelines with `MonadicSharp.Recovery`

---

## Install MonadicSharp

```bash
# Core (Result<T>, Option<T>, Error, Try)
dotnet add package MonadicSharp

# AI/LLM extensions
dotnet add package MonadicSharp.AI

# Self-healing pipelines
dotnet add package MonadicSharp.Recovery

# Everything at once
dotnet add package MonadicSharp.Framework
```

---

## Links

- GitHub: https://github.com/Danny4897/MonadicSharp
- NuGet: https://www.nuget.org/profiles/Klexir
- Framework repo: https://github.com/Danny4897/MonadicSharp.Framework
- AI extensions: https://github.com/Danny4897/MonadicSharp.AI

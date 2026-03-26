# Getting Started

MonadicSharp for OpenCode brings green-code analysis and auto-migration into your OpenCode AI coding session.

## Requirements

- [OpenCode](https://opencode.ai) installed
- [MonadicLeaf](https://danny4897.github.io/MonadicLeaf/) (`dotnet tool install -g MonadicLeaf`)
- .NET 8.0+

## Install the integration

```bash
# Install MonadicLeaf CLI (required)
dotnet tool install -g MonadicLeaf

# Clone the OpenCode integration
git clone https://github.com/Danny4897/MonadicSharp-OpenCode
cd MonadicSharp-OpenCode

# Install into OpenCode
opencode plugin install .
```

## Available commands

Once installed, three slash commands are available inside any OpenCode session:

| Command | Description |
|---------|-------------|
| `/forge-analyze` | Analyze current file for green-code violations |
| `/green-check` | Run full Green Score on the project |
| `/migrate` | Auto-migrate violations in the current file |

## Your first analysis

Open a C# file in OpenCode, then type:

```
/forge-analyze
```

MonadicSharp for OpenCode will:
1. Send the file to MonadicLeaf for analysis
2. Return a structured list of violations
3. Suggest MonadicSharp-idiomatic fixes inline

## Configure green-code defaults

Add a `.monadicleaf.json` at your project root to configure the minimum acceptable Green Score and which rules to enforce:

```json
{
  "minGreenScore": 80,
  "failOnSeverity": "error",
  "excludePaths": ["**/Migrations/**"]
}
```

## Next steps

- [/forge-analyze](./commands/forge-analyze) — detailed command reference
- [What is Green Code?](./green-code) — the philosophy behind Railway-Oriented Programming
- [ROL Patterns](./rol-patterns) — common patterns and transformations

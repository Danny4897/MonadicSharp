# MonadicSharp Video Template — Remotion

React → MP4 video template for MonadicSharp YouTube content.
Produces 1920×1080 videos at 30fps with Shiki syntax highlighting and animated before/after transitions.

---

## Stack

| Tool | Purpose |
|------|---------|
| **Remotion** | React components → MP4 video |
| **Shiki** | C# syntax highlighting (VS Code identical) |
| **Shiki Magic Move** | Animated BEFORE → AFTER code transition |
| **JetBrains Mono** | Code font (same as shown in VS Code) |

---

## Setup (one time)

```bash
cd remotion
npm install

# Optional: install JetBrains Mono font for accurate preview
# (Remotion bundles it automatically at render time)
```

---

## Preview in browser

```bash
npm run studio
# Opens Remotion Studio at http://localhost:3000
# Live-preview the video with your props, scrub the timeline
```

---

## Render a video locally

```bash
npm run render -- --props props.json --output output/video.mp4
```

Where `props.json` follows this shape:

```json
{
  "title": "Stop Writing try/catch — Use Result<T> Instead",
  "screens": [
    {
      "order": 1,
      "label": "BEFORE — exception-driven",
      "code": "public async Task<OrderDto> ProcessOrderAsync...",
      "durationSeconds": 18
    },
    {
      "order": 2,
      "label": "AFTER — MonadicSharp pipeline",
      "code": "public async Task<Result<OrderDto>> ProcessOrderAsync...",
      "durationSeconds": 16
    }
  ],
  "sections": [
    { "sectionName": "hook",     "durationSeconds": 28,  "audioFile": "audio/hook.mp3" },
    { "sectionName": "problema", "durationSeconds": 90,  "audioFile": "audio/problema.mp3" },
    { "sectionName": "soluzione","durationSeconds": 120, "audioFile": "audio/soluzione.mp3" }
  ]
}
```

---

## Scene structure

```
HookScene          → Title card + MonadicSharp logo (hook section)
HookScene (dimmed) → Subtitle "Why exceptions fail you" (problema section)
CodeScene × N      → One per code screen (soluzione + building blocks)
BeforeAfterScene   → Shiki Magic Move animated BEFORE → AFTER
CtaScene           → Install command + GitHub link + next video
```

## Visual language

| Element | Value |
|---------|-------|
| Background | `#0d1117` (GitHub dark) |
| Primary text | `#e6edf3` |
| Secondary text | `#8b949e` |
| Green accent | `#238636` (GitHub green) |
| BEFORE badge | `#da3633` (red) |
| AFTER badge | `#238636` (green) |
| Font | JetBrains Mono |
| Code theme | `github-dark` (Shiki) |

---

## Adding a new video

The pipeline generates `props.json` automatically. To manually test a new topic:

1. Edit `src/index.ts` → update `defaultProps`
2. Run `npm run studio`
3. Tweak until happy
4. Run `npm run render`

---

## Used by

`MonadicSharp.VideoPipeline` — the automated C# pipeline that calls
`npx remotion render` with AI-generated props from GitHub + Claude API.

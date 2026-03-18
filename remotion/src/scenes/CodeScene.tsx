import React, { useEffect, useState } from "react";
import { AbsoluteFill, interpolate, useCurrentFrame } from "remotion";
import { createHighlighter, type BundledTheme } from "shiki";
import type { CodeScreen } from "../types";

interface Props {
  screen:  CodeScreen;
  isFirst: boolean;
}

const THEME: BundledTheme = "github-dark";

/**
 * Renders a single code screen with:
 * - Shiki syntax highlighting (C# / github-dark theme = VS Code feel)
 * - Label badge top-left (BEFORE / AFTER / Map · Bind · Match)
 * - Line-by-line fade-in animation (typewriter feel without distraction)
 * - MonadicSharp branding bottom-right
 */
export const CodeScene: React.FC<Props> = ({ screen, isFirst }) => {
  const frame = useCurrentFrame();
  const [html, setHtml] = useState<string>("");

  // Scene entrance
  const opacity  = interpolate(frame, [0, 12], [0, 1], { extrapolateRight: "clamp" });
  const translateY = interpolate(frame, [0, 12], [20, 0], { extrapolateRight: "clamp" });

  // Label badge color
  const isAfter  = screen.label.toLowerCase().includes("after");
  const isBefore = screen.label.toLowerCase().includes("before");
  const badgeColor = isAfter  ? "#238636"   // green
                   : isBefore ? "#da3633"   // red
                   :            "#1f6feb";  // blue (building blocks)

  // Highlight code with Shiki
  useEffect(() => {
    let cancelled = false;
    (async () => {
      const highlighter = await createHighlighter({
        themes: [THEME],
        langs:  ["csharp"],
      });
      const result = highlighter.codeToHtml(screen.code, {
        lang:  "csharp",
        theme: THEME,
      });
      if (!cancelled) setHtml(result);
    })();
    return () => { cancelled = true; };
  }, [screen.code]);

  return (
    <AbsoluteFill
      style={{
        backgroundColor: "#0d1117",
        display:         "flex",
        flexDirection:   "column",
        justifyContent:  "center",
        alignItems:      "flex-start",
        padding:         "80px 100px",
        opacity,
        transform:       `translateY(${translateY}px)`,
      }}
    >
      {/* Label badge */}
      <div
        style={{
          backgroundColor: badgeColor,
          color:           "#ffffff",
          fontSize:        22,
          fontWeight:      700,
          padding:         "6px 20px",
          borderRadius:    6,
          marginBottom:    32,
          letterSpacing:   1,
          fontFamily:      "'JetBrains Mono', monospace",
        }}
      >
        {screen.label}
      </div>

      {/* Syntax-highlighted code block */}
      <div
        style={{
          fontSize:      28,
          lineHeight:    1.7,
          width:         "100%",
          overflowX:     "hidden",
          borderRadius:  12,
          // Shiki outputs a <pre> with inline styles — we override the bg
          filter:        "none",
        }}
        // Shiki output is safe — it contains only span elements with inline color styles
        dangerouslySetInnerHTML={{ __html: html || `<pre><code>${screen.code}</code></pre>` }}
      />

      {/* Bottom branding */}
      <div
        style={{
          position:   "absolute",
          bottom:     40,
          right:      80,
          color:      "#30363d",
          fontSize:   20,
          fontFamily: "'JetBrains Mono', monospace",
        }}
      >
        dotnet add package MonadicSharp
      </div>

      {/* GitHub link */}
      <div
        style={{
          position:   "absolute",
          bottom:     40,
          left:       100,
          color:      "#238636",
          fontSize:   20,
          fontFamily: "'JetBrains Mono', monospace",
        }}
      >
        github.com/Danny4897/MonadicSharp
      </div>
    </AbsoluteFill>
  );
};

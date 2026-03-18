import React, { useEffect, useState } from "react";
import { AbsoluteFill, interpolate, useCurrentFrame, useVideoConfig } from "remotion";
import { createHighlighter } from "shiki";
import { ShikiMagicMove } from "shiki-magic-move/react";
import "shiki-magic-move/dist/style.css";
import type { CodeScreen } from "../types";

interface Props {
  beforeScreen: CodeScreen;
  afterScreen:  CodeScreen;
}

const THEME    = "github-dark";
const LANG     = "csharp";
// Transition timing: show BEFORE for 40% of the scene, animate, show AFTER for 40%
const BEFORE_RATIO = 0.40;
const AFTER_RATIO  = 0.40;

/**
 * The climax of the video: animated BEFORE → AFTER transition.
 *
 * Uses Shiki Magic Move to morph the bad exception-based code
 * into the MonadicSharp pipeline line-by-line with smooth FLIP animations.
 *
 * Timeline:
 *   0%–40%  → BEFORE code visible, red badge
 *   40%–60% → Shiki Magic Move animates the transition (~600ms)
 *   60%–100% → AFTER code visible, green badge
 */
export const BeforeAfterScene: React.FC<Props> = ({ beforeScreen, afterScreen }) => {
  const frame = useCurrentFrame();
  const { durationInFrames, fps } = useVideoConfig();

  const [highlighter, setHighlighter] = useState<Awaited<ReturnType<typeof createHighlighter>> | null>(null);

  const progress = frame / durationInFrames; // 0 → 1
  const showAfter = progress > BEFORE_RATIO;

  // Current code shown to ShikiMagicMove (it animates on prop change)
  const currentCode = showAfter ? afterScreen.code : beforeScreen.code;

  // Badge opacity and color
  const badgeColor  = showAfter ? "#238636" : "#da3633";
  const badgeLabel  = showAfter ? "✅ AFTER — MonadicSharp" : "❌ BEFORE — try/catch";

  // Scene fade in
  const sceneOpacity = interpolate(frame, [0, 8], [0, 1], { extrapolateRight: "clamp" });

  // "SAME LOGIC" label fades in after AFTER is shown
  const sameLogicOpacity = interpolate(
    frame,
    [durationInFrames * 0.65, durationInFrames * 0.75],
    [0, 1],
    { extrapolateRight: "clamp" }
  );

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const h = await createHighlighter({ themes: [THEME], langs: [LANG] });
      if (!cancelled) setHighlighter(h);
    })();
    return () => { cancelled = true; };
  }, []);

  return (
    <AbsoluteFill
      style={{
        backgroundColor: "#0d1117",
        display:         "flex",
        flexDirection:   "column",
        justifyContent:  "center",
        alignItems:      "flex-start",
        padding:         "60px 100px",
        opacity:         sceneOpacity,
      }}
    >
      {/* Badge */}
      <div
        style={{
          backgroundColor: badgeColor,
          color:           "#ffffff",
          fontSize:        24,
          fontWeight:      700,
          padding:         "8px 24px",
          borderRadius:    8,
          marginBottom:    28,
          letterSpacing:   1,
          fontFamily:      "'JetBrains Mono', monospace",
          transition:      "background-color 0.4s ease",
        }}
      >
        {badgeLabel}
      </div>

      {/* Shiki Magic Move — animates between before/after code */}
      {highlighter ? (
        <ShikiMagicMove
          lang={LANG}
          theme={THEME}
          highlighter={highlighter}
          code={currentCode}
          options={{
            duration:    600,
            stagger:     0.3,
            lineNumbers: false,
          }}
          style={{
            fontSize:     28,
            lineHeight:   1.7,
            width:        "100%",
            borderRadius: 12,
            fontFamily:   "'JetBrains Mono', monospace",
          }}
        />
      ) : (
        // Fallback while highlighter loads
        <pre style={{ color: "#e6edf3", fontSize: 28, lineHeight: 1.7 }}>
          {currentCode}
        </pre>
      )}

      {/* "Same logic. 4 lines." label — appears after AFTER is shown */}
      <div
        style={{
          marginTop:  28,
          color:      "#238636",
          fontSize:   32,
          fontWeight: 700,
          opacity:    sameLogicOpacity,
          fontFamily: "'JetBrains Mono', monospace",
        }}
      >
        Same logic. {afterScreen.code.split("\n").length} lines. Every failure typed.
      </div>

      {/* Install command */}
      <div
        style={{
          position:        "absolute",
          bottom:          40,
          left:            100,
          right:           100,
          display:         "flex",
          justifyContent:  "space-between",
          color:           "#8b949e",
          fontSize:        20,
          fontFamily:      "'JetBrains Mono', monospace",
        }}
      >
        <span style={{ color: "#238636" }}>dotnet add package MonadicSharp</span>
        <span>github.com/Danny4897/MonadicSharp</span>
      </div>
    </AbsoluteFill>
  );
};

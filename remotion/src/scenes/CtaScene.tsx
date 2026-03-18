import React from "react";
import { AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";

/**
 * Final scene: install command + GitHub link + subscribe prompt.
 * Clean, minimal — lets the voiceover carry the moment.
 */
export const CtaScene: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const enter = spring({ frame, fps, config: { damping: 16, mass: 0.9 } });
  const opacity = interpolate(frame, [0, 10], [0, 1], { extrapolateRight: "clamp" });

  const translateY = interpolate(enter, [0, 1], [30, 0]);

  // Staggered item reveals
  const item1Opacity = interpolate(frame, [8,  18], [0, 1], { extrapolateRight: "clamp" });
  const item2Opacity = interpolate(frame, [18, 28], [0, 1], { extrapolateRight: "clamp" });
  const item3Opacity = interpolate(frame, [28, 38], [0, 1], { extrapolateRight: "clamp" });

  return (
    <AbsoluteFill
      style={{
        backgroundColor: "#0d1117",
        display:         "flex",
        flexDirection:   "column",
        justifyContent:  "center",
        alignItems:      "flex-start",
        padding:         "0 120px",
        opacity,
      }}
    >
      {/* Green accent bar */}
      <div
        style={{
          width:           interpolate(frame, [4, 20], [0, 280], { extrapolateRight: "clamp" }),
          height:          4,
          backgroundColor: "#238636",
          borderRadius:    2,
          marginBottom:    48,
        }}
      />

      {/* Headline */}
      <h2
        style={{
          color:      "#e6edf3",
          fontSize:   64,
          fontWeight: 700,
          margin:     "0 0 60px",
          transform:  `translateY(${translateY}px)`,
          opacity,
          fontFamily: "'JetBrains Mono', monospace",
        }}
      >
        Install MonadicSharp
      </h2>

      {/* Install command */}
      <div
        style={{
          opacity:         item1Opacity,
          backgroundColor: "#161b22",
          border:          "1px solid #30363d",
          borderRadius:    12,
          padding:         "24px 40px",
          marginBottom:    32,
          width:           "100%",
          maxWidth:        900,
        }}
      >
        <span style={{ color: "#8b949e", fontSize: 28, fontFamily: "'JetBrains Mono', monospace" }}>
          ${"  "}
        </span>
        <span style={{ color: "#79c0ff", fontSize: 36, fontFamily: "'JetBrains Mono', monospace" }}>
          dotnet add package
        </span>
        <span style={{ color: "#e6edf3", fontSize: 36, fontFamily: "'JetBrains Mono', monospace" }}>
          {" "}MonadicSharp
        </span>
      </div>

      {/* GitHub link */}
      <div
        style={{
          opacity:    item2Opacity,
          color:      "#238636",
          fontSize:   32,
          fontFamily: "'JetBrains Mono', monospace",
          marginBottom: 20,
        }}
      >
        ★ github.com/Danny4897/MonadicSharp
      </div>

      {/* Subscribe nudge */}
      <div
        style={{
          opacity:    item3Opacity,
          color:      "#8b949e",
          fontSize:   28,
          fontFamily: "'JetBrains Mono', monospace",
        }}
      >
        Next video →
        <span style={{ color: "#e6edf3", marginLeft: 12 }}>
          Null Checks Are Making Your Code Ugly — Option&lt;T&gt;
        </span>
      </div>

      {/* MonadicSharp logo bottom-right */}
      <div
        style={{
          position:   "absolute",
          bottom:     48,
          right:      80,
          display:    "flex",
          alignItems: "center",
          gap:        12,
          opacity:    item3Opacity,
        }}
      >
        <div
          style={{
            width:           44,
            height:          44,
            borderRadius:    10,
            backgroundColor: "#238636",
            display:         "flex",
            alignItems:      "center",
            justifyContent:  "center",
            fontSize:        22,
            fontWeight:      700,
            color:           "#ffffff",
            fontFamily:      "'JetBrains Mono', monospace",
          }}
        >
          M#
        </div>
        <span style={{ color: "#8b949e", fontSize: 26, fontFamily: "'JetBrains Mono', monospace" }}>
          MonadicSharp
        </span>
      </div>
    </AbsoluteFill>
  );
};

import React from "react";
import { AbsoluteFill, spring, useCurrentFrame, useVideoConfig, interpolate } from "remotion";

interface Props {
  title:    string;
  subtitle?: string;
  dimmed?:  boolean;
}

/**
 * Opening scene: dark background, MonadicSharp logo top-left,
 * title animates in from bottom with spring physics.
 */
export const HookScene: React.FC<Props> = ({ title, subtitle, dimmed = false }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  // Title spring entrance
  const titleY = spring({ frame, fps, config: { damping: 14, mass: 0.8 } });
  const titleOpacity = interpolate(frame, [0, 8], [0, 1], { extrapolateRight: "clamp" });

  // Subtitle fades in after title
  const subtitleOpacity = interpolate(frame, [12, 24], [0, 1], { extrapolateRight: "clamp" });

  // Green accent bar width
  const barWidth = interpolate(frame, [4, 20], [0, 320], { extrapolateRight: "clamp" });

  return (
    <AbsoluteFill
      style={{
        backgroundColor: dimmed ? "#080d12" : "#0d1117",
        display:         "flex",
        flexDirection:   "column",
        justifyContent:  "center",
        alignItems:      "flex-start",
        padding:         "0 120px",
        transition:      "background-color 0.5s",
      }}
    >
      {/* MonadicSharp logo — top left */}
      <div
        style={{
          position: "absolute",
          top:      48,
          left:     80,
          display:  "flex",
          alignItems: "center",
          gap:      12,
          opacity:  titleOpacity,
        }}
      >
        <div
          style={{
            width:           36,
            height:          36,
            borderRadius:    8,
            backgroundColor: "#238636",
            display:         "flex",
            alignItems:      "center",
            justifyContent:  "center",
            fontSize:        18,
            fontWeight:      700,
            color:           "#ffffff",
            fontFamily:      "'JetBrains Mono', monospace",
          }}
        >
          M#
        </div>
        <span style={{ color: "#8b949e", fontSize: 22, letterSpacing: 1 }}>
          MonadicSharp
        </span>
      </div>

      {/* Green accent bar */}
      <div
        style={{
          width:           barWidth,
          height:          4,
          backgroundColor: "#238636",
          borderRadius:    2,
          marginBottom:    32,
        }}
      />

      {/* Main title */}
      <h1
        style={{
          color:      "#e6edf3",
          fontSize:   72,
          fontWeight: 700,
          lineHeight: 1.15,
          margin:     0,
          maxWidth:   1400,
          transform:  `translateY(${interpolate(titleY, [0, 1], [40, 0])}px)`,
          opacity:    titleOpacity,
          fontFamily: "'JetBrains Mono', monospace",
        }}
      >
        {title}
      </h1>

      {/* Optional subtitle */}
      {subtitle && (
        <p
          style={{
            color:     "#8b949e",
            fontSize:  36,
            marginTop: 24,
            opacity:   subtitleOpacity,
            fontFamily:"'JetBrains Mono', monospace",
          }}
        >
          {subtitle}
        </p>
      )}

      {/* Bottom watermark */}
      <div
        style={{
          position:  "absolute",
          bottom:    48,
          right:     80,
          color:     "#30363d",
          fontSize:  20,
          fontFamily:"'JetBrains Mono', monospace",
        }}
      >
        github.com/Danny4897/MonadicSharp
      </div>
    </AbsoluteFill>
  );
};

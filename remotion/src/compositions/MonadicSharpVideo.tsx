import React from "react";
import { AbsoluteFill, Series, Audio } from "remotion";
import type { VideoProps } from "../types";
import { sec } from "../types";
import { HookScene }        from "../scenes/HookScene";
import { CodeScene }        from "../scenes/CodeScene";
import { BeforeAfterScene } from "../scenes/BeforeAfterScene";
import { CtaScene }         from "../scenes/CtaScene";
import { AudioTrack }       from "./AudioTrack";

export const MonadicSharpVideo: React.FC<VideoProps> = ({ title, screens, sections }) => {
  const hookSection    = sections[0];
  const contentScreens = screens.slice(0, -1);   // all except last
  const afterScreen    = screens[screens.length - 1]; // always the AFTER screen
  const ctaSection     = sections[sections.length - 1];

  return (
    <AbsoluteFill style={{ backgroundColor: "#0d1117", fontFamily: "'JetBrains Mono', monospace" }}>

      {/* ── Audio tracks — one per section ──────────────────────────────── */}
      <AudioTrack sections={sections} />

      {/* ── Scenes in sequence ──────────────────────────────────────────── */}
      <Series>

        {/* Hook — title card + pain point statement */}
        <Series.Sequence durationInFrames={sec(hookSection.durationSeconds)}>
          <HookScene title={title} />
        </Series.Sequence>

        {/* Problema — same hook background, no code yet (audio carries it) */}
        <Series.Sequence durationInFrames={sec(sections[1].durationSeconds)}>
          <HookScene title={title} subtitle="Why exceptions fail you" dimmed />
        </Series.Sequence>

        {/* Code screens — soluzione + building blocks */}
        {contentScreens.map((screen, i) => (
          <Series.Sequence
            key={screen.order}
            durationInFrames={sec(screen.durationSeconds)}
          >
            <CodeScene screen={screen} isFirst={i === 0} />
          </Series.Sequence>
        ))}

        {/* Before/After — animated Shiki Magic Move transition */}
        <Series.Sequence durationInFrames={sec(sections[4].durationSeconds)}>
          <BeforeAfterScene
            beforeScreen={screens[0]}  // always BEFORE first
            afterScreen={afterScreen}
          />
        </Series.Sequence>

        {/* CTA — install command + GitHub link */}
        <Series.Sequence durationInFrames={sec(ctaSection.durationSeconds)}>
          <CtaScene />
        </Series.Sequence>

      </Series>
    </AbsoluteFill>
  );
};

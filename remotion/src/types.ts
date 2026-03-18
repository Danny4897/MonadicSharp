// ── Shared types — mirrored from MonadicSharp.VideoPipeline Models ───────────

export interface CodeScreen {
  order:           number;
  label:           string;  // e.g. "BEFORE", "AFTER", "Map · Bind · Match"
  code:            string;
  durationSeconds: number;
}

export interface AudioSection {
  sectionName:     string;
  durationSeconds: number;
  audioFile:       string;  // relative path: "audio/hook.mp3"
}

export interface VideoProps {
  title:    string;
  screens:  CodeScreen[];
  sections: AudioSection[];
}

// ── Timeline helpers ──────────────────────────────────────────────────────────

export const FPS = 30;

/** Convert seconds to Remotion frames */
export const sec = (s: number) => s * FPS;

/** Sum durations of sections up to (not including) index i */
export function sectionStartFrame(sections: AudioSection[], i: number): number {
  return sections
    .slice(0, i)
    .reduce((acc, s) => acc + sec(s.durationSeconds), 0);
}

/** Map each screen to its start frame based on cumulative section durations.
 *  Screens are distributed across the video proportionally. */
export function screenStartFrame(
  screens:  CodeScreen[],
  sections: AudioSection[],
  index:    number
): number {
  // Skip hook (section 0) and problema (section 1) — show screens from soluzione onward
  const contentStart = sectionStartFrame(sections, 2);
  const elapsed = screens
    .slice(0, index)
    .reduce((acc, s) => acc + sec(s.durationSeconds), 0);
  return contentStart + elapsed;
}

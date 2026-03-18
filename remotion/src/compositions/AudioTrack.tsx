import React from "react";
import { Audio, Series } from "remotion";
import type { AudioSection } from "../types";
import { sec } from "../types";

interface Props {
  sections: AudioSection[];
}

/**
 * Renders one <Audio> tag per section, each starting at the correct frame.
 * Remotion handles sync automatically via Series.Sequence timing.
 */
export const AudioTrack: React.FC<Props> = ({ sections }) => (
  <Series>
    {sections.map((section) => (
      <Series.Sequence
        key={section.sectionName}
        durationInFrames={sec(section.durationSeconds)}
      >
        <Audio src={section.audioFile} />
      </Series.Sequence>
    ))}
  </Series>
);

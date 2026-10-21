export interface ContentCreationData {
  topics: TopicOption[];
  prompts: PromptOption[];
  citations: CitationOption[];
  brandKits: BrandKitOption[];
  audienceSegments: AudienceSegmentOption[];
}

export interface TopicOption {
  id: number;
  topic: string;
  description?: string;
  searchVolume: number;
}

export interface PromptOption {
  id: number;
  promptText: string;
  category?: string;
  priority: number;
}

export interface CitationOption {
  id: number;
  url: string;
  title: string;
  citationCount: number;
  domain?: string;
}

export interface BrandKitOption {
  id: number;
  name: string;
  description?: string;
}

export interface AudienceSegmentOption {
  id: number;
  name: string;
  description?: string;
}

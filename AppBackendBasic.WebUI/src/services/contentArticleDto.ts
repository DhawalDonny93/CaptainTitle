// DTO for creating content articles - matches backend CreateContentArticleDto
export interface CreateContentArticleDto {
  projectId: number;
  title: string;
  slug?: string;
  contentHtml?: string;
  contentMarkdown?: string;
  metaTitle?: string;
  metaDescription?: string;
  featuredImageUrl?: string;
  status: 'Draft' | 'Scheduled' | 'Published' | 'Archived';
  scheduledPublishAt?: string;
  wordCount: number;
  aiModelUsed?: string;
  generationPrompt?: string;
  seoScore: number;
}

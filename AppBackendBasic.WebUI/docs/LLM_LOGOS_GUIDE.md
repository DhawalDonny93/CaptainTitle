# LLM Logos Implementation Guide

## Overview

This document provides comprehensive guidance on using LLM (Large Language Model) logos throughout the AuraSEO application. The logo system is designed to be reusable, consistent, and maintainable across all pages and components.

## Logo Storage

All LLM logos are stored in the following directory:

```
/public/images/llm-logos/
├── chatgpt.svg
├── claude.svg
├── gemini.svg
└── perplexity.svg
```

### Logo Files

| LLM | File | Brand Color |
|-----|------|-------------|
| **ChatGPT** | `chatgpt.svg` | Green (#10b981) |
| **Claude** | `claude.svg` | Orange (#f97316) |
| **Gemini** | `gemini.svg` | Blue (#3b82f6) |
| **Perplexity** | `perplexity.svg` | Purple (#8b5cf6) |

## LLMLogo Component

### Location

`src/components/ui/llm-logo.tsx`

### Component Props

```typescript
interface LLMLogoProps {
  name: 'ChatGPT' | 'Claude' | 'Gemini' | 'Perplexity'  // Required
  size?: 'sm' | 'md' | 'lg' | 'xl'                       // Optional, default: 'md'
  className?: string                                      // Optional, additional CSS classes
  alt?: string                                            // Optional, custom alt text
}
```

### Size Reference

| Size | Tailwind Class | Pixels | Use Case |
|------|---|---|---|
| `sm` | `w-4 h-4` | 16px | Inline text, badges |
| `md` | `w-5 h-5` | 20px | Standard text mentions |
| `lg` | `w-6 h-6` | 24px | Medium sections |
| `xl` | `w-8 h-8` | 32px | Large headers, main sections |

### Brand Colors

```typescript
const brandColors = {
  ChatGPT: '#10b981',   // Green
  Claude: '#f97316',    // Orange
  Gemini: '#3b82f6',    // Blue
  Perplexity: '#8b5cf6' // Purple
}
```

## Usage Examples

### Basic Usage

```tsx
import { LLMLogo } from '@/components/ui/llm-logo'

export function MyComponent() {
  return (
    <div>
      <LLMLogo name="ChatGPT" />
    </div>
  )
}
```

### With Size Specification

```tsx
// Small - for inline text
<LLMLogo name="Claude" size="sm" />

// Medium - for standard mentions (default)
<LLMLogo name="Gemini" size="md" />

// Large - for prominent sections
<LLMLogo name="Perplexity" size="lg" />

// Extra Large - for headers
<LLMLogo name="ChatGPT" size="xl" />
```

### With Custom CSS Classes

```tsx
// With hover effect
<LLMLogo 
  name="Claude" 
  size="md" 
  className="hover:scale-110 transition-transform"
/>

// With custom styling
<LLMLogo 
  name="Gemini" 
  size="lg" 
  className="drop-shadow-lg"
/>
```

### With Custom Alt Text

```tsx
<LLMLogo 
  name="ChatGPT" 
  alt="OpenAI ChatGPT logo"
/>
```

### Inline with Text

```tsx
// In a sentence
<p>
  Track how <LLMLogo name="ChatGPT" size="sm" /> ChatGPT 
  mentions your brand...
</p>

// With flex layout
<span className="flex items-center gap-2">
  <LLMLogo name="Claude" size="md" />
  <span>Claude</span>
</span>
```

## Implementation Patterns

### Pattern 1: Dashboard with Multiple LLMs

```tsx
import { LLMLogo } from '@/components/ui/llm-logo'

const platforms = [
  { name: 'ChatGPT' as const, color: '#10b981', pct: 78 },
  { name: 'Claude' as const, color: '#f97316', pct: 63 },
  { name: 'Gemini' as const, color: '#3b82f6', pct: 51 },
  { name: 'Perplexity' as const, color: '#8b5cf6', pct: 44 },
]

export function VisibilityDashboard() {
  return (
    <div>
      {platforms.map(p => (
        <div key={p.name} className="flex items-center gap-2">
          <LLMLogo name={p.name} size="md" />
          <span>{p.name}</span>
          <span>{p.pct}%</span>
        </div>
      ))}
    </div>
  )
}
```

### Pattern 2: Marquee with Logos

```tsx
import { LLMLogo } from '@/components/ui/llm-logo'

const llms = [
  { name: 'ChatGPT' as const, color: '#10b981' },
  { name: 'Claude' as const, color: '#f97316' },
  { name: 'Gemini' as const, color: '#3b82f6' },
  { name: 'Perplexity' as const, color: '#8b5cf6' },
]

export function PlatformsMarquee() {
  return (
    <div className="flex gap-4 overflow-x-auto">
      {llms.map(llm => (
        <div key={llm.name} className="flex items-center gap-2">
          <LLMLogo name={llm.name} size="lg" />
          <span>{llm.name}</span>
        </div>
      ))}
    </div>
  )
}
```

### Pattern 3: Badges with Logos

```tsx
import { LLMLogo } from '@/components/ui/llm-logo'

interface BadgeProps {
  name: 'ChatGPT' | 'Claude' | 'Gemini' | 'Perplexity'
  found: boolean
  rank?: number
}

export function PlatformBadge({ name, found, rank }: BadgeProps) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 text-xs rounded-full px-3 py-1 ${
        found
          ? 'bg-green-50 text-green-700 border border-green-200'
          : 'bg-gray-50 text-gray-500 border border-gray-200'
      }`}
    >
      <LLMLogo name={name} size="sm" />
      {name}
      {rank && <span>#{rank}</span>}
    </span>
  )
}
```

### Pattern 4: Inline Text with Logo

```tsx
import { LLMLogo } from '@/components/ui/llm-logo'

export function FeatureDescription() {
  return (
    <p className="text-lg leading-relaxed">
      Get visibility across{' '}
      <span className="inline-flex items-center gap-1">
        <LLMLogo name="ChatGPT" size="md" /> ChatGPT
      </span>
      {', '}
      <span className="inline-flex items-center gap-1">
        <LLMLogo name="Claude" size="md" /> Claude
      </span>
      {', '}
      <span className="inline-flex items-center gap-1">
        <LLMLogo name="Gemini" size="md" /> Gemini
      </span>
      {', and '}
      <span className="inline-flex items-center gap-1">
        <LLMLogo name="Perplexity" size="md" /> Perplexity
      </span>
      .
    </p>
  )
}
```

## TypeScript Guidelines

### Type-Safe Usage

Always use `as const` when passing LLM names in loops to maintain type safety:

```tsx
// ✅ Correct
const llms = ['ChatGPT', 'Claude', 'Gemini', 'Perplexity'] as const
llms.map(name => <LLMLogo name={name} />)

// OR
const llms = [
  { name: 'ChatGPT' as const },
  { name: 'Claude' as const },
]
```

### Importing the Component

```tsx
// Named import (recommended)
import { LLMLogo } from '@/components/ui/llm-logo'

// Default import
import LLMLogo from '@/components/ui/llm-logo'
```

## Current Implementation Locations

The LLMLogo component is currently used in the following sections of the landing page:

### 1. **Visibility Dashboard Mockup**
- **File**: `src/pages/LandingPage.tsx`
- **Section**: VisibilityMockup component
- **Usage**: Shows logo beside platform name in metrics

### 2. **Prompt Tracker Mockup**
- **File**: `src/pages/LandingPage.tsx`
- **Section**: PromptMockup component
- **Usage**: Displays logo in query result badges

### 3. **Feature 1 Description**
- **File**: `src/pages/LandingPage.tsx`
- **Section**: Feature 1 paragraph text
- **Usage**: Inline logos with LLM names

### 4. **Platforms Marquee Section**
- **File**: `src/pages/LandingPage.tsx`
- **Section**: "Monitoring brand visibility across"
- **Usage**: Scrolling carousel with logos and platform names

## Adding Logos to New Pages

To add LLM logos to a new page or component:

1. **Import the component**:
   ```tsx
   import { LLMLogo } from '@/components/ui/llm-logo'
   ```

2. **Choose the appropriate size**:
   - Use `sm` for inline text
   - Use `md` for standard mentions
   - Use `lg` for prominent sections

3. **Implement the pattern** that matches your use case (see examples above)

4. **Ensure accessibility**:
   - Always have text accompanying the logo
   - Use meaningful alt text
   - Don't rely on logos alone for meaning

## Maintenance

### Updating Logos

To update a logo:

1. Replace the corresponding SVG file in `/public/images/llm-logos/`
2. Clear browser cache
3. No code changes needed

Example: To update ChatGPT logo, replace `/public/images/llm-logos/chatgpt.svg`

### Adding New LLMs

To add support for a new LLM:

1. Add the SVG file to `/public/images/llm-logos/`
2. Update the `logoMap` in `src/components/ui/llm-logo.tsx`
3. Update the `LLMLogoProps` type to include the new LLM name
4. Update the `brandColors` documentation in this guide

## Best Practices

✅ **DO:**
- Use the LLMLogo component for consistency
- Pair logos with text labels
- Use appropriate sizes for context
- Use `as const` for type safety with LLM names
- Add custom CSS for hover/animation effects

❌ **DON'T:**
- Use the same logo file multiple times in one component (use the component instead)
- Use the logo without accompanying text
- Mix sized logos without reason (use consistent sizes in the same section)
- Hard-code logo paths (always use the component)
- Ignore accessibility concerns (always provide alt text)

## Accessibility

The LLMLogo component includes:
- `alt` attribute for screen readers
- Lazy loading (`loading="lazy"`)
- Semantic `<img>` element
- Default meaningful alt text based on LLM name

For additional accessibility:
```tsx
// Custom alt text for context
<LLMLogo 
  name="ChatGPT" 
  alt="ChatGPT platform - available for brand monitoring"
/>
```

## Performance Considerations

- SVG logos are lightweight (typically < 1KB each)
- Lazy loading is enabled by default
- Logos are cached by the browser
- No external dependencies required

## Troubleshooting

### Logo Not Showing

1. Check that the logo file exists in `/public/images/llm-logos/`
2. Verify the filename matches exactly (case-sensitive)
3. Clear browser cache
4. Check browser console for 404 errors

### Wrong Logo Size

Adjust the `size` prop:
```tsx
// Too small? Use larger size
<LLMLogo name="ChatGPT" size="lg" />

// Too large? Use smaller size
<LLMLogo name="Claude" size="sm" />
```

## References

- Component: `src/components/ui/llm-logo.tsx`
- Logo files: `/public/images/llm-logos/`
- Landing page usage: `src/pages/LandingPage.tsx`

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-04-08 | Initial implementation with ChatGPT, Claude, Gemini, Perplexity |

---

**Last Updated**: 2025-04-08  
**Maintained By**: Development Team  
**Status**: Active

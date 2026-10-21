# LLM Logos Quick Reference Card

## One-Minute Setup

### Import
```tsx
import { LLMLogo } from '@/components/ui/llm-logo'
```

### Basic Usage
```tsx
<LLMLogo name="ChatGPT" />
<LLMLogo name="Claude" />
<LLMLogo name="Gemini" />
<LLMLogo name="Perplexity" />
```

## Sizes at a Glance

```
sm  → 16px (use in badges, inline text)
md  → 20px (use in standard mentions) ← DEFAULT
lg  → 24px (use in section headers)
xl  → 32px (use in major sections)
```

## Quick Copy-Paste Examples

### Dashboard Metrics
```tsx
<div className="flex items-center gap-2">
  <LLMLogo name="ChatGPT" size="md" />
  <span>ChatGPT</span>
  <span>78%</span>
</div>
```

### Inline with Text
```tsx
<p>
  Get visibility across <LLMLogo name="ChatGPT" size="sm" /> ChatGPT...
</p>
```

### With Hover Effect
```tsx
<LLMLogo 
  name="Claude" 
  size="lg" 
  className="hover:scale-110 transition-transform cursor-pointer" 
/>
```

### Badge
```tsx
<span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-green-50">
  <LLMLogo name="Gemini" size="sm" />
  Gemini #3
</span>
```

### Loop Through All
```tsx
const llms = ['ChatGPT', 'Claude', 'Gemini', 'Perplexity'] as const

{llms.map(name => (
  <LLMLogo key={name} name={name} size="md" />
))}
```

## File Locations

| What | Where |
|------|-------|
| **Component** | `src/components/ui/llm-logo.tsx` |
| **Logos** | `/public/images/llm-logos/` |
| **Full Guide** | `docs/LLM_LOGOS_GUIDE.md` |

## Brand Colors

| LLM | Color | Hex |
|-----|-------|-----|
| ChatGPT | 🟢 Green | #10b981 |
| Claude | 🟠 Orange | #f97316 |
| Gemini | 🔵 Blue | #3b82f6 |
| Perplexity | 🟣 Purple | #8b5cf6 |

## Props Summary

```typescript
<LLMLogo
  name="ChatGPT"           // Required: ChatGPT|Claude|Gemini|Perplexity
  size="md"               // Optional: sm|md(default)|lg|xl
  className=""            // Optional: CSS classes
  alt="Custom alt text"   // Optional: accessibility
/>
```

## Current Usage on Landing Page

✅ Visibility Dashboard - platform metrics  
✅ Prompt Tracker - query result badges  
✅ Feature 1 Description - inline text mentions  
✅ Platforms Marquee - scrolling carousel  

## Common Patterns

### Pattern 1: With Label
```tsx
<span className="flex items-center gap-2">
  <LLMLogo name="ChatGPT" size="md" />
  ChatGPT
</span>
```

### Pattern 2: In Sentence
```tsx
<p>
  Track <LLMLogo name="Claude" size="sm" /> Claude mentions...
</p>
```

### Pattern 3: Grid Layout
```tsx
<div className="grid grid-cols-4 gap-4">
  {['ChatGPT', 'Claude', 'Gemini', 'Perplexity'].map(name => (
    <LLMLogo key={name} name={name as any} size="lg" />
  ))}
</div>
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Logo not showing | Check logo file exists in `/public/images/llm-logos/` |
| Wrong size | Change `size` prop: sm, md, lg, or xl |
| Need custom styling | Use `className` prop |
| Accessibility issue | Add `alt` prop with descriptive text |

## Key Features

✨ Type-safe with TypeScript  
✨ Lazy loading by default  
✨ SVG-based (lightweight)  
✨ Accessible with alt text  
✨ Consistent brand colors  
✨ 4 size options  
✨ Custom CSS support  

## Need More Details?

👉 See **`docs/LLM_LOGOS_GUIDE.md`** for the complete guide with advanced patterns and best practices.

---

**For React Components**: Import once, use multiple times ✨

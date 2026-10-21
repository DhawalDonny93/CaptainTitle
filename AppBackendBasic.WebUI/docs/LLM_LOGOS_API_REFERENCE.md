# LLM Logos API Reference

## Overview

The LLM Logos system provides a unified, type-safe way to display logos for ChatGPT, Claude, Gemini, and Perplexity throughout the AuraSEO application.

---

## Component: `LLMLogo`

### Import Statement

```typescript
import { LLMLogo } from '@/components/ui/llm-logo'
```

### Component Signature

```typescript
function LLMLogo(props: LLMLogoProps): JSX.Element
```

### Props Interface

```typescript
interface LLMLogoProps {
  /**
   * The name of the LLM to display
   * @type {'ChatGPT' | 'Claude' | 'Gemini' | 'Perplexity'}
   * @required
   */
  name: 'ChatGPT' | 'Claude' | 'Gemini' | 'Perplexity'

  /**
   * Size of the logo
   * @type {'sm' | 'md' | 'lg' | 'xl'}
   * @default 'md'
   * @example
   * sm  → 16px  (inline badges)
   * md  → 20px  (standard mentions)
   * lg  → 24px  (section headers)
   * xl  → 32px  (major sections)
   */
  size?: 'sm' | 'md' | 'lg' | 'xl'

  /**
   * Additional CSS classes for styling
   * @type {string}
   * @default ''
   * @example
   * className="hover:scale-110 transition-transform"
   */
  className?: string

  /**
   * Alternative text for accessibility and SEO
   * @type {string}
   * @default '{name} logo'
   * @example
   * alt="OpenAI ChatGPT logo for brand monitoring"
   */
  alt?: string
}
```

---

## Logo Assets

### Directory Structure

```
/public/images/llm-logos/
├── chatgpt.svg      (614 bytes)
├── claude.svg       (719 bytes)
├── gemini.svg       (567 bytes)
└── perplexity.svg   (963 bytes)
```

### Logo Details

| LLM | File | Size | Brand Color | SVG Format |
|-----|------|------|-------------|-----------|
| ChatGPT | `chatgpt.svg` | 614B | #10b981 | ✓ |
| Claude | `claude.svg` | 719B | #f97316 | ✓ |
| Gemini | `gemini.svg` | 567B | #3b82f6 | ✓ |
| Perplexity | `perplexity.svg` | 963B | #8b5cf6 | ✓ |

### Logo Colors & Branding

```typescript
const LLM_COLORS = {
  ChatGPT: {
    primary: '#10b981',      // Emerald Green
    light: '#d1fae5',        // Light green background
    dark: '#059669',         // Dark green accent
  },
  Claude: {
    primary: '#f97316',      // Orange
    light: '#ffedd5',        // Light orange background
    dark: '#ea580c',         // Dark orange accent
  },
  Gemini: {
    primary: '#3b82f6',      // Blue
    light: '#dbeafe',        // Light blue background
    dark: '#1d4ed8',         // Dark blue accent
  },
  Perplexity: {
    primary: '#8b5cf6',      // Purple
    light: '#ede9fe',        // Light purple background
    dark: '#6d28d9',         // Dark purple accent
  },
}
```

---

## Size Reference

### Tailwind Classes

```typescript
const SIZE_MAP = {
  sm: 'w-4 h-4',   // 16px  - 1rem
  md: 'w-5 h-5',   // 20px  - 1.25rem (default)
  lg: 'w-6 h-6',   // 24px  - 1.5rem
  xl: 'w-8 h-8',   // 32px  - 2rem
}
```

### Use Cases by Size

| Size | Pixels | Use Cases |
|------|--------|-----------|
| `sm` | 16px | Badges, inline text, compact lists |
| `md` | 20px | Standard text mentions, feature descriptions (default) |
| `lg` | 24px | Section headers, prominent cards |
| `xl` | 32px | Major section headers, hero sections, large cards |

### Visual Comparison

```
sm: ▢ (tiny)
md: ■ (small) ← default
lg: ██ (medium)
xl: ███ (large)
```

---

## Usage Patterns

### Pattern 1: Simple Display

```typescript
<LLMLogo name="ChatGPT" />
```

**Output**: ChatGPT logo at 20px (default size)

---

### Pattern 2: With Size

```typescript
<LLMLogo name="Claude" size="lg" />
```

**Output**: Claude logo at 24px

---

### Pattern 3: With Custom Styling

```typescript
<LLMLogo 
  name="Gemini" 
  size="md"
  className="hover:scale-110 transition-transform cursor-pointer drop-shadow-lg"
/>
```

**Output**: Gemini logo with hover effect and shadow

---

### Pattern 4: Inline with Text

```typescript
<span className="flex items-center gap-2">
  <LLMLogo name="ChatGPT" size="md" />
  <span className="font-semibold">ChatGPT</span>
</span>
```

**Output**: Logo with text label, aligned center

---

### Pattern 5: In a List

```typescript
const llms = ['ChatGPT', 'Claude', 'Gemini', 'Perplexity'] as const

{llms.map(name => (
  <div key={name} className="flex items-center gap-2">
    <LLMLogo name={name} size="md" />
    <span>{name}</span>
  </div>
))}
```

**Output**: List with logos for all platforms

---

### Pattern 6: Styled Badge

```typescript
<div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-green-50 border border-green-200">
  <LLMLogo name="ChatGPT" size="sm" />
  <span className="text-sm font-medium text-green-700">ChatGPT</span>
  <span className="text-xs text-green-600">#1</span>
</div>
```

**Output**: Styled badge with logo, name, and rank

---

### Pattern 7: Grid Layout

```typescript
<div className="grid grid-cols-2 md:grid-cols-4 gap-4">
  {['ChatGPT', 'Claude', 'Gemini', 'Perplexity'].map(name => (
    <div key={name} className="flex flex-col items-center gap-2 p-4 rounded-lg border">
      <LLMLogo name={name as any} size="lg" />
      <span className="font-semibold">{name}</span>
    </div>
  ))}
</div>
```

**Output**: 4-column grid with logos and labels

---

## TypeScript Support

### Type-Safe Usage

The component uses TypeScript union types for strict type checking:

```typescript
// ✅ Valid
<LLMLogo name="ChatGPT" />
<LLMLogo name="Claude" />
<LLMLogo name="Gemini" />
<LLMLogo name="Perplexity" />

// ❌ Invalid (TypeScript error)
<LLMLogo name="OpenAI" />      // Not a valid LLM name
<LLMLogo name="chatgpt" />     // Case-sensitive
```

### Type-Safe Loops

```typescript
// Method 1: Using 'as const'
const llms = ['ChatGPT', 'Claude', 'Gemini', 'Perplexity'] as const
llms.map(name => <LLMLogo name={name} />)

// Method 2: Using type assertion
const llms = ['ChatGPT', 'Claude', 'Gemini', 'Perplexity']
llms.map(name => <LLMLogo name={name as const} />)

// Method 3: Type-safe array
const llms: Array<'ChatGPT' | 'Claude' | 'Gemini' | 'Perplexity'> = [
  'ChatGPT', 'Claude', 'Gemini', 'Perplexity'
]
llms.map(name => <LLMLogo name={name} />)
```

---

## Accessibility

### Alt Text

```typescript
// Automatic (default)
<LLMLogo name="ChatGPT" />
// → alt="ChatGPT logo"

// Custom
<LLMLogo 
  name="ChatGPT" 
  alt="OpenAI ChatGPT logo - brand monitoring platform"
/>
```

### Screen Reader Support

```typescript
<figure>
  <LLMLogo name="Claude" size="lg" />
  <figcaption>Claude by Anthropic</figcaption>
</figure>
```

### ARIA Attributes (if needed)

```typescript
<div role="img" aria-label="ChatGPT logo">
  <LLMLogo name="ChatGPT" />
</div>
```

---

## Performance Characteristics

### File Sizes

```
chatgpt.svg   → 614 bytes
claude.svg    → 719 bytes
gemini.svg    → 567 bytes
perplexity.svg → 963 bytes
```

### Features

- ✓ Lazy loading enabled (`loading="lazy"`)
- ✓ Browser caching support
- ✓ Minimal DOM footprint (`<img>` element)
- ✓ No JavaScript required beyond React
- ✓ Inline SVG support

### Load Time Impact

- Initial load: <5ms per logo
- Cache hit: <1ms per logo
- Network: ~1KB total for all 4 logos

---

## Configuration

### Logo Path Mapping

```typescript
// File: src/components/ui/llm-logo.tsx
const logoMap: Record<string, string> = {
  ChatGPT: '/images/llm-logos/chatgpt.svg',
  Claude: '/images/llm-logos/claude.svg',
  Gemini: '/images/llm-logos/gemini.svg',
  Perplexity: '/images/llm-logos/perplexity.svg',
}
```

To change the logo path structure, update this mapping.

---

## Current Implementation Locations

### Landing Page (`src/pages/LandingPage.tsx`)

1. **Visibility Dashboard** (VisibilityMockup component)
   - Location: Metrics display showing platform visibility percentages
   - Usage: `<LLMLogo name={p.name} size="sm" />`

2. **Prompt Tracker** (PromptMockup component)
   - Location: Query results badges
   - Usage: `<LLMLogo name={r.p} size="sm" />`

3. **Feature 1 Description**
   - Location: Paragraph text about platform coverage
   - Usage: Inline with text in flex containers

4. **Platforms Marquee**
   - Location: "Monitoring brand visibility across" section
   - Usage: Scrolling carousel with logos and platform names

---

## Maintenance

### Updating Logos

1. Replace the SVG file in `/public/images/llm-logos/`
2. Clear browser cache
3. No code changes required

Example:
```bash
# Replace ChatGPT logo
cp new-chatgpt-logo.svg /public/images/llm-logos/chatgpt.svg
```

### Adding New LLMs

1. Add SVG file to `/public/images/llm-logos/`
2. Update `logoMap` in `src/components/ui/llm-logo.tsx`
3. Update `LLMLogoProps` type
4. Update documentation

---

## Best Practices

### Do ✅

- Use the component for all LLM logo displays
- Pair logos with text labels
- Choose appropriate sizes for context
- Use `as const` for type safety
- Add custom CSS for enhanced styling
- Provide meaningful alt text

### Don't ❌

- Manually reference logo files
- Use logos without text context
- Mix inconsistent size choices
- Hardcode color values
- Ignore accessibility requirements
- Duplicate logos instead of using the component

---

## Troubleshooting

### Logo Not Rendering

**Problem**: Logo appears as broken image

**Solutions**:
1. Verify file exists: `/public/images/llm-logos/{name}.svg`
2. Check filename matches exactly (case-sensitive)
3. Clear browser cache
4. Check console for 404 errors

---

### Wrong Size

**Problem**: Logo is too small or too large

**Solution**: Adjust the `size` prop
```typescript
<LLMLogo name="ChatGPT" size="lg" />  // Larger
<LLMLogo name="ChatGPT" size="sm" />  // Smaller
```

---

### Styling Not Applied

**Problem**: Custom CSS classes not working

**Solution**: Ensure Tailwind classes are available
```typescript
<LLMLogo 
  name="ChatGPT" 
  className="hover:scale-110"  // Works if Tailwind is included
/>
```

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-04-08 | Initial release with ChatGPT, Claude, Gemini, Perplexity support |

---

## Related Documentation

- **Quick Reference**: `docs/LLM_LOGOS_QUICK_REFERENCE.md`
- **Full Guide**: `docs/LLM_LOGOS_GUIDE.md`
- **Component**: `src/components/ui/llm-logo.tsx`
- **Logos**: `/public/images/llm-logos/`

---

## Support

For questions or issues:
1. Check the troubleshooting section above
2. Review the Quick Reference guide
3. Consult the Full Implementation Guide
4. Check the component source code

---

**Last Updated**: 2025-04-08  
**Status**: Active  
**Maintained By**: Development Team

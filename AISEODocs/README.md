# AISEO Documentation

> **Comprehensive HTML documentation for the AISEO AI Visibility & SEO Platform**

## 📚 Overview

This is a lightweight, fast, and fully responsive HTML documentation site for AISEO, inspired by modern documentation platforms like Promptmonitor and Searchable. Built with pure HTML, CSS, and vanilla JavaScript for maximum performance and simplicity.

## ✨ Features

- **📱 Fully Responsive** - Works perfectly on desktop, tablet, and mobile
- **⚡ Lightning Fast** - Pure HTML/CSS/JS with no frameworks or dependencies
- **🔍 Search Functionality** - Client-side search with highlighting
- **📖 Table of Contents** - Auto-generated TOC with scroll tracking
- **🎨 Modern Design** - Clean, professional UI inspired by leading docs sites
- **♿ Accessible** - WCAG compliant with keyboard navigation
- **📋 Code Copy** - One-click code snippet copying
- **🌙 Print Friendly** - Optimized print styles
- **🚀 SEO Optimized** - Proper meta tags and semantic HTML

## 📂 Project Structure

```
AISEODocs/
├── index.html              # Main documentation homepage
├── css/
│   └── styles.css          # Complete styling (responsive, modern)
├── js/
│   └── main.js             # Interactive features (search, nav, copy)
├── pages/                  # Individual documentation pages
│   ├── quickstart.html     # Quick start guide
│   ├── visibility-dashboard.html
│   ├── site-audits.html
│   ├── content-creation.html
│   ├── tracking-prompts.html
│   ├── mentions-citations.html
│   ├── sources-analysis.html
│   ├── competitor-tracking.html
│   ├── prompt-discovery.html
│   ├── api-overview.html
│   └── ... (more pages)
├── assets/                 # Images, icons, etc.
└── README.md              # This file
```

## 🚀 Getting Started

### Quick Start

1. **Open the documentation:**
   ```bash
   # Simply open index.html in your browser
   open AISEODocs/index.html
   
   # Or serve with a local server (recommended)
   cd AISEODocs
   python3 -m http.server 8000
   # Visit http://localhost:8000
   ```

2. **No build process required!** Pure HTML/CSS/JS means instant loading.

### Deployment

Deploy to any static hosting service:

**Netlify:**
```bash
# Drag and drop the AISEODocs folder to Netlify
# Or use Netlify CLI
netlify deploy --dir=AISEODocs --prod
```

**GitHub Pages:**
```bash
# Push to GitHub and enable Pages in repository settings
git add AISEODocs
git commit -m "Add documentation"
git push origin main
```

**Vercel:**
```bash
# Deploy with Vercel CLI
vercel --prod AISEODocs
```

## 📖 Documentation Sections

### Getting Started
- **Introduction** - Overview of AISEO platform and capabilities
- **Quick Start Guide** - Get up and running in 5 minutes
- **Metrics Overview** - Understanding key metrics and scores

### AI Visibility Tracking
- **Visibility Dashboard** - Monitor brand visibility across AI platforms
- **Tracking Prompts** - Set up and manage prompt tracking
- **Mentions & Citations** - Track brand mentions and citations
- **Sources Analysis** - Analyze which sources AI platforms cite
- **Competitor Tracking** - Monitor and compare with competitors

### Site Audits & Optimization
- **Site Audits** - Comprehensive technical and AEO audits
- **Technical SEO** - Fix technical SEO issues
- **AEO Optimization** - Optimize for Answer Engine Optimization
- **Content Analysis** - Analyze and improve content quality

### AI-Powered Content
- **Content Creation** - Generate AI-optimized content
- **Content Optimization** - Optimize existing content
- **Brand Kit** - Maintain consistent brand voice
- **Audience Segments** - Target specific audiences

### Prompt Research
- **Prompt Discovery** - Find high-value prompts
- **Prompt Analytics** - Analyze prompt performance
- **Custom Prompts** - Create and manage custom prompts

### Platform Features
- **Projects** - Manage multiple projects
- **Filters & Analytics** - Advanced filtering and analytics
- **Notifications** - Stay updated with alerts

### API Reference
- **API Overview** - Introduction to AISEO API
- **Authentication** - API authentication methods
- **Endpoints** - Complete API endpoint reference

## 🎨 Design Philosophy

### Inspired By
- **Promptmonitor.io/docs** - Clean navigation and structure
- **Searchable.com/docs** - Modern design and feature cards
- **Stripe Docs** - Clear typography and code examples
- **Tailwind Docs** - Responsive layout and search

### Key Design Principles
1. **Simplicity** - No unnecessary complexity
2. **Speed** - Optimized for fast loading
3. **Clarity** - Clear, concise documentation
4. **Accessibility** - Usable by everyone
5. **Consistency** - Uniform design language

## 🛠️ Customization

### Colors
Edit CSS variables in `css/styles.css`:
```css
:root {
    --primary-color: #2563eb;      /* Main brand color */
    --secondary-color: #f97316;    /* Accent color */
    --text-primary: #111827;       /* Main text */
    /* ... more variables */
}
```

### Navigation
Update sidebar navigation in each HTML file:
```html
<aside class="sidebar">
    <nav class="sidebar-nav">
        <!-- Add/remove sections here -->
    </nav>
</aside>
```

### Content
Each page follows this structure:
```html
<article class="doc-content">
    <h1>Page Title</h1>
    <p class="lead">Introduction</p>
    <!-- Content here -->
</article>
```

## 📊 Features Documented

Based on the AISEO codebase analysis, the documentation covers:

### ✅ Implemented Features
- ✅ Visibility Dashboard with score tracking
- ✅ Site Audits (Technical, AEO, Content)
- ✅ Mentions & Citations tracking
- ✅ Sources analysis and top domains
- ✅ Competitor tracking (Direct & SERP)
- ✅ Content creation with AI
- ✅ Prompt research and discovery
- ✅ Project management
- ✅ Advanced filters (Date range, Platform, Topics, Locations)
- ✅ Brand kit and audience segments
- ✅ Opportunity scanner
- ✅ Action plans

### 📝 API Endpoints Documented
- `/api/visibility/dashboard` - Dashboard overview
- `/api/citations/mentions/analytics` - Mentions analytics
- `/api/citations/sources/analytics` - Sources analytics
- `/api/citations/top-domains` - Top cited domains
- `/api/siteaudits/run` - Start site audit
- `/api/siteaudits/project/{id}` - Get audits
- `/api/ContentArticles` - Content management

## 🔧 Technical Details

### Performance
- **No Dependencies** - Zero external libraries
- **Minified Assets** - CSS and JS can be minified for production
- **Lazy Loading** - Images load on demand
- **Optimized Rendering** - Efficient DOM manipulation

### Browser Support
- ✅ Chrome/Edge (latest)
- ✅ Firefox (latest)
- ✅ Safari (latest)
- ✅ Mobile browsers

### Accessibility
- ✅ Semantic HTML5
- ✅ ARIA labels where needed
- ✅ Keyboard navigation
- ✅ Screen reader friendly
- ✅ High contrast ratios

## 📱 Responsive Breakpoints

```css
/* Desktop: 1280px+ */
/* Tablet: 768px - 1279px */
/* Mobile: < 768px */
```

## 🔍 Search Implementation

The search feature uses client-side JavaScript:
- Real-time search as you type
- Highlights matching text
- Keyboard shortcut: `Ctrl/Cmd + K`
- Debounced for performance

## 📋 Code Examples

All code examples include:
- Syntax highlighting
- Copy button
- Language indicators
- Proper formatting

## 🚀 Future Enhancements

Potential additions (not yet implemented):
- [ ] Dark mode toggle
- [ ] Multi-language support
- [ ] Advanced search with filters
- [ ] Version selector
- [ ] Feedback widget
- [ ] Analytics integration

## 📞 Support

For questions or issues:
- 📧 Email: support@aiseo.com
- 💬 Live Chat: Available on the platform
- 📚 FAQ: See `/pages/faq.html`

## 📄 License

Documentation © 2024 AISEO. All rights reserved.

## 🙏 Credits

Design inspiration from:
- Promptmonitor.io
- Searchable.com
- Stripe Documentation
- Tailwind CSS Documentation

---

**Built with ❤️ for the AISEO platform**

*Last Updated: February 2024*

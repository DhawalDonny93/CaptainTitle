# Product Requirements Document (PRD): AuraSEO (GEO Suite)

## 1. Project Overview
**AuraSEO** is a next-generation Generative Engine Optimization (GEO) platform. Unlike traditional SEO tools that focus on search engine results pages (SERPs), AuraSEO focuses on "Generative Engine" visibility. It helps brands track, analyze, and influence how they are mentioned by LLMs (ChatGPT, Claude, Gemini, Grok) and AI agents (Manus).

---

## 2. Target Audience
* **SaaS Founders & Marketing Teams:** Wanting to ensure their product is the "recommended" choice by AI.
* **SEO Agencies:** Pivoting from traditional search to AI-driven discovery.
* **Reputation Managers:** Monitoring how AI perceives and summarizes brand sentiment.

---

## 3. Core Feature Specifications

### F1: LLM Visibility & Citation Tracker
* **Goal:** Determine if an LLM mentions the brand for specific high-intent queries.
* **Models Supported:** ChatGPT (OpenAI), Claude (Anthropic), Gemini (Google), Grok (xAI).
* **Mechanism:** Backend workers query LLM APIs with prompts like *"What are the best [Category] for [Niche]?"* and parse the response for the user's domain/brand.
* **Output:** An **AI Visibility Score** (0-100%).

### F2: The "ProTip" Social Signal Engine (Reddit & Quora)
* **Goal:** Influence the datasets LLMs use for "Trust" and "Social Proof."
* **Mechanism:** * Monitor Reddit and Quora for niche keywords using APIs/Scrapers.
    * Identify "Gap Threads" where competitors are mentioned but the user is not.
    * **AI Drafting:** Generate a high-quality, helpful response draft.
* **Strategy:** Human-in-the-Loop (HITL). The tool provides the draft and link; the human clicks "Post" via a browser extension to avoid bot bans.

### F3: Agentic UX Auditor (Manus & Gemini)
* **Goal:** Ensure autonomous agents (like Manus or Google's AI Overviews) can "read" and "act" on the site.
* **Checks:**
    * **llms.txt:** Validate the presence and content of the `/llms.txt` standard.
    * **Schema.org:** Audit JSON-LD (Product, Organization, FAQ) for machine readability.
    * **DOM Clarity:** Check for ARIA labels and semantic HTML that AI browsers require.

### F4: Grok Real-Time Pulse
* **Goal:** Leverage X (Twitter) real-time data ingestion used by Grok.
* **Mechanism:** Track trending conversations on X and suggest "High-Engagement Replies" to boost brand indexing.

---

## 4. Technical Architecture



### Backend (.NET 9)
* **Framework:** ASP.NET Core 9 Web API.
* **Database:** PostgreSQL (with EF Core).
* **Background Jobs:** Hangfire or Quartz.NET for periodic LLM probing.
* **API Clients:** Refit or HttpClient for OpenAI, Anthropic, and xAI integrations.

### Frontend (Vite + React)
* **Framework:** React 19 (Vite) + TypeScript.
* **Styling:** Tailwind CSS + Shadcn/UI components.
* **State Management:** TanStack Query (React Query) for data fetching.
* **Charts:** Recharts for visibility trends.

---

## 5. Implementation Strategy: Human-in-the-Loop (HITL)
To maintain account safety and high-quality "seeding," the tool follows a strict HITL workflow:
1.  **Detect:** Backend finds a relevant Reddit/Quora thread.
2.  **Draft:** LLM creates a contextual response.
3.  **Approve:** User reviews the draft in the AuraSEO dashboard.
4.  **Execute:** A Chrome Extension injects the draft into the actual site, and the user manually submits.

---

## 6. Financial Model (For 50 Customers)

### Operational Costs (Monthly)
| Item | Provider | Cost |
| :--- | :--- | :--- |
| **LLM APIs** | OpenAI / Anthropic | ~$400 |
| **Search/Scraping** | Firecrawl / Serper | ~$150 |
| **Data Feeds** | Reddit/X (Third Party) | ~$200 |
| **Hosting** | Azure / DigitalOcean | $50 |
| **Total Cost** | | **$800** |

### Pricing & Margins
* **Subscription:** $149/mo (Standard) or $499/mo (Agency).
* **Total Revenue (50 users):** ~$7,450.
* **Profit Margin:** **~89%** (before labor).

---

## 7. Vibe Coding System Prompt
*Use this prompt in your AI Code Editor to build the app:*

> "Act as a Senior Full-Stack Engineer. Build a GEO (Generative Engine Optimization) platform named AuraSEO. 
> 1. Setup a .NET 9 Web API with folders for 'Core', 'Infrastructure', and 'API'.
> 2. Implement a PostgreSQL database with tables for 'Projects', 'Keywords', and 'Mentions'.
> 3. Create a Vite + React frontend using Tailwind and Shadcn/UI.
> 4. Feature: Build a Dashboard displaying an 'AI Visibility Score' gauge.
> 5. Feature: Create a 'Community Opportunities' page that lists mocked Reddit threads with an 'AI Draft' button.
> 6. Integration: Create a service in .NET that calls the OpenAI API to check for brand mentions in a simulated conversation.
> 7. Architecture: Ensure the backend is modular and the frontend is fully responsive."

---

## 8. Roadmap
* **Phase 1 (MVP):** Dashboard + ChatGPT/Gemini Tracking + Reddit Monitor.
* **Phase 2 (Extension):** Chrome Extension for draft injection.
* **Phase 3 (Agentic):** Automatic `llms.txt` generator and Technical SEO fix-it tool.
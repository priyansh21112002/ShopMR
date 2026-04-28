Let's go! 🚀

Since your Day 1–2 plan focuses on **Unity MR**, but we can maximize productivity by **parallelizing** — let's start with what I can directly help you build right now: the **backend foundation**, so that when your Unity scene is ready, it has something to talk to.

Here's what I recommend we knock out **right now**:

---

## Day 1 — Session Plan

| # | Task | Why Now |
|---|---|---|
| 1 | `docs/architecture.md` | Lock down the system design before writing code |
| 2 | `backend/app/config.py` | Centralized settings (env vars, DB URL, API keys) |
| 3 | `backend/app/models/database.py` | SQLAlchemy models (users, sessions, events, products) |
| 4 | `backend/app/models/schemas.py` | Pydantic schemas for request/response validation |
| 5 | `backend/app/main.py` | FastAPI app with CORS, lifespan, router includes |
| 6 | `backend/docker-compose.yml` | PostgreSQL + FastAPI containers |
| 7 | `backend/Dockerfile` | Backend container image |
| 8 | Boot it up | `docker-compose up` → see FastAPI docs at `localhost:8000/docs` |

We'll build **file by file**, I'll give you the complete code, you paste it in, and we move to the next one. At the end of the session you'll have a **running containerized backend** with a live Swagger UI.

---

## File 1: `docs/architecture.md`

This is your north-star reference. Paste this into `docs/architecture.md`:

```markdown
# ShopMR — System Architecture

## High-Level Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      META QUEST 3                           │
│                                                             │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌───────────┐  │
│  │Passthrough│  │ Product  │  │  Hand    │  │  Chat UI  │  │
│  │  + Room   │  │ Catalog  │  │ Tracking │  │  (LLM)   │  │
│  │  Scan     │  │ Browser  │  │ Placing  │  │           │  │
│  └──────────┘  └──────────┘  └──────────┘  └───────────┘  │
│                        │                                    │
│                   UnityWebRequest                           │
│                     (REST/JSON)                              │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTPS
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    AWS EC2 (Docker Host)                     │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              FastAPI Backend (:8000)                  │   │
│  │                                                      │   │
│  │  ┌────────────┐ ┌────────────┐ ┌────────────────┐  │   │
│  │  │  Sessions   │ │   Events   │ │Recommendations │  │   │
│  │  │  Router     │ │   Router   │ │    Router      │  │   │
│  │  └────────────┘ └────────────┘ └────────────────┘  │   │
│  │  ┌────────────┐ ┌────────────┐                     │   │
│  │  │    Chat     │ │ Dashboard  │                     │   │
│  │  │   Router    │ │   Router   │                     │   │
│  │  └────────────┘ └────────────┘                     │   │
│  │                      │                              │   │
│  │  ┌──────────────────────────────────────────────┐  │   │
│  │  │              Service Layer                    │  │   │
│  │  │                                               │  │   │
│  │  │  ab_engine  │ reco_engine │ llm_agent │ pinecone│ │   │
│  │  └──────────────────────────────────────────────┘  │   │
│  └─────────────────────────┬───────────────────────────┘   │
│                             │                               │
│  ┌──────────┐  ┌───────────┴──┐  ┌───────────┐            │
│  │PostgreSQL │  │   MLflow     │  │ Streamlit  │            │
│  │  (RDS)    │  │  (:5000)     │  │  (:8501)   │            │
│  └──────────┘  └──────────────┘  └───────────┘            │
└─────────────────────────────────────────────────────────────┘
                         │
          ┌──────────────┼──────────────┐
          ▼              ▼              ▼
   ┌───────────┐  ┌───────────┐  ┌──────────┐
   │  Pinecone  │  │   Groq    │  │ AWS S3   │
   │ Vector DB  │  │   LLM     │  │Artifacts │
   └───────────┘  └───────────┘  └──────────┘

```

## Data Flow

### 1. Session Start
```
Quest 3 → POST /api/sessions/start → Create session + A/B assignment → Return session_id + variant
```

### 2. Event Tracking
```
Quest 3 → POST /api/events → Store event (view, place, rotate, purchase, chat) → 200 OK
```

### 3. Product Recommendations
```
Quest 3 → GET /api/recommendations/{session_id} → Hybrid scoring (collaborative + semantic) → Return ranked products
```

### 4. Chat Assistant
```
Quest 3 → POST /api/chat → LangChain Agent → Tools (search, recommend, lookup, room) → Groq LLM → Response
```

### 5. Dashboard
```
Streamlit → GET /api/dashboard/metrics → Aggregate from PostgreSQL → Render charts
```

## Database Schema (PostgreSQL)

### Tables
- **users** — user_id, created_at
- **sessions** — session_id, user_id, variant (A/B), started_at, ended_at
- **events** — event_id, session_id, event_type, product_id, metadata (JSON), timestamp
- **products** — product_id, name, category, price, description, model_url, embedding_id
- **ab_experiments** — experiment_id, name, variants (JSON), created_at

## A/B Testing Design
- **Variant A (Control)**: Popularity-based recommendations
- **Variant B (Treatment)**: AI-powered hybrid recommendations (collaborative + semantic)
- **Assignment**: Deterministic hash of user_id → consistent variant
- **Metrics**: CTR, placement rate, purchase rate, session duration

## Key API Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/sessions/start` | Start session, assign A/B variant |
| POST | `/api/events` | Log user event |
| GET | `/api/recommendations/{session_id}` | Get product recommendations |
| POST | `/api/chat` | Chat with AI assistant |
| GET | `/api/dashboard/metrics` | Dashboard KPIs |
| GET | `/api/dashboard/funnel` | Conversion funnel data |
| GET | `/api/dashboard/ab-results` | A/B test statistics |
```

---

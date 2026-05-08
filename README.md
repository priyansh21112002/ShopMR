
# ShopMR: Agentic AI-Powered E-Commerce Platform on Mixed Reality

AI-powered Mixed Reality shopping platform on Meta Quest 3 with Dockerized FastAPI backend, LangChain agentic assistant, Pinecone vector search, A/B experimentation, MLflow tracking, and Streamlit analytics dashboard — deployed on AWS via GitHub Actions CI/CD.

## Demo

[Add GIF/video of passthrough + furniture placement + chat here]

## Features

- **Mixed Reality Shopping** — Browse and place life-sized 3D furniture in your actual room via Meta Quest 3 passthrough
- **AI Shopping Assistant** — Conversational agent powered by LangChain + Groq LLaMA-3.3 with tools for search, recommendations, room analysis, and budget filtering
- **Smart Recommendations** — Hybrid engine combining Pinecone semantic search, collaborative filtering, and popularity signals
- **A/B Testing** — Deterministic SHA-256 variant assignment with statistical significance testing (z-tests, Mann-Whitney U)
- **Real-time Analytics** — Streamlit dashboard with KPIs, conversion funnels, and A/B results
- **ML Experiment Tracking** — MLflow logging for recommendation model versions and metrics

## Tech Stack

| Layer | Technologies |
|-------|-------------|
| **MR Client** | Unity, C#, Meta Quest 3, Meta XR SDK, XR Interaction Toolkit |
| **Backend** | FastAPI, Pydantic, PostgreSQL, SQLAlchemy, Uvicorn |
| **AI/ML** | LangChain, Groq (LLaMA-3.3), Sentence Transformers, Pinecone, Scikit-learn, XGBoost |
| **Infrastructure** | Docker, Docker Compose, AWS (EC2, RDS, S3, ECR), GitHub Actions CI/CD |
| **Analytics** | Streamlit, Plotly, MLflow, SciPy |

## Quick Start

```bash
# Clone and start backend
git clone https://github.com/priyansh21112002/ShopMR.git
cd ShopMR/backend
docker-compose up -d

# Seed data
docker exec shopmr-backend python -m scripts.seed_products
docker exec shopmr-backend python -m scripts.index_products

# Access points
# API Docs:     http://localhost:8000/docs
# Dashboard:    http://localhost:8501
# MLflow UI:    http://localhost:5050
```

**Unity:**
1. Open `unity/ShopMR-Unity/` in Unity 6000.0.47f1
2. Load `Assets/_ShopMR/Scenes/MainMR.unity`
3. Build for Android → deploy to Quest 3

## A/B Test Results

Experiment: `reco_engine_v1` — Popularity (control) vs AI Hybrid (treatment)  
Sample: 300 simulated sessions

| Metric | Control | Treatment | Lift | p-value |
|--------|---------|-----------|------|---------|
| Placement Rate | 27.5% | 48.1% | +74.8% | <0.0001 |
| Purchase Rate | 10.8% | 30.8% | +184.2% | <0.0001 |
| Reco CTR | 67.4% | 87.0% | +29.1% | <0.0001 |

## Project Structure

```
ShopMR/
├── .github/workflows/     # CI/CD pipeline
├── backend/               # FastAPI app, Docker, scripts
│   ├── app/
│   ├── scripts/
│   └── tests/
├── dashboard/             # Streamlit analytics
├── docs/                  # Architecture, A/B results
└── unity/ShopMR-Unity/    # Unity project
```

## Architecture

[Insert architecture diagram from docs/architecture.md]

## API Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/sessions/start` | Start session, assign A/B variant |
| POST | `/api/sessions/end` | End session |
| POST | `/api/events/track` | Batch track events |
| GET | `/api/products/` | List products |
| POST | `/api/recommendations/get` | Get recommendations |
| POST | `/api/chat/message` | Chat with AI assistant |
| GET | `/api/dashboard/metrics` | KPIs |
| GET | `/api/dashboard/funnel` | Conversion funnel |
| GET | `/api/dashboard/ab-results` | A/B test statistics |


## License

MIT
```

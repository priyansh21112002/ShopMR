# ShopMR — Full Project Report

> **Agentic AI-Powered E-Commerce Platform on Mixed Reality**

---

## 1. Executive Summary

ShopMR is an end-to-end Mixed Reality e-commerce platform where users wear a **Meta Quest 3** headset, see their real room through passthrough mode, browse a virtual furniture catalog, place life-sized 3D furniture in their actual space, receive AI-driven product recommendations, and converse with an intelligent shopping assistant — all powered by a cloud-deployed, Dockerized backend with A/B experimentation, ML experiment tracking, and a live analytics dashboard.

**Key Stats:**
- 20 furniture products across 8 categories
- 300+ simulated A/B test sessions
- 74.8% placement rate lift (treatment over control, p < 0.0001)
- Full CI/CD pipeline via GitHub Actions → AWS EC2
- Zero-cost deployment (all free tiers)

---

## 2. Repository Structure

```
/Users/priyanshsrivastava/Documents/ShopMR/
├── .github/workflows/deploy.yml     # CI/CD pipeline
├── .gitignore
├── LICENSE
├── README.md
├── Project idea.md                   # Detailed 10-part project spec
│
├── backend/                          # FastAPI Python backend
│   ├── Dockerfile                    # Multi-stage Python 3.11
│   ├── docker-compose.yml            # PostgreSQL + FastAPI + MLflow + Dashboard
│   ├── requirements.txt              # 150+ Python packages
│   ├── .env                          # Secrets (Pinecone, Groq, AWS keys)
│   ├── app/
│   │   ├── main.py                   # FastAPI entry point
│   │   ├── config.py                 # Pydantic settings (env-driven)
│   │   ├── models/
│   │   │   ├── database.py           # SQLAlchemy models (users, sessions, events, products, ab_experiments)
│   │   │   └── schemas.py            # Pydantic request/response schemas
│   │   ├── routers/
│   │   │   ├── sessions.py           # POST /api/sessions/start, /end
│   │   │   ├── events.py             # POST /api/events/single, /track (batch)
│   │   │   ├── products.py           # GET /api/products/
│   │   │   ├── recommendations.py    # POST /api/recommendations/get
│   │   │   ├── chat.py               # POST /api/chat/message
│   │   │   └── dashboard.py          # GET /api/dashboard/metrics, /funnel, /ab-results
│   │   └── services/
│   │       ├── ab_engine.py          # Deterministic SHA-256 variant assignment
│   │       ├── reco_engine.py        # Hybrid recommendation engine (popularity vs AI)
│   │       ├── pinecone_client.py    # Vector embedding + Pinecone queries
│   │       └── llm_agent.py          # Groq LLaMA-3.3 agent with 4 tools
│   ├── scripts/
│   │   ├── seed_products.py          # Populate 20 products in PostgreSQL
│   │   ├── index_products.py         # Generate embeddings → Pinecone
│   │   ├── simulate_users.py         # 300 synthetic A/B sessions
│   │   ├── ab_analysis.py            # Statistical analysis + MLflow logging
│   │   └── update_model_urls.py      # Update product model URLs
│   └── tests/                        # pytest test suite
│
├── dashboard/                        # Streamlit analytics dashboard
│   ├── Dockerfile
│   ├── requirements.txt
│   └── app.py                        # KPIs, funnel, A/B results, product catalog
│
├── docs/
│   ├── architecture.md               # System architecture diagram
│   └── ab_results.md                 # A/B experiment findings
│
└── unity/ShopMR-Unity/               # Unity 6000.0.47f1 project (THIS PROJECT ROOT)
    ├── Assets/
    │   ├── _ShopMR/                  # Main project code
    │   │   ├── Scripts/
    │   │   │   ├── Core/             # SessionManager, EventTracker, DebugMenu, EditorInputFix
    │   │   │   ├── Networking/       # APIClient, DataModels
    │   │   │   ├── Catalog/          # CatalogManager, CatalogPanelController, ProductCardUI
    │   │   │   ├── Placement/        # FurniturePlacer, PlaceableFurniture, FurnitureAnchorManager
    │   │   │   └── Chat/             # ChatPanelController, ChatMessageUI
    │   │   ├── Prefabs/              # CatalogPanel, ProductCard, CategoryButton, ChatPanel, ChatMessageBubble, PlaceablePlaceholder
    │   │   ├── Materials/            # EditorFloorMat, GhostMat, PlaceholderMat
    │   │   ├── Resources/Furniture/  # 20 furniture prefabs (from FBX models)
    │   │   ├── Scenes/MainMR.unity   # Main scene
    │   │   ├── Sprites/
    │   │   └── UI/
    │   ├── ithappy/Furniture_FREE/   # Third-party furniture 3D models (FBX)
    │   ├── Oculus/                    # Meta XR SDK config
    │   ├── Plugins/Android/           # AndroidManifest.xml
    │   └── Editor/Coplay/            # Editor utility scripts (30+ setup scripts)
    ├── Packages/manifest.json         # URP, Meta XR SDK 201.0, Input System, etc.
    └── ProjectSettings/
```

---

## 3. Technology Stack

| Layer | Technology | Role |
|-------|-----------|------|
| **MR Client** | Unity 6000.0.47f1, C# | Application framework |
| **XR Hardware** | Meta Quest 3 (+ Quest 2/Pro/3S) | MR headset with passthrough, hand tracking |
| **XR SDK** | Meta XR SDK 201.0.0, OpenXR | Passthrough, hand tracking, spatial anchors, controller interaction |
| **Rendering** | Universal Render Pipeline (URP) 17.0.4 | Optimized mobile rendering |
| **UI Interaction** | Meta Interaction SDK (ISDK), OVRRaycaster | Ray + Poke canvas interaction |
| **Backend** | FastAPI (Python 3.11), Uvicorn | REST API server |
| **Database** | PostgreSQL 16 (Alpine) | Persistent storage (users, sessions, events, products) |
| **ORM** | SQLAlchemy 2.0 | Python ↔ PostgreSQL interface |
| **Validation** | Pydantic 2.13 | Request/response type safety |
| **Embeddings** | Sentence Transformers (all-MiniLM-L6-v2) | 384-dim product vectors |
| **Vector DB** | Pinecone | Semantic similarity search |
| **LLM** | Groq API (LLaMA-3.3-70B-versatile) | Agentic shopping assistant |
| **ML Tracking** | MLflow 3.11 | Experiment logging + model comparison |
| **A/B Testing** | Custom SHA-256 hashing engine | Deterministic variant assignment |
| **Stats** | SciPy (z-test, Mann-Whitney U) | Statistical significance testing |
| **Dashboard** | Streamlit 1.56 + Plotly | Live analytics visualization |
| **Containerization** | Docker + Docker Compose | 4 services orchestrated |
| **Cloud** | AWS EC2, RDS, S3, ECR | Hosting + storage |
| **CI/CD** | GitHub Actions | Automated deploy on push to main |
| **Additional ML** | Scikit-learn, XGBoost | Hybrid scoring model |

---

## 4. Backend Architecture

### 4.1 API Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `POST` | `/api/sessions/start` | Create session, assign A/B variant (control/treatment) |
| `POST` | `/api/sessions/end` | End session, record duration |
| `GET`  | `/api/sessions/active` | List active sessions (monitoring) |
| `POST` | `/api/events/single` | Track one event (view, place, rotate, scale, purchase, chat, recommend_click) |
| `POST` | `/api/events/track` | Batch track up to 100 events |
| `GET`  | `/api/events/session/{id}` | Get all events for a session |
| `GET`  | `/api/products/` | List all products (filterable by category) |
| `POST` | `/api/recommendations/get` | Get ranked recommendations (routed by A/B variant) |
| `POST` | `/api/chat/message` | Send message to LLM agent, get response |
| `GET`  | `/api/dashboard/metrics` | KPIs (sessions, events, users, revenue) |
| `GET`  | `/api/dashboard/funnel` | Conversion funnel (optionally per-variant) |
| `GET`  | `/api/dashboard/ab-results` | A/B test results + significance |
| `GET`  | `/health` | Health check (used by Docker HEALTHCHECK) |

### 4.2 Database Schema

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐
│    users     │     │   sessions   │     │    events    │
├─────────────┤     ├──────────────┤     ├──────────────┤
│ user_id (PK)│◄────│ user_id (FK) │     │ event_id (PK)│
│ device_id   │     │ session_id   │◄────│ session_id   │
│ created_at  │     │ variant      │     │ event_type   │
└─────────────┘     │ started_at   │     │ product_id   │──►┌──────────────┐
                    │ ended_at     │     │ metadata(J)  │   │   products   │
                    │ is_active    │     │ timestamp    │   ├──────────────┤
                    └──────────────┘     └──────────────┘   │ product_id   │
                                                            │ name         │
┌──────────────────┐                                        │ category     │
│ ab_experiments   │                                        │ price        │
├──────────────────┤                                        │ description  │
│ experiment_id    │                                        │ model_url    │
│ name             │                                        │ dimensions(J)│
│ variants (JSON)  │                                        │ tags (JSON)  │
│ is_active        │                                        │ embedding_id │
│ created_at       │                                        │ is_active    │
└──────────────────┘                                        └──────────────┘
```

### 4.3 Recommendation Engine

**Control (Popularity-based):**
- Score = (views × 1) + (placements × 3) + (purchases × 5)
- Normalized 0–1, return top-k

**Treatment (AI Hybrid):**
1. Build user behavior text from session events
2. Generate embedding → query Pinecone (60% weight)
3. "More like this" for current product → Pinecone (20% weight)
4. Popularity score (20% weight)
5. Combine, rank, return top-k
6. Log to MLflow

### 4.4 LLM Agent (Chat)

The chat service uses Groq's LLaMA-3.3-70B with 4 deterministic tool routes:

| Tool | Trigger Keywords | Action |
|------|------------------|--------|
| `search_products` | "show me", "find", "looking for" | Semantic search via Pinecone |
| `get_product_details` | "tell me about", "details", "price" + product_id | PostgreSQL lookup |
| `get_recommendations` | "recommend", "suggest", "what else" | Full reco engine pipeline |
| `analyze_room` | "fit", "room", "living room", "space" | Room context + product suggestions |

System prompt limits responses to 2-4 sentences, grounded in tool results.

### 4.5 A/B Testing

- **Assignment:** `SHA-256(user_id + ":" + experiment_name)[:8]` → mod 2
- **Variants:** `["control", "treatment"]`
- **Primary KPI:** Placement rate (places / views)
- **Secondary:** Purchase rate, Reco CTR, session duration
- **Statistical tests:** Two-proportion z-test (binary), Mann-Whitney U (continuous)

---

## 5. Unity (MR Client) Architecture

### 5.1 Scene Hierarchy (MainMR.unity)

```
MainMR (Scene)
├── [BuildingBlock] Camera Rig          ← Meta OVRCameraRig (head, hands, controllers)
│   └── TrackingSpace/
│       ├── CenterEyeAnchor (Camera)
│       ├── LeftHandAnchor (+ Hand Tracking, Controller Tracking)
│       └── RightHandAnchor (+ Hand Tracking, Controller Tracking)
├── [BuildingBlock] Passthrough         ← OVRPassthroughLayer (environmental)
├── GameManager (DontDestroyOnLoad)
│   ├── APIClient                       ← Networking singleton
│   ├── SessionManager                  ← Session lifecycle
│   ├── EventTracker                    ← Event dispatch
│   ├── CatalogManager                  ← Product data cache
│   ├── FurniturePlacer                 ← Placement state machine
│   └── DebugMenu                       ← F1 debug overlay
├── CatalogCanvas (World Space, 800×600, scale=0.001)
│   ├── CatalogPanel                    ← CatalogPanelController singleton
│   │   ├── TitleText ("ShopMR Catalog")
│   │   ├── CategoryRow (HorizontalLayoutGroup)
│   │   ├── CardScrollView → Viewport → Content
│   │   └── StatusText
│   ├── ISDK_RayCanvasInteraction       ← Meta ray interactable
│   └── ISDK_PokeCanvasInteraction      ← Meta poke interactable
├── ChatCanvas (World Space, 800×700, scale=0.001)
│   ├── ChatPanel                       ← ChatPanelController singleton
│   │   ├── TitleBar (Title + CloseButton)
│   │   ├── ChatScrollView → Viewport → Content
│   │   ├── StatusText ("Thinking..." / errors)
│   │   └── InputRow (TMP_InputField + SendButton)
│   ├── ISDK_RayCanvasInteraction
│   └── ISDK_PokeCanvasInteraction
├── EventSystem                         ← PointableCanvasModule + EditorInputFix
├── _MRFloorCollider                    ← Physics plane for placement raycasts
├── ControllerRayVisual                 ← Controller ray line renderer
└── PlacedFurniture (runtime)           ← Container for placed items
```

### 5.2 Script Architecture

#### Namespaces
- `ShopMR.Core` — SessionManager, EventTracker, EditorInputFix, DebugMenu, ControllerRayVisual
- `ShopMR.Networking` — APIClient, DataModels
- `ShopMR.Catalog` — CatalogManager, CatalogPanelController, ProductCardUI
- `ShopMR.Placement` — FurniturePlacer, PlaceableFurniture, FurnitureAnchorManager
- `ShopMR.Chat` — ChatPanelController, ChatMessageUI

#### Singleton Pattern (all managers)
```csharp
public static ClassName Instance { get; private set; }
void Awake() {
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
}
```

#### Key Design Patterns
| Pattern | Usage |
|---------|-------|
| Singleton | All managers (APIClient, SessionManager, EventTracker, etc.) |
| async/await | All HTTP calls — never coroutines |
| CanvasGroup show/hide | Never `SetActive(false)` on panels containing scripts |
| World-space Canvas | All UI positioned in 3D space in front of user's head |
| Event-driven | `OnProductSelected`, `OnCatalogLoaded`, `OnFurniturePlaced` |
| MaterialPropertyBlock | Per-product tint without material instances |

### 5.3 Data Flow (Unity ↔ Backend)

```
1. App Start → SessionManager → POST /api/sessions/start → receive session_id + variant
2. CatalogManager → GET /api/products/ → populate catalog UI
3. User views product → EventTracker → POST /api/events/single {type: "view"}
4. User selects product → FurniturePlacer → BeginPlacement() → ghost preview follows controller
5. User confirms → ConfirmPlacement() → POST /api/events/single {type: "place"}
6. User opens chat → ChatPanelController → type message → POST /api/chat/message → display response
7. App exit → SessionManager → POST /api/sessions/end
```

### 5.4 Input Mapping

| Action | Quest 3 | Editor |
|--------|---------|--------|
| Toggle catalog | Right A button (OVRInput.Button.One) | Tab |
| Toggle chat | Left A button (OVRInput.Button.Three) | T |
| Confirm placement | Right trigger (PrimaryIndexTrigger) | Left-click / Space |
| Cancel placement | B button (Button.Two) | Escape |
| Rotate ghost | Right thumbstick X-axis | Q/E keys, scroll wheel |
| Remove last placed | B button (when not placing) | Delete/Backspace |
| Debug menu | Left Menu + B held 1s | F1 |

### 5.5 Product Catalog (20 Items)

| ID | Name | Category | Price | Model Path |
|----|------|----------|-------|------------|
| prod-001 | Modern Leather Sofa | sofa | $899.99 | Furniture/sofa_modern_leather |
| prod-002 | Scandinavian Fabric Sofa | sofa | $649.99 | Furniture/sofa_scandinavian |
| prod-003 | Velvet Sectional Sofa | sofa | $1,299.99 | Furniture/sofa_velvet_sectional |
| prod-004 | Ergonomic Office Chair | chair | $349.99 | Furniture/chair_ergonomic |
| prod-005 | Mid-Century Accent Chair | chair | $299.99 | Furniture/chair_accent |
| prod-006 | Rattan Lounge Chair | chair | $199.99 | Furniture/chair_rattan |
| prod-007 | Oak Dining Table | table | $599.99 | Furniture/table_oak_dining |
| prod-008 | Glass Coffee Table | table | $249.99 | Furniture/table_glass_coffee |
| prod-009 | Marble Side Table | table | $179.99 | Furniture/table_marble_side |
| prod-010 | Arc Floor Lamp | lamp | $149.99 | Furniture/lamp_arc_floor |
| prod-011 | Ceramic Table Lamp | lamp | $89.99 | Furniture/lamp_ceramic_table |
| prod-012 | Industrial Bookshelf | shelf | $329.99 | Furniture/shelf_industrial |
| prod-013 | Floating Wall Shelves | shelf | $79.99 | Furniture/shelf_floating |
| prod-014 | Upholstered Platform Bed | bed | $749.99 | Furniture/bed_upholstered |
| prod-015 | Minimalist Wooden Bed Frame | bed | $499.99 | Furniture/bed_minimalist |
| prod-016 | Standing Desk | desk | $449.99 | Furniture/desk_standing |
| prod-017 | Writing Desk | desk | $279.99 | Furniture/desk_writing |
| prod-018 | Large Indoor Planter | decor | $59.99 | Furniture/decor_planter |
| prod-019 | Abstract Wall Art | decor | $129.99 | Furniture/decor_wall_art |
| prod-020 | Woven Area Rug | decor | $199.99 | Furniture/decor_area_rug |

### 5.6 Android Manifest Permissions

- `android.permission.INTERNET` — Backend communication
- `com.oculus.permission.HAND_TRACKING` — Hand interactions
- `com.oculus.permission.USE_ANCHOR_API` — Spatial anchors for placed furniture
- `com.oculus.permission.USE_SCENE` — Room scanning (floor/wall detection)
- `com.oculus.feature.PASSTHROUGH` — See-through camera feed
- Supported devices: Quest 2, Quest Pro, Quest 3, Quest 3S

---

## 6. Docker Deployment

### 6.1 Services

| Service | Image | Port | Purpose |
|---------|-------|------|---------|
| `db` | postgres:16-alpine | 5432 | PostgreSQL database |
| `backend` | Custom (Python 3.11) | 8000 | FastAPI application |
| `mlflow` | ghcr.io/mlflow/mlflow:v2.21.3 | 5050 | ML experiment tracking |
| `dashboard` | Custom (Python) | 8501 | Streamlit analytics |

### 6.2 CI/CD Pipeline (GitHub Actions)

File: `.github/workflows/deploy.yml`

Automated on push to `main`:
1. Lint code
2. Run pytest tests
3. Build Docker image
4. Push to AWS ECR
5. SSH into EC2
6. Pull latest image
7. Restart Docker Compose

---

## 7. A/B Experiment Results

**Experiment:** `reco_engine_v1` — Popularity (control) vs AI Hybrid (treatment)
**Sample:** 300 simulated sessions (~157 control, ~143 treatment)

| Metric | Control | Treatment | Lift | p-value | Significant? |
|--------|---------|-----------|------|---------|:---:|
| Placement Rate | 27.5% | 48.1% | +74.8% | <0.0001 | ✅ |
| Purchase Rate | 10.8% | 30.8% | +184.2% | <0.0001 | ✅ |
| Reco CTR | 67.4% | 87.0% | +29.1% | <0.0001 | ✅ |
| Events/Session | 13.84 | 16.76 | +21.1% | — | — |

**Conclusion:** Treatment (AI hybrid) wins decisively on all primary KPIs.

---

## 8. Dashboard Features

The Streamlit dashboard (`http://localhost:8501`) provides:

1. **KPI Cards** — Total users, sessions, events, purchases, revenue
2. **A/B Test Results** — Winner badge, per-variant table, conversion rate bar chart, engagement chart
3. **Conversion Funnel** — session_start → view → recommend_click → place → purchase (overall + per-variant + side-by-side)
4. **Product Catalog** — Category pie chart + full product table
5. **Auto-refresh** — Polls every 10s for live updates
6. **Links** — Direct access to API docs and MLflow UI

---

## 9. Key Dependencies

### Unity Packages (manifest.json)
- `com.meta.xr.sdk.all` 201.0.0 — Full Meta XR SDK (passthrough, hand tracking, interaction SDK, spatial anchors)
- `com.unity.render-pipelines.universal` 17.0.4 — URP rendering
- `com.unity.inputsystem` 1.14.0 — New Input System
- `com.unity.xr.meta-openxr` 2.5.0 — OpenXR Meta plugin
- `com.unity.ugui` 2.0.0 — UI framework
- `com.unity.ai.navigation` 2.0.7 — NavMesh (future use)
- TextMesh Pro — All UI text

### Python Backend (key packages)
- FastAPI 0.136, Uvicorn 0.46 — Web framework
- SQLAlchemy 2.0, psycopg2 — Database
- Pinecone 8.1 — Vector database
- Groq 0.37 — LLM API client
- sentence-transformers 5.4, torch 2.11 — Embeddings
- LangChain 1.2 — Agent framework (used for types, custom routing)
- MLflow 3.11 — Experiment tracking
- SciPy 1.17 — Statistical tests
- Streamlit 1.56, Plotly 6.7 — Dashboard
- XGBoost 3.2, scikit-learn 1.8 — ML models
- boto3 — AWS SDK

---

## 10. Configuration

### Backend Environment Variables
| Variable | Value | Purpose |
|----------|-------|---------|
| `DATABASE_URL` | postgresql://shopmr_user:shopmr_pass@db:5432/shopmr | PostgreSQL connection |
| `PINECONE_API_KEY` | pcsk_5qcs... | Vector DB access |
| `PINECONE_INDEX_NAME` | shopmr-products | Index name |
| `GROQ_API_KEY` | gsk_qc57... | LLM inference |
| `GROQ_MODEL` | llama-3.3-70b-versatile | Chat model |
| `EMBEDDING_MODEL` | all-MiniLM-L6-v2 | Product embeddings |
| `EMBEDDING_DIMENSION` | 384 | Vector size |
| `AWS_REGION` | ap-south-1 | Mumbai region |
| `MLFLOW_TRACKING_URI` | http://mlflow:5000 | Experiment tracking |
| `AB_DEFAULT_EXPERIMENT` | reco_engine_v1 | Active experiment |
| `AB_VARIANTS` | ["control", "treatment"] | Variant names |

### Unity Configuration
| Setting | Value |
|---------|-------|
| Default Backend URL | http://10.181.182.134:8000 |
| Backend URL (overridable) | PlayerPrefs "ShopMR_BaseUrl" |
| API Timeout | 15 seconds |
| Max ray distance | 10m |
| Ghost alpha | 0.5 |
| Catalog spawn distance | 0.8m |
| Chat spawn distance | 1.2m |
| Product tint strength | 0.75 |

---

## 11. Operational Scripts

| Script | Command | Purpose |
|--------|---------|---------|
| Seed Products | `docker exec shopmr-backend python -m scripts.seed_products` | Insert 20 products into DB |
| Index Products | `docker exec shopmr-backend python -m scripts.index_products` | Generate embeddings → Pinecone |
| Simulate Users | `docker exec shopmr-backend python -m scripts.simulate_users` | Generate 300 A/B sessions |
| A/B Analysis | `docker exec shopmr-backend python -m scripts.ab_analysis` | Statistical analysis + MLflow log |
| Update Models | `docker exec shopmr-backend python -m scripts.update_model_urls` | Update product model_url paths |

---

## 12. Feature Summary by System

### ✅ Implemented Features

**Unity MR Client:**
- [x] Passthrough mode (environmental)
- [x] Product catalog with category filtering
- [x] World-space floating UI panels (catalog + chat)
- [x] Furniture placement via controller raycast
- [x] Ghost preview with per-product color tinting
- [x] Y-axis rotation (thumbstick / Q/E keys)
- [x] Placement confirmation / cancellation
- [x] Remove last placed item
- [x] OVR Spatial Anchors for placed objects
- [x] Hand tracking support
- [x] Controller ray interaction (ISDK Ray + Poke)
- [x] Chat panel with message history
- [x] Event tracking (view, place, purchase, chat, recommend_click)
- [x] Session lifecycle management
- [x] Runtime backend URL configuration (debug menu)
- [x] Editor input compatibility (mouse clicks, keyboard shortcuts)
- [x] Mutual panel exclusion (catalog ↔ chat)

**Backend:**
- [x] RESTful API with full Swagger docs
- [x] PostgreSQL with 5 tables (users, sessions, events, products, ab_experiments)
- [x] A/B variant assignment (deterministic, consistent)
- [x] Popularity-based recommendations (control)
- [x] AI hybrid recommendations with Pinecone semantic search (treatment)
- [x] LLM chat agent with 4 tools (search, details, recommend, room analysis)
- [x] MLflow experiment tracking
- [x] Event ingestion (single + batch)
- [x] Session duration tracking
- [x] Dashboard API (KPIs, funnel, A/B results)
- [x] Docker containerization (4 services)
- [x] Health checks on all services
- [x] CORS enabled for Quest HTTP requests

**Dashboard:**
- [x] Real-time KPI cards
- [x] Conversion funnel (overall + per-variant)
- [x] A/B test winner detection with p-value
- [x] Product catalog visualization
- [x] Auto-refresh capability

**DevOps:**
- [x] GitHub Actions CI/CD pipeline
- [x] AWS deployment (EC2, RDS, S3, ECR)
- [x] Docker Compose orchestration
- [x] Feature branch Git workflow

---

## 13. Build & Run

### Backend
```bash
cd backend/
docker-compose up -d         # Start all services
docker exec shopmr-backend python -m scripts.seed_products    # Seed catalog
docker exec shopmr-backend python -m scripts.index_products   # Index to Pinecone
docker exec shopmr-backend python -m scripts.simulate_users   # Simulate 300 sessions
docker exec shopmr-backend python -m scripts.ab_analysis      # Run A/B analysis
```

### Unity
1. Open `unity/ShopMR-Unity/` in Unity 6000.0.47f1
2. Open scene `Assets/_ShopMR/Scenes/MainMR.unity`
3. Set backend URL in APIClient (default: http://10.181.182.134:8000)
4. Build for Android (Quest 3) or run in Editor with keyboard/mouse

### Access Points
- **API Docs:** http://localhost:8000/docs
- **Dashboard:** http://localhost:8501
- **MLflow:** http://localhost:5050
- **Health Check:** http://localhost:8000/health

---

## 14. Project Cost

| Service | Cost |
|---------|------|
| Unity Personal License | Free |
| Meta Quest 3 Developer Mode | Free |
| AWS (EC2 + RDS + S3 + ECR) | Free Tier |
| Pinecone Starter Plan | Free |
| Groq API | Free Tier |
| All Python libraries | Free / Open Source |
| Docker / Docker Compose | Free |
| GitHub + Actions | Free (public repo) |
| **Total** | **$0** |

---

*Report generated on 2025-01-06. Project status: Active development.*

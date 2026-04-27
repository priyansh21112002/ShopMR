# ShopMR: Agentic AI-Powered E-Commerce Platform on Mixed Reality

---

## Project Overview

ShopMR is a self-built, end-to-end Mixed Reality e-commerce platform where users wear a Meta Quest 3 headset, see their real room through passthrough mode, browse a virtual product catalog, place life-sized 3D furniture in their actual space using hand tracking, receive AI-driven product recommendations, and converse with an intelligent shopping assistant — all powered by a cloud-deployed, Dockerized backend with A/B experimentation, ML experiment tracking, and a live analytics dashboard.

---

## The Problem

Online furniture shopping suffers from a fundamental limitation — users cannot visualize how a product looks, fits, or complements their existing room before purchasing. 2D images and product descriptions fail to convey scale, proportion, and spatial compatibility. This leads to high return rates, low conversion, and poor customer confidence. Traditional recommendation systems rely on purchase history or popularity, ignoring the physical context of the user's space.

---

## The Solution

ShopMR solves this by merging Mixed Reality with AI. The user's real room becomes the showroom. Virtual furniture appears at real-world scale in the user's actual space. An agentic AI assistant understands the room context, answers questions, and suggests products intelligently. Behind the scenes, an A/B testing engine measures whether AI-powered recommendations outperform simple popularity-based ones, with full statistical rigor.

---

## Detailed Idea

### Part 1: The Mixed Reality Shopping Experience

The user puts on a Meta Quest 3 headset. The app launches in Unity. Passthrough mode activates, showing the user's real room through the headset's cameras. The Meta Scene API scans the physical environment and detects the floor plane, walls, and major surfaces like tables. These real-world surfaces become invisible collision boundaries in the virtual scene so that virtual furniture respects real-world geometry — a virtual sofa cannot float through a real table.

A floating product catalog panel appears in front of the user at eye level. It displays a scrollable grid of 8–10 furniture items — sofas, dining tables, bookshelves, lamps, coffee tables, rugs, chairs, and TV stands. Each product is a 3D model sourced from free asset libraries. Each model carries metadata: product ID, category, color, style, price in INR, and physical dimensions (width, height, depth).

The user interacts using Meta Quest 3's hand tracking. They pinch-grab a product from the catalog, and a life-sized clone spawns and follows their hand. They walk around the room, position the furniture wherever they want, and release to place it. Two-hand pinch allows scaling. A fist gesture near a placed object removes it. Every single interaction — every view, grab, placement, removal, scaling — is logged as a timestamped event containing the event type, product ID, A/B group assignment, spatial coordinates (x, y, z), dwell time in milliseconds, and a list of nearby already-placed products within a 2-meter radius.

Events are batched locally inside Unity and sent to the cloud backend every 10 events or every 5 seconds, whichever comes first, via REST API calls using Unity's HTTP client.

A chat panel floats on the left side of the user's view. The user can type using Quest 3's virtual keyboard or use voice-to-text. Messages are sent to the backend's chat endpoint. The AI assistant's response streams back and appears in the chat panel. The assistant can also proactively suggest products based on what's currently in the room.

When the app starts, it calls the backend to register a new session. The backend assigns the user to an A/B test group using a deterministic hash of their user ID. Based on the assigned group, Unity configures the experience — Group A sees popularity-based recommendations with no AI assistant, Group B sees AI-hybrid recommendations with the full agentic assistant enabled.

---

### Part 2: The Cloud Backend

The backend is a FastAPI application written in Python. It exposes five core REST API endpoints:

1. **Session Start** — receives a user ID, assigns an A/B group via deterministic hashing, creates a session record in the database, and returns the session ID and assigned group to Unity.

2. **Event Ingestion** — receives batched interaction events from Unity, validates each event against a Pydantic schema, and inserts them into the database.

3. **Recommendations** — receives the current session ID and a list of products already placed in the room, routes to the appropriate recommendation engine based on A/B group, and returns a ranked list of 5 recommended products.

4. **Chat** — receives a user message and session ID, passes it to the LangChain agentic assistant, and returns the assistant's response.

5. **Dashboard Metrics** — returns aggregated analytics (funnel metrics, conversion rates, A/B group comparisons) for the Streamlit dashboard to consume.

All request and response data is validated using Pydantic schemas to enforce type safety and prevent bad data from entering the system.

The database is PostgreSQL hosted on AWS RDS. It contains four tables:
- **Users** — user IDs and creation timestamps.
- **Sessions** — session ID, linked user, assigned A/B group, start and end timestamps, total event count.
- **Events** — every interaction event with session reference, event type, product ID, A/B group tag, timestamp, dwell time, spatial coordinates (x, y, z), and list of nearby products.
- **Products** — the product catalog with name, category, color, style, price, dimensions, description, and embedding index status.

Database interactions from FastAPI use SQLAlchemy as the ORM layer. The application server is Uvicorn, running the FastAPI app asynchronously.

The entire backend is containerized using Docker. A Dockerfile defines the Python environment, installs all dependencies, copies the application code, and sets Uvicorn as the entry command. Docker Compose orchestrates four containers together:
1. **api** — the FastAPI application on port 8000
2. **db** — PostgreSQL on port 5432
3. **dashboard** — Streamlit on port 8501
4. **mlflow** — MLflow tracking server on port 5000

All containers share a Docker network so they can communicate by service name. A single `docker-compose up` command starts the entire stack.

The backend is deployed on an AWS EC2 instance. Raw event dumps and model artifacts are stored in AWS S3. Docker images are pushed to AWS ECR (Elastic Container Registry).

Deployment is fully automated via a GitHub Actions CI/CD pipeline. On every push to the `main` branch, the pipeline:
1. Lints the code
2. Runs all pytest test cases
3. Builds the Docker image
4. Pushes the image to AWS ECR
5. SSHs into the EC2 instance
6. Pulls the latest image from ECR
7. Restarts the Docker Compose stack

The codebase follows a feature branch Git workflow. Each feature (Unity MR scene, FastAPI backend, recommendation engine, LangChain agent, A/B testing, AWS deployment, dashboard) is developed on a separate branch, pushed with meaningful commits, submitted as a Pull Request to `main`, and merged after review. This creates a clean, professional Git history.

---

### Part 3: The AI Recommendation Engine

When a user places a product in their room and the app requests recommendations, the engine behaves differently based on the A/B group.

**Group A (Popularity Baseline):** The engine queries PostgreSQL for the most frequently placed products across all sessions, excludes products already in the user's room, and returns the top 5. No intelligence, no personalization — pure global popularity.

**Group B (AI Hybrid):** The engine follows a multi-step process:

**Step 1 — Product Embeddings:** Each product's metadata (category, color, style, name, description, price) is concatenated into a text string and converted into a 384-dimensional vector using the Sentence Transformers MiniLM model. All product vectors are indexed in Pinecone (vector database) with their metadata.

**Step 2 — Contextual Query:** The engine retrieves the embedding vectors for all products currently placed in the user's room and computes their average vector. This average represents the "style profile" of the user's current room.

**Step 3 — Vector Similarity Search:** The average vector is sent to Pinecone with a filter to exclude categories already present in the room (if the user has a sofa, don't recommend another sofa). Pinecone returns the top 10 most similar products by cosine similarity.

**Step 4 — Hybrid Scoring:** Each candidate product is scored using a weighted combination of three signals:
- Cosine similarity score from Pinecone (50% weight) — how stylistically compatible is this product.
- Room fit score (30% weight) — whether the product's physical dimensions fit in the available room space.
- Popularity score (20% weight) — how frequently other users have placed this product.

**Step 5 — Ranking and Return:** Candidates are sorted by final hybrid score and the top 5 are returned to Unity for display.

Every recommendation run is logged to MLflow — the embedding model used, scoring weights, Pinecone top-k parameter, final top-k, hit rate, mean reciprocal rank, and average inference time in milliseconds. MLflow's UI allows comparison across different model versions and configurations.

---

### Part 4: The LangChain Agentic Shopping Assistant

The chat endpoint is powered by a LangChain agent connected to LLaMA-3.1-70B hosted on Groq (free API). The agent is not a simple chatbot — it is an autonomous agent with access to four custom tools:

**Tool 1 — Product Search:** When the user asks to find specific items ("show me wooden tables"), the tool encodes the query using MiniLM, queries Pinecone for semantically similar products, and returns matching results with names, colors, styles, and prices.

**Tool 2 — Room Context Analyzer:** When the user asks "what should I add?", the tool retrieves all products currently placed in the room, identifies which furniture categories are missing (e.g., user has a sofa and lamp but no coffee table, rug, or bookshelf), and suggests what would complete the room.

**Tool 3 — Budget Filter:** When the user mentions price or budget ("something under 5000 rupees"), the tool queries PostgreSQL for products in the relevant category under the specified price and returns options.

**Tool 4 — Room Fit Checker:** When the user asks "will this fit next to my sofa?", the tool retrieves the product's dimensions, calculates available space at the specified position using room scan data, and confirms whether the product fits or suggests a smaller alternative.

The agent uses LangChain's ConversationBufferMemory to maintain multi-turn conversation context within a session. The user can have a natural back-and-forth dialogue:

> User: "What should I add to my room?"
> Agent: (calls Room Context Analyzer) → "You have a sofa and a lamp. A coffee table, rug, and bookshelf would complete the look."
> User: "Show me coffee tables under 5000"
> Agent: (calls Budget Filter) → "Here are your options: Compact Oak Table ₹3,499, Minimal Glass Table ₹4,299..."
> User: "Will the oak one fit next to my sofa?"
> Agent: (calls Room Fit Checker) → "Yes, it's 0.6m wide and there's 0.9m of space there. It fits perfectly."

The LLM decides autonomously which tool to invoke, what parameters to pass, how to interpret the tool's output, and how to format the final response. The agent is served as a separate Docker container called internally by FastAPI.

---

### Part 5: The A/B Testing Engine

The platform runs a controlled experiment to measure whether AI-hybrid recommendations outperform popularity-based recommendations.

**Experiment Design:**
- **Independent Variable:** Recommendation strategy (Group A: popularity-only vs Group B: AI-hybrid with agentic assistant)
- **Dependent Variables:** Conversion rate (did the user place at least one recommended product), click-through rate on recommendations, average session duration, average dwell time per product, funnel progression (browse → view → place → "purchase")

**Assignment:** Users are assigned to groups using a deterministic SHA-256 hash of their user ID concatenated with the experiment name. This ensures the same user always lands in the same group across sessions, eliminating crossover contamination.

**Event Tagging:** Every event in the system carries the `ab_group` field. All downstream analytics (dashboard, statistical tests) can split by group.

**Simulated Users:** Since recruiting hundreds of real users within the project timeline is impractical, a Python simulation script generates 300+ synthetic user sessions. Each simulated user starts a session, receives an A/B assignment, browses a random subset of products with realistic dwell times (Gaussian distribution, mean 3 seconds, std 1 second), and converts (places a recommended product) with a probability that varies by group — approximately 45% for Group A and 65% for Group B. This simulates a realistic scenario where AI recommendations genuinely improve conversion.

**Statistical Analysis:** After simulation, an analysis script pulls all session and event data from PostgreSQL and computes:
- Conversion rate per group
- Lift (percentage improvement of Group B over Group A)
- Proportions z-test for conversion rate significance
- Welch's t-test for dwell time and session duration differences
- p-values and confidence intervals
- Sample size validation via power analysis

**Expected Results:** Approximately 43% conversion lift for AI-hybrid over popularity baseline, with p-value < 0.01, confirming statistical significance.

---

### Part 6: The Analytics Dashboard

A Streamlit web application connects directly to the PostgreSQL database and renders four analytics views:

**View 1 — KPI Cards:** Total sessions, total events processed, overall conversion rate, and average dwell time displayed as headline metrics at the top of the dashboard.

**View 2 — User Funnel:** A Plotly funnel chart showing drop-off at each stage: all sessions → sessions with product views → sessions with product placements → sessions with "purchases." This reveals where users disengage.

**View 3 — A/B Test Results:** Side-by-side bar charts comparing Group A vs Group B on conversion rate, average dwell time, and session duration. Accompanied by a text summary showing lift percentage, p-value, and whether the result is statistically significant.

**View 4 — 3D Spatial Heatmap:** A Plotly 3D scatter plot showing the spatial coordinates (x, y, z) where users place products in their rooms, color-coded by product category. This reveals placement patterns — for example, most users place sofas along walls and coffee tables in the center.

**View 5 — Popular Products:** A bar chart ranking products by total placements, helping identify top-performing inventory.

The MLflow UI is also accessible as a separate web interface, showing all recommendation model experiments — parameters, metrics, and artifacts for each version.

---

### Part 7: End-to-End Data Flow

1. User opens app on Quest 3 → Unity calls backend → session created with A/B assignment → stored in PostgreSQL
2. User browses products → interaction events batched in Unity → sent to backend → validated by Pydantic → stored in PostgreSQL
3. User places a product → Unity requests recommendations → backend routes to popularity engine (Group A) or AI-hybrid engine (Group B) → Pinecone queried for vector similarity → results scored and ranked → logged in MLflow → returned to Unity → displayed in MR
4. User chats with assistant → message sent to backend → LangChain agent receives message → LLM on Groq reasons about which tool to use → tool executes (Pinecone search / PostgreSQL query / spatial calculation) → LLM formats response → returned to Unity → displayed in chat panel
5. User ends session → session closed in PostgreSQL → data available for dashboard
6. Streamlit dashboard reads PostgreSQL → renders funnels, A/B results, heatmaps, KPIs
7. MLflow UI shows model experiment history and comparison
8. Developer pushes code → GitHub Actions runs lint, tests, Docker build → pushes to ECR → deploys to EC2 → containers restart automatically

---

### Part 8: Complete Technology Stack and Roles

| Technology | Role in ShopMR |
|---|---|
| **Unity** | MR application development — scene rendering, interactions, UI, game loop |
| **C#** | Scripting language for all Unity logic — event logging, REST client, product placement, catalog management |
| **Meta Quest 3** | Hardware — MR headset with passthrough, hand tracking, spatial scanning |
| **Meta XR SDK** | Unity plugin — passthrough access, Scene API for room scanning, hand tracking, spatial anchors |
| **XR Interaction Toolkit** | Unity package — grab, poke, select interactions without custom code |
| **FastAPI** | Backend web framework — REST API endpoints, request routing, async handling |
| **Pydantic** | Data validation — enforces type safety on all API requests and responses |
| **Uvicorn** | ASGI server — runs the FastAPI application and handles HTTP connections |
| **PostgreSQL** | Relational database — stores users, sessions, events, and product catalog |
| **SQLAlchemy** | ORM — Python interface to PostgreSQL without raw SQL |
| **AWS EC2** | Cloud compute — hosts the Docker Compose stack with a public IP |
| **AWS RDS** | Managed database — hosts PostgreSQL with automatic backups |
| **AWS S3** | Object storage — raw event dumps, model artifacts |
| **AWS ECR** | Container registry — stores Docker images for deployment |
| **Docker** | Containerization — packages each service with all dependencies |
| **Docker Compose** | Multi-container orchestration — runs API, database, dashboard, MLflow together |
| **GitHub Actions** | CI/CD — automated lint, test, build, push, deploy on every push to main |
| **Git + GitHub** | Version control — feature branch workflow, Pull Requests, commit history |
| **Sentence Transformers (MiniLM)** | Embedding model — converts product text into 384-dimensional vectors |
| **Pinecone** | Vector database — stores product embeddings, performs cosine similarity search |
| **LangChain** | LLM application framework — agent orchestration, tool management, conversation memory |
| **Groq API** | LLM inference provider — runs LLaMA-3.1-70B for the shopping assistant |
| **Scikit-learn** | ML library — clustering, preprocessing, evaluation metrics |
| **XGBoost** | ML library — hybrid recommendation scoring model |
| **MLflow** | Experiment tracking — logs model parameters, metrics, artifacts, enables version comparison |
| **SciPy** | Statistical testing — proportions z-test, Welch's t-test for A/B analysis |
| **Streamlit** | Dashboard framework — renders analytics web app from Python |
| **Plotly** | Interactive charting — funnel charts, bar charts, 3D scatter plots |
| **Postman** | API testing — manual endpoint testing during development |
| **pytest** | Unit testing — automated test suite run in CI/CD pipeline |

---

### Part 9: Key Metrics and Expected Outcomes

| Metric | Expected Value |
|---|---|
| API response latency (P95) | < 400ms |
| Recommendation inference time | < 200ms |
| Pinecone query time | < 50ms |
| LLM response time (Groq) | < 2 seconds |
| A/B conversion lift (Group B over A) | ~43% |
| A/B p-value | < 0.01 |
| Simulated sessions | 300+ |
| Total events processed | 5,000+ |
| Docker containers | 4 (API + DB + Dashboard + MLflow) |
| CI/CD pipeline | Fully automated on push to main |
| Product catalog size | 8–10 items |
| Product embedding dimensions | 384 |
| Telemetry channels per event | 8 (event type, product ID, timestamp, dwell time, x, y, z, nearby products) |

---

### Part 10: Project Cost

| Service | Cost |
|---|---|
| Unity Personal License | Free |
| Meta Quest 3 Developer Mode | Free (device already owned) |
| AWS EC2 + RDS + S3 + ECR | Free (AWS Free Tier) |
| Pinecone Starter Plan | Free (no credit card required) |
| Groq API | Free tier |
| All Python libraries | Free and open source |
| Docker and Docker Compose | Free and open source |
| GitHub and GitHub Actions | Free for public repos |
| **Total** | **$0** |

---

### Part 11: Timeline

| Day | Focus | Deliverable |
|---|---|---|
| Day 1–2 | Unity MR scene, room scanning, product placement, hand tracking, event logging, A/B variant loading, chat UI | Fully interactive MR shopping experience on Quest 3 |
| Day 3 | FastAPI skeleton, Pydantic schemas, PostgreSQL tables, Docker Compose, local testing | Dockerized backend accepting requests locally |
| Day 4 | Unity REST client integration with backend — session start, event ingestion, recommendations, chat | End-to-end data flow: Quest 3 ↔ Cloud Backend |
| Day 5 | Product embeddings with MiniLM, Pinecone indexing, hybrid recommendation engine, MLflow logging | AI recommendations live in MR |
| Day 6 | LangChain agent with four tools, Groq LLM connection, conversation memory, FastAPI chat endpoint | Conversational shopping assistant working |
| Day 7 | A/B assignment logic, event tagging, simulation script for 300 users, statistical analysis script | Full A/B experiment with significant results |
| Day 8 | AWS EC2 setup, RDS, S3, ECR, Docker deployment, GitHub Actions CI/CD pipeline | Fully cloud-deployed with automated deployment |
| Day 9 | Streamlit dashboard with KPIs, funnel, A/B results, spatial heatmap, MLflow UI | Live analytics dashboard |
| Day 10 | README, demo video, Git cleanup, PR history, final metrics documentation | Portfolio-ready project |

---

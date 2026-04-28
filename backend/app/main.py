"""
ShopMR — FastAPI Application Entry Point
Configures CORS, lifespan events, routers, and health check.
"""

import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI, Depends
from fastapi.middleware.cors import CORSMiddleware
from sqlalchemy import text

from app.config import get_settings
from app.models.database import engine, get_db, create_all_tables, SessionLocal
from app.models.schemas import HealthResponse
from app.routers import sessions, events, recommendations, chat, dashboard

settings = get_settings()

# ── Logging ──────────────────────────────────────────────────
logging.basicConfig(
    level=logging.DEBUG if settings.DEBUG else logging.INFO,
    format="%(asctime)s | %(levelname)-8s | %(name)s | %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S",
)
logger = logging.getLogger("shopmr")


# ── Lifespan (startup / shutdown) ────────────────────────────
@asynccontextmanager
async def lifespan(app: FastAPI):
    """
    Runs on startup and shutdown.
    - Startup: create DB tables, log config
    - Shutdown: dispose DB engine
    """
    # ── STARTUP ──
    logger.info("=" * 60)
    logger.info(f"  {settings.APP_NAME} v{settings.APP_VERSION} starting up...")
    logger.info("=" * 60)

    # Create all tables (safe — skips existing ones)
    try:
        create_all_tables()
        logger.info("✅ Database tables created / verified")
    except Exception as e:
        logger.error(f"❌ Database table creation failed: {e}")

    # Verify DB connection
    try:
        db = SessionLocal()
        db.execute(text("SELECT 1"))
        db.close()
        logger.info("✅ Database connection verified")
    except Exception as e:
        logger.error(f"❌ Database connection failed: {e}")

    logger.info(f"📊 A/B Experiment: {settings.AB_DEFAULT_EXPERIMENT}")
    logger.info(f"🤖 LLM Model: {settings.GROQ_MODEL}")
    logger.info(f"🔢 Embedding: {settings.EMBEDDING_MODEL} ({settings.EMBEDDING_DIMENSION}d)")
    logger.info(f"🎯 Pinecone Index: {settings.PINECONE_INDEX_NAME}")
    logger.info("🚀 Ready to serve requests!")
    logger.info("=" * 60)

    yield  # ← App runs here

    # ── SHUTDOWN ──
    logger.info("Shutting down ShopMR...")
    engine.dispose()
    logger.info("Database connections closed. Goodbye! 👋")


# ── FastAPI App ──────────────────────────────────────────────
app = FastAPI(
    title=settings.APP_NAME,
    description=(
        "Agentic AI-Powered E-Commerce Platform on Mixed Reality. "
        "Browse furniture in your real room using Meta Quest 3, "
        "get AI recommendations, and chat with an intelligent assistant."
    ),
    version=settings.APP_VERSION,
    lifespan=lifespan,
    docs_url="/docs",       # Swagger UI
    redoc_url="/redoc",     # ReDoc
    openapi_url="/openapi.json",
)


# ── CORS Middleware ──────────────────────────────────────────
# Quest 3 (Unity) makes HTTP requests from the headset.
# In dev, we allow all origins. In prod, lock this down.
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # TODO: restrict in production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# ── Routers ──────────────────────────────────────────────────
app.include_router(
    sessions.router,
    prefix="/api/sessions",
    tags=["Sessions"],
)

app.include_router(
    events.router,
    prefix="/api/events",
    tags=["Events"],
)

app.include_router(
    recommendations.router,
    prefix="/api/recommendations",
    tags=["Recommendations"],
)

app.include_router(
    chat.router,
    prefix="/api/chat",
    tags=["Chat"],
)

app.include_router(
    dashboard.router,
    prefix="/api/dashboard",
    tags=["Dashboard"],
)


# ── Health Check ─────────────────────────────────────────────
@app.get(
    "/health",
    response_model=HealthResponse,
    tags=["System"],
    summary="Health check endpoint",
)
def health_check():
    """
    Returns app status. Used by Docker HEALTHCHECK and monitoring.
    """
    db_status = "connected"
    try:
        db = SessionLocal()
        db.execute(text("SELECT 1"))
        db.close()
    except Exception:
        db_status = "disconnected"

    return HealthResponse(
        status="healthy" if db_status == "connected" else "degraded",
        app_name=settings.APP_NAME,
        version=settings.APP_VERSION,
        database=db_status,
    )


# ── Root ─────────────────────────────────────────────────────
@app.get("/", tags=["System"])
def root():
    """Welcome endpoint."""
    return {
        "app": settings.APP_NAME,
        "version": settings.APP_VERSION,
        "docs": "/docs",
        "health": "/health",
    }
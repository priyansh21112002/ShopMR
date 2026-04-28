"""
ShopMR — Centralized Configuration
Loads all environment variables from .env and exposes them as typed settings.
"""

from functools import lru_cache
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """
    Application settings loaded from environment variables.
    Pydantic will auto-read from the .env file specified in model_config.
    """

    # ── App ──────────────────────────────────────────────
    APP_NAME: str = "ShopMR"
    APP_VERSION: str = "0.1.0"
    DEBUG: bool = True

    # ── Database (PostgreSQL) ────────────────────────────
    DATABASE_URL: str = "postgresql://shopmr_user:shopmr_pass@db:5432/shopmr"

    # ── Pinecone (Vector DB) ─────────────────────────────
    PINECONE_API_KEY: str = ""
    PINECONE_INDEX_NAME: str = "shopmr-products"

    # ── Groq (LLM) ──────────────────────────────────────
    GROQ_API_KEY: str = ""
    GROQ_MODEL: str = "llama-3.3-70b-versatile"

    # ── AWS ──────────────────────────────────────────────
    AWS_ACCESS_KEY_ID: str = ""
    AWS_SECRET_ACCESS_KEY: str = ""
    AWS_REGION: str = "ap-south-1"
    S3_BUCKET_NAME: str = "shopmr-artifacts"

    # ── MLflow ───────────────────────────────────────────
    MLFLOW_TRACKING_URI: str = "http://mlflow:5000"

    # ── A/B Testing ──────────────────────────────────────
    AB_DEFAULT_EXPERIMENT: str = "reco_engine_v1"
    AB_VARIANTS: list[str] = ["control", "treatment"]

    # ── Embedding Model ──────────────────────────────────
    EMBEDDING_MODEL: str = "all-MiniLM-L6-v2"
    EMBEDDING_DIMENSION: int = 384

    model_config = {
        "env_file": ".env",
        "env_file_encoding": "utf-8",
        "case_sensitive": True,
    }


@lru_cache()
def get_settings() -> Settings:
    """
    Cached settings instance.
    Called once, reused everywhere — avoids re-reading .env on every request.
    """
    return Settings()
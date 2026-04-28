"""
ShopMR — A/B Testing Engine
Deterministic variant assignment using consistent hashing.
Ensures the same user always gets the same variant.
"""

import hashlib
import logging

from sqlalchemy.orm import Session as DBSession

from app.config import get_settings
from app.models.database import ABExperiment

settings = get_settings()
logger = logging.getLogger("shopmr.ab_engine")


def get_or_create_experiment(db: DBSession) -> ABExperiment:
    """
    Get the default A/B experiment, or create it if it doesn't exist.
    Called on first session start.
    """
    experiment = db.query(ABExperiment).filter(
        ABExperiment.name == settings.AB_DEFAULT_EXPERIMENT,
        ABExperiment.is_active == True,
    ).first()

    if not experiment:
        experiment = ABExperiment(
            name=settings.AB_DEFAULT_EXPERIMENT,
            description="Recommendation engine A/B test: popularity vs AI hybrid",
            variants={
                "control": "Popularity-based recommendations",
                "treatment": "AI-powered hybrid recommendations (collaborative + semantic)",
            },
            is_active=True,
        )
        db.add(experiment)
        db.commit()
        db.refresh(experiment)
        logger.info(f"✅ Created A/B experiment: {experiment.name}")

    return experiment


def assign_variant(user_id: str, experiment_name: str = None) -> str:
    """
    Deterministically assign a user to a variant using consistent hashing.

    How it works:
    1. Hash the user_id + experiment_name
    2. Convert to integer
    3. Modulo by number of variants
    4. Map to variant name

    This ensures:
    - Same user ALWAYS gets the same variant (deterministic)
    - ~50/50 split across all users (uniform distribution)
    - No database lookup needed for assignment
    """
    experiment_name = experiment_name or settings.AB_DEFAULT_EXPERIMENT
    variants = settings.AB_VARIANTS  # ["control", "treatment"]

    # Create a consistent hash
    hash_input = f"{user_id}:{experiment_name}"
    hash_value = hashlib.sha256(hash_input.encode()).hexdigest()

    # Convert first 8 hex chars to integer, mod by variant count
    bucket = int(hash_value[:8], 16) % len(variants)

    variant = variants[bucket]
    logger.debug(f"User {user_id} → variant '{variant}' (bucket {bucket})")

    return variant


def get_variant_distribution(db: DBSession) -> dict:
    """
    Get the current distribution of users across variants.
    Used by dashboard to verify even split.
    """
    from app.models.database import Session as SessionModel

    results = db.query(
        SessionModel.variant,
        db.query(SessionModel).filter(
            SessionModel.variant == SessionModel.variant
        ).count(),
    ).group_by(SessionModel.variant).all()

    # Simpler approach
    from sqlalchemy import func
    distribution = db.query(
        SessionModel.variant,
        func.count(SessionModel.session_id).label("count")
    ).group_by(SessionModel.variant).all()

    return {row.variant: row.count for row in distribution}
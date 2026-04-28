"""
ShopMR — Recommendation Engine
Hybrid recommendation system with A/B variants:
  - Control: Popularity-based (view count + purchase count)
  - Treatment: AI-powered (semantic similarity + user behavior + popularity)
"""

import logging
from typing import Optional

import mlflow
from sqlalchemy import func
from sqlalchemy.orm import Session as DBSession

from app.config import get_settings
from app.models.database import (
    Event,
    Product,
    Session as SessionModel,
)
from app.models.schemas import RecommendedProduct
from app.services.pinecone_client import (
    query_similar_products,
    query_similar_to_product,
)

settings = get_settings()
logger = logging.getLogger("shopmr.reco_engine")


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  CONTROL: Popularity-Based Recommendations
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

def get_popularity_scores(db: DBSession) -> dict[str, float]:
    """
    Calculate popularity scores for all products based on event counts.
    Score = (views * 1) + (placements * 3) + (purchases * 5)
    """
    # Count events by product and type
    event_counts = db.query(
        Event.product_id,
        Event.event_type,
        func.count(Event.event_id).label("count"),
    ).filter(
        Event.product_id.isnot(None),
    ).group_by(
        Event.product_id, Event.event_type
    ).all()

    # Weight events
    weights = {
        "view": 1.0,
        "place": 3.0,
        "rotate": 1.5,
        "scale": 1.5,
        "purchase": 5.0,
        "recommend_click": 2.0,
    }

    scores = {}
    for product_id, event_type, count in event_counts:
        weight = weights.get(event_type, 1.0)
        scores[product_id] = scores.get(product_id, 0) + (count * weight)

    # Normalize to 0-1 range
    if scores:
        max_score = max(scores.values())
        if max_score > 0:
            scores = {k: round(v / max_score, 4) for k, v in scores.items()}

    return scores


def recommend_control(
    db: DBSession,
    session_id: str,
    current_product_id: Optional[str] = None,
    limit: int = 6,
) -> list[RecommendedProduct]:
    """
    CONTROL variant: Popularity-based recommendations.
    Returns most popular products, excluding the current one.
    """
    popularity = get_popularity_scores(db)

    # Get all active products
    products = db.query(Product).filter(Product.is_active == True).all()

    # Score and rank
    scored = []
    for product in products:
        if product.product_id == current_product_id:
            continue
        score = popularity.get(product.product_id, 0.1)  # Default 0.1 for unseen
        scored.append((product, score))

    # Sort by popularity descending
    scored.sort(key=lambda x: x[1], reverse=True)

    # Build response
    recommendations = []
    for product, score in scored[:limit]:
        recommendations.append(
            RecommendedProduct(
                product_id=product.product_id,
                name=product.name,
                category=product.category,
                price=product.price,
                thumbnail_url=product.thumbnail_url,
                score=score,
                reason="Popular among other shoppers",
            )
        )

    logger.info(f"📊 Control reco: {len(recommendations)} products (popularity-based)")
    return recommendations


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  TREATMENT: AI-Powered Hybrid Recommendations
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

def get_user_behavior_text(db: DBSession, session_id: str) -> str:
    """
    Build a text representation of the user's behavior in this session.
    Used as the semantic query for personalized recommendations.
    """
    # Get products the user has interacted with
    events = db.query(Event).filter(
        Event.session_id == session_id,
        Event.product_id.isnot(None),
    ).order_by(Event.timestamp.desc()).limit(10).all()

    if not events:
        return "popular furniture for home decoration"

    # Collect product details
    product_ids = list(set(e.product_id for e in events))
    products = db.query(Product).filter(
        Product.product_id.in_(product_ids)
    ).all()

    product_map = {p.product_id: p for p in products}

    # Build behavior text
    parts = []
    categories = set()
    tags_seen = set()

    for event in events:
        product = product_map.get(event.product_id)
        if not product:
            continue

        categories.add(product.category)
        if product.tags:
            tags_seen.update(product.tags)

        if event.event_type == "purchase":
            parts.append(f"Purchased: {product.name}")
        elif event.event_type == "place":
            parts.append(f"Placed in room: {product.name}")
        elif event.event_type == "view":
            parts.append(f"Viewed: {product.name}")

    behavior_text = " | ".join(parts)

    if categories:
        behavior_text += f" | Interested in: {', '.join(categories)}"
    if tags_seen:
        behavior_text += f" | Style preferences: {', '.join(list(tags_seen)[:8])}"

    return behavior_text


def recommend_treatment(
    db: DBSession,
    session_id: str,
    current_product_id: Optional[str] = None,
    room_context: Optional[dict] = None,
    limit: int = 6,
) -> list[RecommendedProduct]:
    """
    TREATMENT variant: AI-powered hybrid recommendations.
    Combines:
    1. Semantic similarity (via Pinecone) — 60% weight
    2. User behavior matching — 20% weight
    3. Popularity — 20% weight
    """
    popularity = get_popularity_scores(db)

    # ── Semantic search based on user behavior ──
    behavior_text = get_user_behavior_text(db, session_id)
    exclude_ids = [current_product_id] if current_product_id else []

    try:
        semantic_results = query_similar_products(
            query_text=behavior_text,
            top_k=limit * 2,
            exclude_ids=exclude_ids,
        )
    except Exception as e:
        logger.error(f"❌ Pinecone query failed: {e}. Falling back to popularity.")
        return recommend_control(db, session_id, current_product_id, limit)

    # ── "More like this" if viewing a product ──
    similar_to_current = []
    if current_product_id:
        try:
            similar_to_current = query_similar_to_product(
                product_id=current_product_id,
                top_k=limit,
            )
        except Exception as e:
            logger.warning(f"⚠️ Similar product query failed: {e}")

    # ── Combine scores ──
    # Collect all candidate product IDs
    all_candidates = {}

    # Semantic results (weight: 0.6)
    for result in semantic_results:
        pid = result["product_id"]
        all_candidates[pid] = {
            "semantic_score": result["score"] * 0.6,
            "similar_score": 0,
            "popularity_score": 0,
            "metadata": result["metadata"],
        }

    # Similar to current (weight: 0.2)
    for result in similar_to_current:
        pid = result["product_id"]
        if pid in all_candidates:
            all_candidates[pid]["similar_score"] = result["score"] * 0.2
        else:
            all_candidates[pid] = {
                "semantic_score": 0,
                "similar_score": result["score"] * 0.2,
                "popularity_score": 0,
                "metadata": result["metadata"],
            }

    # Popularity (weight: 0.2)
    for pid in all_candidates:
        all_candidates[pid]["popularity_score"] = popularity.get(pid, 0.1) * 0.2

    # Calculate final scores
    scored_products = []
    for pid, scores in all_candidates.items():
        if pid == current_product_id:
            continue
        total_score = (
            scores["semantic_score"]
            + scores["similar_score"]
            + scores["popularity_score"]
        )
        scored_products.append((pid, total_score, scores))

    # Sort by final score
    scored_products.sort(key=lambda x: x[1], reverse=True)

    # Get product details from DB
    top_ids = [pid for pid, _, _ in scored_products[:limit]]
    products = db.query(Product).filter(
        Product.product_id.in_(top_ids)
    ).all()
    product_map = {p.product_id: p for p in products}

    # Build response
    recommendations = []
    for pid, total_score, scores in scored_products[:limit]:
        product = product_map.get(pid)
        if not product:
            continue

        # Generate reason based on dominant signal
        reason = _generate_reason(scores, product)

        recommendations.append(
            RecommendedProduct(
                product_id=product.product_id,
                name=product.name,
                category=product.category,
                price=product.price,
                thumbnail_url=product.thumbnail_url,
                score=round(total_score, 4),
                reason=reason,
            )
        )

    logger.info(
        f"🤖 Treatment reco: {len(recommendations)} products "
        f"(hybrid: semantic + behavior + popularity)"
    )
    return recommendations


def _generate_reason(scores: dict, product) -> str:
    """Generate a human-readable reason for the recommendation."""
    dominant = max(
        [
            ("semantic_score", scores["semantic_score"]),
            ("similar_score", scores["similar_score"]),
            ("popularity_score", scores["popularity_score"]),
        ],
        key=lambda x: x[1],
    )

    if dominant[0] == "semantic_score":
        return f"Matches your style preferences — {product.category} you might love"
    elif dominant[0] == "similar_score":
        return f"Similar to what you're viewing — great {product.category} alternative"
    else:
        return f"Trending {product.category} — popular among shoppers"


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  MAIN ENTRY POINT
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

def get_recommendations(
    db: DBSession,
    session_id: str,
    variant: str,
    current_product_id: Optional[str] = None,
    room_context: Optional[dict] = None,
    limit: int = 6,
) -> tuple[list[RecommendedProduct], Optional[str]]:
    """
    Main recommendation function. Routes to control or treatment based on variant.
    Returns (recommendations, mlflow_run_id).
    """
    mlflow_run_id = None

    try:
        # Log to MLflow
        mlflow.set_tracking_uri(settings.MLFLOW_TRACKING_URI)
        mlflow.set_experiment(settings.AB_DEFAULT_EXPERIMENT)

        with mlflow.start_run(nested=True) as run:
            mlflow.log_params({
                "variant": variant,
                "session_id": session_id,
                "current_product_id": current_product_id or "none",
                "limit": limit,
            })

            if variant == "treatment":
                recommendations = recommend_treatment(
                    db, session_id, current_product_id, room_context, limit
                )
            else:
                recommendations = recommend_control(
                    db, session_id, current_product_id, limit
                )

            # Log metrics
            mlflow.log_metrics({
                "num_recommendations": len(recommendations),
                "avg_score": (
                    sum(r.score for r in recommendations) / len(recommendations)
                    if recommendations else 0
                ),
            })

            mlflow_run_id = run.info.run_id

    except Exception as e:
        logger.warning(f"⚠️ MLflow logging failed: {e}. Continuing without tracking.")

        if variant == "treatment":
            recommendations = recommend_treatment(
                db, session_id, current_product_id, room_context, limit
            )
        else:
            recommendations = recommend_control(
                db, session_id, current_product_id, limit
            )

    return recommendations, mlflow_run_id
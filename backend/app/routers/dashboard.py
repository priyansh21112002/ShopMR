"""
Dashboard router - real-time analytics endpoints.
Queries PostgreSQL directly with SQLAlchemy + raw SQL for performance.
"""

from typing import Optional
from datetime import datetime, timezone

import numpy as np
from fastapi import APIRouter, Depends, HTTPException, Query
from scipy import stats
from sqlalchemy import text
from sqlalchemy.orm import Session

from app.models.database import get_db
from app.models.schemas import (
    DashboardKPIs,
    FunnelStep,
    FunnelResponse,
    ABResultMetric,
    ABResultResponse,
)

router = APIRouter(tags=["dashboard"])

# ---------- Helpers ----------

def _scalar(db: Session, sql: str, params: dict = None):
    """Run a single-value scalar query."""
    row = db.execute(text(sql), params or {}).first()
    return row[0] if row else None


def _two_proportion_ztest(x1: int, n1: int, x2: int, n2: int) -> tuple[float, float]:
    """Returns (z, p_value). Returns (0, 1) for degenerate cases."""
    if n1 == 0 or n2 == 0:
        return 0.0, 1.0
    p1 = x1 / n1
    p2 = x2 / n2
    p_pool = (x1 + x2) / (n1 + n2)
    se = float(np.sqrt(p_pool * (1 - p_pool) * (1 / n1 + 1 / n2)))
    if se == 0:
        return 0.0, 1.0
    z = (p2 - p1) / se
    p = 2 * stats.norm.sf(abs(z))
    return float(z), float(p)


# ---------- /metrics ----------

@router.get("/metrics", response_model=DashboardKPIs)
def get_metrics(db: Session = Depends(get_db)):
    """
    Top-level KPIs across all sessions, events, and users.
    """
    total_sessions = _scalar(db, "SELECT COUNT(*) FROM sessions") or 0
    active_sessions = _scalar(db, "SELECT COUNT(*) FROM sessions WHERE is_active = true") or 0
    total_events = _scalar(db, "SELECT COUNT(*) FROM events") or 0
    total_users = _scalar(db, "SELECT COUNT(*) FROM users") or 0

    avg_duration = _scalar(db, """
        SELECT COALESCE(AVG(EXTRACT(EPOCH FROM (ended_at - started_at))), 0)
        FROM sessions
        WHERE ended_at IS NOT NULL
    """)
    avg_duration = float(avg_duration) if avg_duration else None

    total_purchases = _scalar(
        db, "SELECT COUNT(*) FROM events WHERE event_type = 'purchase'"
    ) or 0

    # Revenue: sum the price of products that were purchased
    total_revenue = _scalar(db, """
        SELECT COALESCE(SUM(p.price), 0)
        FROM events e
        JOIN products p ON p.product_id = e.product_id
        WHERE e.event_type = 'purchase'
    """) or 0
    total_revenue = float(total_revenue)

    return DashboardKPIs(
        total_sessions=total_sessions,
        active_sessions=active_sessions,
        total_events=total_events,
        total_users=total_users,
        avg_session_duration_seconds=avg_duration,
        total_purchases=total_purchases,
        total_revenue=total_revenue,
    )


# ---------- /funnel ----------

@router.get("/funnel", response_model=FunnelResponse)
def get_funnel(
    variant: Optional[str] = Query(None, description="Filter by variant: control or treatment"),
    db: Session = Depends(get_db),
):
    """
    Conversion funnel: session_start -> view -> recommend_click -> place -> purchase.
    Each step shows count + retention rate (relative to step 1).
    """
    if variant and variant not in ("control", "treatment"):
        raise HTTPException(status_code=400, detail="variant must be 'control' or 'treatment'")

    # Sessions count (top of funnel)
    if variant:
        sessions = _scalar(
            db, "SELECT COUNT(*) FROM sessions WHERE variant = :v",
            {"v": variant},
        ) or 0
    else:
        sessions = _scalar(db, "SELECT COUNT(*) FROM sessions") or 0

    # Distinct sessions that fired each event type
    def step_count(event_type: str) -> int:
        if variant:
            sql = """
                SELECT COUNT(DISTINCT e.session_id)
                FROM events e
                JOIN sessions s ON s.session_id = e.session_id
                WHERE e.event_type = :et AND s.variant = :v
            """
            return _scalar(db, sql, {"et": event_type, "v": variant}) or 0
        sql = "SELECT COUNT(DISTINCT session_id) FROM events WHERE event_type = :et"
        return _scalar(db, sql, {"et": event_type}) or 0

    n_view = step_count("view")
    n_click = step_count("recommend_click")
    n_place = step_count("place")
    n_purch = step_count("purchase")

    base = sessions if sessions > 0 else 1

    funnel = [
        FunnelStep(step="session_start",   count=sessions, rate=1.0),
        FunnelStep(step="view",            count=n_view,   rate=n_view / base),
        FunnelStep(step="recommend_click", count=n_click,  rate=n_click / base),
        FunnelStep(step="place",           count=n_place,  rate=n_place / base),
        FunnelStep(step="purchase",        count=n_purch,  rate=n_purch / base),
    ]

    return FunnelResponse(funnel=funnel, variant=variant)


# ---------- /ab-results ----------

@router.get("/ab-results", response_model=ABResultResponse)
def get_ab_results(
    experiment: str = Query("reco_engine_v1", description="Experiment name"),
    db: Session = Depends(get_db),
):
    """
    A/B test results: per-variant metrics + statistical significance test
    on placement rate (primary KPI).
    """
    metrics = []
    variant_data = {}  # for stat test

    for variant in ("control", "treatment"):
        sessions = _scalar(
            db, "SELECT COUNT(*) FROM sessions WHERE variant = :v",
            {"v": variant},
        ) or 0

        views = _scalar(db, """
            SELECT COUNT(*)
            FROM events e
            JOIN sessions s ON s.session_id = e.session_id
            WHERE s.variant = :v AND e.event_type = 'view'
        """, {"v": variant}) or 0

        places = _scalar(db, """
            SELECT COUNT(*)
            FROM events e
            JOIN sessions s ON s.session_id = e.session_id
            WHERE s.variant = :v AND e.event_type = 'place'
        """, {"v": variant}) or 0

        purchases = _scalar(db, """
            SELECT COUNT(*)
            FROM events e
            JOIN sessions s ON s.session_id = e.session_id
            WHERE s.variant = :v AND e.event_type = 'purchase'
        """, {"v": variant}) or 0

        total_events = _scalar(db, """
            SELECT COUNT(*)
            FROM events e
            JOIN sessions s ON s.session_id = e.session_id
            WHERE s.variant = :v
        """, {"v": variant}) or 0

        avg_duration = _scalar(db, """
            SELECT COALESCE(AVG(EXTRACT(EPOCH FROM (ended_at - started_at))), 0)
            FROM sessions
            WHERE variant = :v AND ended_at IS NOT NULL
        """, {"v": variant})
        avg_duration = float(avg_duration) if avg_duration else None

        # Conversion rate = sessions that purchased / total sessions
        conversion_rate = (purchases / sessions) if sessions > 0 else 0.0
        avg_events = (total_events / sessions) if sessions > 0 else 0.0

        metrics.append(ABResultMetric(
            variant=variant,
            sessions=sessions,
            conversion_rate=conversion_rate,
            avg_session_duration=avg_duration,
            avg_events_per_session=avg_events,
        ))

        variant_data[variant] = {
            "sessions": sessions,
            "views": views,
            "places": places,
            "purchases": purchases,
        }

    # Statistical test on placement rate (places / views)
    p_value = None
    is_significant = False
    winner = None

    cd = variant_data.get("control", {})
    td = variant_data.get("treatment", {})

    if cd and td and cd.get("views", 0) > 0 and td.get("views", 0) > 0:
        z, p = _two_proportion_ztest(
            cd["places"], cd["views"],
            td["places"], td["views"],
        )
        p_value = p
        is_significant = p < 0.05
        if is_significant:
            cp = cd["places"] / cd["views"]
            tp = td["places"] / td["views"]
            winner = "treatment" if tp > cp else "control"

    return ABResultResponse(
        experiment_name=experiment,
        metrics=metrics,
        p_value=p_value,
        is_significant=is_significant,
        winner=winner,
    )
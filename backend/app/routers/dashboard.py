"""
ShopMR — Dashboard Router
Provides metrics for the Streamlit analytics dashboard.
"""

from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session as DBSession

from app.models.database import get_db
from app.models.schemas import DashboardKPIs, FunnelResponse, ABResultResponse

router = APIRouter()


@router.get("/metrics", response_model=DashboardKPIs)
def get_metrics(db: DBSession = Depends(get_db)):
    """
    High-level KPIs for the dashboard.
    """
    # TODO: implement in Day 9
    return DashboardKPIs(
        total_sessions=0,
        active_sessions=0,
        total_events=0,
        total_users=0,
        avg_session_duration_seconds=None,
        total_purchases=0,
        total_revenue=0.0,
    )


@router.get("/funnel", response_model=FunnelResponse)
def get_funnel(variant: str = None, db: DBSession = Depends(get_db)):
    """
    Conversion funnel: view → place → purchase
    Optionally filtered by A/B variant.
    """
    # TODO: implement in Day 9
    return FunnelResponse(funnel=[], variant=variant)


@router.get("/ab-results", response_model=ABResultResponse)
def get_ab_results(db: DBSession = Depends(get_db)):
    """
    A/B test results with statistical significance.
    """
    # TODO: implement in Day 7
    return ABResultResponse(
        experiment_name="reco_engine_v1",
        metrics=[],
        p_value=None,
        is_significant=False,
        winner=None,
    )
"""
ShopMR — Recommendations Router
Serves product recommendations based on A/B variant.
"""

import logging

from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session as DBSession

from app.models.database import get_db, Session as SessionModel
from app.models.schemas import RecommendationRequest, RecommendationResponse
from app.services.reco_engine import get_recommendations

router = APIRouter()
logger = logging.getLogger("shopmr.recommendations")


@router.post("/get", response_model=RecommendationResponse)
def get_reco(request: RecommendationRequest, db: DBSession = Depends(get_db)):
    """
    Get product recommendations for the current session.
    - Control variant: popularity-based
    - Treatment variant: AI-powered hybrid (semantic + behavioral + popularity)
    """
    # Get session to determine variant
    session = db.query(SessionModel).filter(
        SessionModel.session_id == request.session_id,
    ).first()

    if not session:
        raise HTTPException(status_code=404, detail="Session not found")

    # Get recommendations
    recommendations, mlflow_run_id = get_recommendations(
        db=db,
        session_id=request.session_id,
        variant=session.variant,
        current_product_id=request.current_product_id,
        room_context=request.room_context,
        limit=request.limit,
    )

    logger.info(
        f"🎯 Recommendations served: {len(recommendations)} | "
        f"Session: {request.session_id} | Variant: {session.variant}"
    )

    return RecommendationResponse(
        session_id=request.session_id,
        variant=session.variant,
        recommendations=recommendations,
        model_version=mlflow_run_id,
    )
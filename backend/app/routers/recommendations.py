"""
ShopMR — Recommendations Router
Serves product recommendations based on A/B variant.
"""

from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session as DBSession

from app.models.database import get_db
from app.models.schemas import RecommendationRequest, RecommendationResponse

router = APIRouter()


@router.post("/get", response_model=RecommendationResponse)
def get_recommendations(request: RecommendationRequest, db: DBSession = Depends(get_db)):
    """
    Get product recommendations for the current session.
    - Control: popularity-based
    - Treatment: AI hybrid (collaborative + semantic via Pinecone)
    """
    # TODO: implement in Day 5
    return RecommendationResponse(
        session_id=request.session_id,
        variant="control",
        recommendations=[],
        model_version=None,
    )
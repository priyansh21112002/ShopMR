"""
ShopMR — Sessions Router
Handles session start/end and A/B variant assignment.
"""

from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session as DBSession

from app.models.database import get_db
from app.models.schemas import (
    SessionStartRequest,
    SessionStartResponse,
    SessionEndRequest,
    SessionEndResponse,
)

router = APIRouter()


@router.post("/start", response_model=SessionStartResponse)
def start_session(request: SessionStartRequest, db: DBSession = Depends(get_db)):
    """
    Start a new shopping session.
    - Creates or retrieves user
    - Assigns A/B variant
    - Returns session_id + variant to Quest 3
    """
    # TODO: implement in Day 3
    return SessionStartResponse(
        session_id="placeholder-session-id",
        user_id=request.user_id or "placeholder-user-id",
        variant="control",
        message="Session started (placeholder)",
    )


@router.post("/end", response_model=SessionEndResponse)
def end_session(request: SessionEndRequest, db: DBSession = Depends(get_db)):
    """
    End an active session. Records duration.
    """
    # TODO: implement in Day 3
    return SessionEndResponse(
        session_id=request.session_id,
        duration_seconds=0.0,
        message="Session ended (placeholder)",
    )
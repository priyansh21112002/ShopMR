"""
ShopMR — Sessions Router
Handles session start/end and A/B variant assignment.
"""

import logging
from datetime import datetime, timezone

from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session as DBSession

from app.models.database import get_db, User, Session as SessionModel
from app.models.schemas import (
    SessionStartRequest,
    SessionStartResponse,
    SessionEndRequest,
    SessionEndResponse,
)
from app.services.ab_engine import assign_variant, get_or_create_experiment

router = APIRouter()
logger = logging.getLogger("shopmr.sessions")


@router.post("/start", response_model=SessionStartResponse)
def start_session(request: SessionStartRequest, db: DBSession = Depends(get_db)):
    """
    Start a new shopping session.
    1. Create or retrieve user
    2. Ensure A/B experiment exists
    3. Assign variant via consistent hashing
    4. Create session record
    5. Return session_id + variant to Quest 3
    """
    # ── Step 1: Get or create user ──
    user = None

    if request.user_id:
        user = db.query(User).filter(User.user_id == request.user_id).first()

    if not user and request.device_id:
        user = db.query(User).filter(User.device_id == request.device_id).first()

    if not user:
        user = User(device_id=request.device_id)
        db.add(user)
        db.commit()
        db.refresh(user)
        logger.info(f"👤 New user created: {user.user_id}")
    else:
        logger.info(f"👤 Returning user: {user.user_id}")

    # ── Step 2: Ensure A/B experiment exists ──
    get_or_create_experiment(db)

    # ── Step 3: Assign variant ──
    variant = assign_variant(user.user_id)

    # ── Step 4: Create session ──
    session = SessionModel(
        user_id=user.user_id,
        variant=variant,
        is_active=True,
    )
    db.add(session)
    db.commit()
    db.refresh(session)

    logger.info(
        f"🟢 Session started: {session.session_id} | "
        f"User: {user.user_id} | Variant: {variant}"
    )

    return SessionStartResponse(
        session_id=session.session_id,
        user_id=user.user_id,
        variant=variant,
        message="Session started",
    )


@router.post("/end", response_model=SessionEndResponse)
def end_session(request: SessionEndRequest, db: DBSession = Depends(get_db)):
    """
    End an active session. Records end time and calculates duration.
    """
    session = db.query(SessionModel).filter(
        SessionModel.session_id == request.session_id,
    ).first()

    if not session:
        raise HTTPException(status_code=404, detail="Session not found")

    if not session.is_active:
        raise HTTPException(status_code=400, detail="Session already ended")

    # Calculate duration
    now = datetime.now(timezone.utc)
    session.ended_at = now
    session.is_active = False
    db.commit()

    duration = (now - session.started_at).total_seconds()

    logger.info(
        f"🔴 Session ended: {session.session_id} | "
        f"Duration: {duration:.1f}s"
    )

    return SessionEndResponse(
        session_id=session.session_id,
        duration_seconds=round(duration, 2),
        message="Session ended",
    )


@router.get("/active")
def get_active_sessions(db: DBSession = Depends(get_db)):
    """
    List all active sessions. Useful for monitoring.
    """
    sessions = db.query(SessionModel).filter(
        SessionModel.is_active == True
    ).all()

    return {
        "active_sessions": len(sessions),
        "sessions": [
            {
                "session_id": s.session_id,
                "user_id": s.user_id,
                "variant": s.variant,
                "started_at": s.started_at.isoformat(),
            }
            for s in sessions
        ],
    }
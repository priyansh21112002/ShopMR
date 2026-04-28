"""
ShopMR — Events Router
Ingests user interaction events from Quest 3.
"""

import logging

from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session as DBSession

from app.models.database import get_db, Event, Session as SessionModel
from app.models.schemas import (
    EventCreate,
    EventBatchCreate,
    EventResponse,
    EventBatchResponse,
)

router = APIRouter()
logger = logging.getLogger("shopmr.events")

# Valid event types
VALID_EVENT_TYPES = {
    "view",
    "place",
    "rotate",
    "scale",
    "purchase",
    "chat",
    "recommend_click",
}


def validate_and_create_event(event_data: EventCreate, db: DBSession) -> Event:
    """
    Validate an event and create it in the database.
    Shared logic for single and batch endpoints.
    """
    # Validate event type
    if event_data.event_type not in VALID_EVENT_TYPES:
        raise HTTPException(
            status_code=400,
            detail=f"Invalid event_type '{event_data.event_type}'. "
                   f"Must be one of: {', '.join(sorted(VALID_EVENT_TYPES))}",
        )

    # Validate session exists and is active
    session = db.query(SessionModel).filter(
        SessionModel.session_id == event_data.session_id,
    ).first()

    if not session:
        raise HTTPException(status_code=404, detail="Session not found")

    if not session.is_active:
        raise HTTPException(status_code=400, detail="Session is no longer active")

    # Create event
    event = Event(
        session_id=event_data.session_id,
        event_type=event_data.event_type,
        product_id=event_data.product_id,
        metadata_=event_data.metadata,
    )
    return event


@router.post("/track", response_model=EventBatchResponse)
def track_events(batch: EventBatchCreate, db: DBSession = Depends(get_db)):
    """
    Receive a batch of events from Quest 3.
    Unity buffers events and sends them periodically to reduce network calls.

    Event types: view, place, rotate, scale, purchase, chat, recommend_click
    """
    events = []
    for event_data in batch.events:
        event = validate_and_create_event(event_data, db)
        events.append(event)

    db.add_all(events)
    db.commit()

    logger.info(
        f"📥 Batch received: {len(events)} events | "
        f"Session: {batch.events[0].session_id}"
    )

    return EventBatchResponse(
        received=len(events),
        message=f"{len(events)} events recorded",
    )


@router.post("/single", response_model=EventResponse)
def track_single_event(event_data: EventCreate, db: DBSession = Depends(get_db)):
    """
    Track a single event. Used for real-time events like purchases.
    """
    event = validate_and_create_event(event_data, db)
    db.add(event)
    db.commit()
    db.refresh(event)

    logger.info(
        f"📥 Event: {event.event_type} | "
        f"Session: {event.session_id} | "
        f"Product: {event.product_id or 'N/A'}"
    )

    return EventResponse(
        event_id=event.event_id,
        session_id=event.session_id,
        event_type=event.event_type,
        product_id=event.product_id,
        metadata=event.metadata_,
        timestamp=event.timestamp,
    )


@router.get("/session/{session_id}")
def get_session_events(session_id: str, db: DBSession = Depends(get_db)):
    """
    Get all events for a session. Useful for debugging and dashboard.
    """
    session = db.query(SessionModel).filter(
        SessionModel.session_id == session_id,
    ).first()

    if not session:
        raise HTTPException(status_code=404, detail="Session not found")

    events = db.query(Event).filter(
        Event.session_id == session_id
    ).order_by(Event.timestamp).all()

    return {
        "session_id": session_id,
        "variant": session.variant,
        "total_events": len(events),
        "events": [
            {
                "event_id": e.event_id,
                "event_type": e.event_type,
                "product_id": e.product_id,
                "metadata": e.metadata_,
                "timestamp": e.timestamp.isoformat(),
            }
            for e in events
        ],
    }
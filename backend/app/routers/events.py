"""
ShopMR — Events Router
Ingests user interaction events from Quest 3.
"""

from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session as DBSession

from app.models.database import get_db
from app.models.schemas import (
    EventCreate,
    EventBatchCreate,
    EventResponse,
    EventBatchResponse,
)

router = APIRouter()


@router.post("/track", response_model=EventBatchResponse)
def track_events(batch: EventBatchCreate, db: DBSession = Depends(get_db)):
    """
    Receive a batch of events from Quest 3.
    Events: view, place, rotate, scale, purchase, chat, recommend_click
    """
    # TODO: implement in Day 3
    return EventBatchResponse(
        received=len(batch.events),
        message=f"{len(batch.events)} events recorded (placeholder)",
    )


@router.post("/single", response_model=EventResponse)
def track_single_event(event: EventCreate, db: DBSession = Depends(get_db)):
    """
    Track a single event. Convenience endpoint for real-time events.
    """
    # TODO: implement in Day 3
    return EventResponse(
        event_id=0,
        session_id=event.session_id,
        event_type=event.event_type,
        product_id=event.product_id,
        metadata=event.metadata,
        timestamp="2025-01-01T00:00:00Z",
    )
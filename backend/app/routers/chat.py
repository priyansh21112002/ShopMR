import logging

from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session as DBSessionType

from app.models.database import get_db, Session as SessionModel, Event, utc_now
from app.models.schemas import ChatRequest, ChatResponse
from app.services.llm_agent import chat_with_agent

logger = logging.getLogger(__name__)

router = APIRouter()


@router.post("/message", response_model=ChatResponse)
async def send_message(request: ChatRequest, db: DBSessionType = Depends(get_db)):
    session = db.query(SessionModel).filter(
        SessionModel.session_id == request.session_id
    ).first()

    if not session:
        raise HTTPException(status_code=404, detail=f"Session '{request.session_id}' not found")

    if not session.is_active:
        raise HTTPException(status_code=400, detail="Session has ended. Start a new session to chat.")

    result = await chat_with_agent(
        session_id=request.session_id,
        message=request.message,
        conversation_history=request.conversation_history
    )

    try:
        chat_event = Event(
            session_id=request.session_id,
            event_type="chat",
            metadata_={
                "message": request.message,
                "response": result["response"][:500],
                "tool_used": result["tool_used"],
                "products_mentioned": result["products_mentioned"]
            },
            timestamp=utc_now()
        )
        db.add(chat_event)
        db.commit()
    except Exception as e:
        logger.warning(f"Failed to log chat event: {e}")
        db.rollback()

    return ChatResponse(
        session_id=request.session_id,
        response=result["response"],
        products_mentioned=result["products_mentioned"],
        tool_used=result["tool_used"]
    )
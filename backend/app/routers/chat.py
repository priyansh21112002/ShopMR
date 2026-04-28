"""
ShopMR — Chat Router
AI shopping assistant powered by LangChain + Groq.
"""

from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session as DBSession

from app.models.database import get_db
from app.models.schemas import ChatRequest, ChatResponse

router = APIRouter()


@router.post("/message", response_model=ChatResponse)
def send_message(request: ChatRequest, db: DBSession = Depends(get_db)):
    """
    Send a message to the AI shopping assistant.
    The agent can use tools: search, recommend, lookup, room_analysis.
    """
    # TODO: implement in Day 6
    return ChatResponse(
        session_id=request.session_id,
        response="I'm the ShopMR assistant! I'll be fully functional soon. 🛋️",
        products_mentioned=None,
        tool_used=None,
    )
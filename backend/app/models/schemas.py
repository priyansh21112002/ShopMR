"""
ShopMR — Pydantic Schemas
Request/response models for all API endpoints.
"""

from datetime import datetime
from typing import Optional

from pydantic import BaseModel, Field


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  SESSIONS
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

class SessionStartRequest(BaseModel):
    """Quest 3 sends this when user opens the app."""
    device_id: Optional[str] = Field(None, description="Quest 3 device identifier")
    user_id: Optional[str] = Field(None, description="Existing user ID (if returning user)")


class SessionStartResponse(BaseModel):
    """Returned to Quest 3 — tells it the session ID and A/B variant."""
    session_id: str
    user_id: str
    variant: str  # "control" or "treatment"
    message: str = "Session started"


class SessionEndRequest(BaseModel):
    """Quest 3 sends this when user exits the app."""
    session_id: str


class SessionEndResponse(BaseModel):
    session_id: str
    duration_seconds: float
    message: str = "Session ended"


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  EVENTS
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

class EventCreate(BaseModel):
    """Single event from Quest 3."""
    session_id: str
    event_type: str = Field(
        ...,
        description="One of: view, place, rotate, scale, purchase, chat, recommend_click"
    )
    product_id: Optional[str] = Field(None, description="Product involved (if any)")
    metadata: Optional[dict] = Field(None, description="Extra event data (position, duration, etc.)")


class EventBatchCreate(BaseModel):
    """Batch of events — Unity can buffer and send multiple at once."""
    events: list[EventCreate] = Field(..., min_length=1, max_length=100)


class EventResponse(BaseModel):
    event_id: int
    session_id: str
    event_type: str
    product_id: Optional[str] = None
    metadata: Optional[dict] = None
    timestamp: datetime


class EventBatchResponse(BaseModel):
    received: int
    message: str = "Events recorded"


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  PRODUCTS
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

class ProductBase(BaseModel):
    name: str
    category: str
    price: float = Field(..., gt=0)
    description: Optional[str] = None
    model_url: Optional[str] = None
    thumbnail_url: Optional[str] = None
    dimensions: Optional[dict] = None  # {"width": 2.1, "height": 0.9, "depth": 0.95}
    tags: Optional[list[str]] = None   # ["modern", "leather", "brown"]


class ProductCreate(ProductBase):
    """Admin endpoint — add a product to the catalog."""
    pass


class ProductResponse(ProductBase):
    """Full product returned to Unity or dashboard."""
    product_id: str
    embedding_id: Optional[str] = None
    is_active: bool
    created_at: datetime

    model_config = {"from_attributes": True}


class ProductListResponse(BaseModel):
    products: list[ProductResponse]
    total: int


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  RECOMMENDATIONS
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

class RecommendationRequest(BaseModel):
    """Optional context for better recommendations."""
    session_id: str
    current_product_id: Optional[str] = Field(None, description="Product user is currently viewing")
    room_context: Optional[dict] = Field(None, description="Room dimensions, existing furniture")
    limit: int = Field(6, ge=1, le=20)


class RecommendedProduct(BaseModel):
    product_id: str
    name: str
    category: str
    price: float
    thumbnail_url: Optional[str] = None
    score: float = Field(..., description="Relevance score 0-1")
    reason: str = Field(..., description="Why this was recommended")


class RecommendationResponse(BaseModel):
    session_id: str
    variant: str  # so Unity knows which reco engine was used
    recommendations: list[RecommendedProduct]
    model_version: Optional[str] = None  # MLflow run ID


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  CHAT
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

class ChatRequest(BaseModel):
    """User message from Quest 3 chat UI."""
    session_id: str
    message: str = Field(..., min_length=1, max_length=1000)
    conversation_history: Optional[list[dict]] = Field(
        None,
        description="Previous messages: [{'role': 'user'|'assistant', 'content': '...'}]"
    )


class ChatResponse(BaseModel):
    session_id: str
    response: str
    products_mentioned: Optional[list[str]] = Field(
        None,
        description="Product IDs referenced in the response"
    )
    tool_used: Optional[str] = Field(
        None,
        description="Which agent tool was invoked (search, recommend, lookup, room)"
    )


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  DASHBOARD
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

class DashboardKPIs(BaseModel):
    total_sessions: int
    active_sessions: int
    total_events: int
    total_users: int
    avg_session_duration_seconds: Optional[float] = None
    total_purchases: int
    total_revenue: float


class FunnelStep(BaseModel):
    step: str       # "view" → "place" → "purchase"
    count: int
    rate: float     # conversion rate from previous step


class FunnelResponse(BaseModel):
    funnel: list[FunnelStep]
    variant: Optional[str] = None  # if filtered by variant


class ABResultMetric(BaseModel):
    variant: str
    sessions: int
    conversion_rate: float
    avg_session_duration: Optional[float] = None
    avg_events_per_session: float


class ABResultResponse(BaseModel):
    experiment_name: str
    metrics: list[ABResultMetric]
    p_value: Optional[float] = None
    is_significant: bool = False
    winner: Optional[str] = None


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  HEALTH CHECK
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

class HealthResponse(BaseModel):
    status: str = "healthy"
    app_name: str
    version: str
    database: str = "connected"
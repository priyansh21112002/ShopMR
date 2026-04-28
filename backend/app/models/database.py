"""
ShopMR — SQLAlchemy Database Models & Connection
Defines all PostgreSQL tables and provides the session factory.
"""

import uuid
from datetime import datetime, timezone

from sqlalchemy import (
    Column,
    String,
    Float,
    Boolean,
    DateTime,
    ForeignKey,
    Integer,
    Text,
    create_engine,
)
from sqlalchemy.dialects.postgresql import UUID, JSONB
from sqlalchemy.orm import declarative_base, relationship, sessionmaker

from app.config import get_settings

settings = get_settings()

# ── Engine & Session ─────────────────────────────────────────
engine = create_engine(
    settings.DATABASE_URL,
    pool_size=10,
    max_overflow=20,
    pool_pre_ping=True,  # reconnect stale connections
)

SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

Base = declarative_base()


# ── Dependency for FastAPI routes ────────────────────────────
def get_db():
    """
    Yields a DB session per request, auto-closes after response.
    Usage: db: Session = Depends(get_db)
    """
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


# ── Helper ───────────────────────────────────────────────────
def generate_uuid():
    return str(uuid.uuid4())


def utc_now():
    return datetime.now(timezone.utc)


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  TABLE: users
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
class User(Base):
    __tablename__ = "users"

    user_id = Column(String, primary_key=True, default=generate_uuid)
    device_id = Column(String, nullable=True, index=True)
    created_at = Column(DateTime(timezone=True), default=utc_now)

    # Relationships
    sessions = relationship("Session", back_populates="user", cascade="all, delete-orphan")

    def __repr__(self):
        return f"<User {self.user_id}>"


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  TABLE: sessions
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
class Session(Base):
    __tablename__ = "sessions"

    session_id = Column(String, primary_key=True, default=generate_uuid)
    user_id = Column(String, ForeignKey("users.user_id"), nullable=False, index=True)
    variant = Column(String, nullable=False)  # "control" or "treatment"
    started_at = Column(DateTime(timezone=True), default=utc_now)
    ended_at = Column(DateTime(timezone=True), nullable=True)
    is_active = Column(Boolean, default=True)

    # Relationships
    user = relationship("User", back_populates="sessions")
    events = relationship("Event", back_populates="session", cascade="all, delete-orphan")

    def __repr__(self):
        return f"<Session {self.session_id} variant={self.variant}>"


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  TABLE: events
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
class Event(Base):
    __tablename__ = "events"

    event_id = Column(Integer, primary_key=True, autoincrement=True)
    session_id = Column(String, ForeignKey("sessions.session_id"), nullable=False, index=True)
    event_type = Column(String, nullable=False, index=True)
    # Event types: "view", "place", "rotate", "scale", "purchase", "chat", "recommend_click"
    product_id = Column(String, ForeignKey("products.product_id"), nullable=True, index=True)
    metadata_ = Column("metadata", JSONB, nullable=True)
    # metadata examples:
    #   view:     {"duration_seconds": 12.5}
    #   place:    {"position": {"x": 1.2, "y": 0, "z": -3.4}, "rotation": {"y": 90}}
    #   purchase: {"price": 299.99, "currency": "USD"}
    #   chat:     {"message": "Show me a red sofa", "response": "..."}
    timestamp = Column(DateTime(timezone=True), default=utc_now, index=True)

    # Relationships
    session = relationship("Session", back_populates="events")
    product = relationship("Product", back_populates="events")

    def __repr__(self):
        return f"<Event {self.event_id} type={self.event_type}>"


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  TABLE: products
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
class Product(Base):
    __tablename__ = "products"

    product_id = Column(String, primary_key=True, default=generate_uuid)
    name = Column(String, nullable=False)
    category = Column(String, nullable=False, index=True)
    # Categories: "sofa", "chair", "table", "lamp", "shelf", "bed", "desk", "decor"
    price = Column(Float, nullable=False)
    description = Column(Text, nullable=True)
    model_url = Column(String, nullable=True)  # S3 URL to .glb 3D model
    thumbnail_url = Column(String, nullable=True)  # S3 URL to preview image
    dimensions = Column(JSONB, nullable=True)
    # {"width": 2.1, "height": 0.9, "depth": 0.95} in meters
    tags = Column(JSONB, nullable=True)
    # ["modern", "leather", "brown", "living-room"]
    embedding_id = Column(String, nullable=True)  # Pinecone vector ID
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime(timezone=True), default=utc_now)

    # Relationships
    events = relationship("Event", back_populates="product")

    def __repr__(self):
        return f"<Product {self.product_id} name={self.name}>"


# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#  TABLE: ab_experiments
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
class ABExperiment(Base):
    __tablename__ = "ab_experiments"

    experiment_id = Column(String, primary_key=True, default=generate_uuid)
    name = Column(String, nullable=False, unique=True)
    description = Column(Text, nullable=True)
    variants = Column(JSONB, nullable=False)
    # {"control": "Popularity-based reco", "treatment": "AI hybrid reco"}
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime(timezone=True), default=utc_now)

    def __repr__(self):
        return f"<ABExperiment {self.name}>"


# ── Table Creation Helper ────────────────────────────────────
def create_all_tables():
    """Create all tables in the database. Called on app startup."""
    Base.metadata.create_all(bind=engine)
"""
ShopMR LLM Agent Service
------------------------
Reliable Groq-powered shopping assistant with 4 tools:
- search_products
- get_recommendations
- get_product_details
- analyze_room

This implementation avoids fragile LangChain agent-executor version issues
while still using Groq + conversation history + tool-backed responses.
"""

import json
import logging
import re
from typing import Optional

from groq import Groq

from app.config import get_settings
from app.models.database import SessionLocal, Product, Session as DBSession
from app.services.pinecone_client import query_similar_products
from app.services.reco_engine import get_recommendations

logger = logging.getLogger(__name__)
settings = get_settings()


def get_groq_client() -> Groq:
    return Groq(api_key=settings.GROQ_API_KEY)


def search_products_tool(query: str, category: Optional[str] = None, max_results: int = 5) -> dict:
    """Search catalog using Pinecone semantic search."""
    db = SessionLocal()
    try:
        max_results = min(max_results, 10)

        results = query_similar_products(
            query_text=query,
            top_k=max_results,
            category_filter=category
        )

        if not results:
            return {"products": [], "total_found": 0, "message": "No matching products found."}

        product_ids = [r["product_id"] for r in results]
        products = db.query(Product).filter(
            Product.product_id.in_(product_ids),
            Product.is_active == True
        ).all()

        product_map = {p.product_id: p for p in products}

        enriched = []
        for r in results:
            p = product_map.get(r["product_id"])
            if not p:
                continue
            enriched.append({
                "product_id": p.product_id,
                "name": p.name,
                "category": p.category,
                "price": p.price,
                "description": p.description,
                "dimensions": p.dimensions,
                "tags": p.tags,
                "score": round(float(r["score"]), 4)
            })

        return {
            "products": enriched,
            "total_found": len(enriched),
            "query": query,
            "category": category
        }

    except Exception as e:
        logger.error(f"search_products_tool error: {e}", exc_info=True)
        return {"error": str(e)}
    finally:
        db.close()


def get_product_details_tool(product_id: str) -> dict:
    """Fetch one product from PostgreSQL."""
    db = SessionLocal()
    try:
        product = db.query(Product).filter(
            Product.product_id == product_id,
            Product.is_active == True
        ).first()

        if not product:
            return {"error": f"Product '{product_id}' not found"}

        return {
            "product_id": product.product_id,
            "name": product.name,
            "category": product.category,
            "price": product.price,
            "description": product.description,
            "dimensions": product.dimensions,
            "tags": product.tags,
            "model_url": product.model_url,
            "thumbnail_url": product.thumbnail_url
        }

    except Exception as e:
        logger.error(f"get_product_details_tool error: {e}", exc_info=True)
        return {"error": str(e)}
    finally:
        db.close()


def get_recommendations_tool(
    session_id: str,
    current_product_id: Optional[str] = None,
    room_context: Optional[str] = None,
    limit: int = 6
) -> dict:
    """Get personalized recommendations using existing reco engine."""
    db = SessionLocal()
    try:
        session = db.query(DBSession).filter(DBSession.session_id == session_id).first()
        if not session:
            return {"error": f"Session '{session_id}' not found"}

        recommendations, run_id = get_recommendations(
            db=db,
            session_id=session_id,
            variant=session.variant,
            current_product_id=current_product_id,
            room_context=room_context,
            limit=limit
        )

        serialized_recommendations = []
        for r in recommendations:
            if isinstance(r, dict):
                serialized_recommendations.append({
                    "product_id": r.get("product_id"),
                    "name": r.get("name"),
                    "category": r.get("category"),
                    "price": r.get("price"),
                    "thumbnail_url": r.get("thumbnail_url"),
                    "score": float(r.get("score", 0.0)) if r.get("score") is not None else 0.0,
                    "reason": r.get("reason"),
                })
            else:
                serialized_recommendations.append({
                    "product_id": getattr(r, "product_id", None),
                    "name": getattr(r, "name", None),
                    "category": getattr(r, "category", None),
                    "price": getattr(r, "price", None),
                    "thumbnail_url": getattr(r, "thumbnail_url", None),
                    "score": float(getattr(r, "score", 0.0) or 0.0),
                    "reason": getattr(r, "reason", None),
                })

        return {
            "session_id": session_id,
            "variant": session.variant,
            "model_run_id": run_id,
            "recommendations": serialized_recommendations
        }

    except Exception as e:
        logger.error(f"get_recommendations_tool error: {e}", exc_info=True)
        return {"error": str(e)}
    finally:
        db.close()


def analyze_room_tool(room_description: str, furniture_type: Optional[str] = None) -> dict:
    """Give room-fit suggestions backed by product search."""
    query = f"{furniture_type or 'furniture'} for {room_description}"
    search_result = search_products_tool(query=query, category=furniture_type, max_results=5)

    return {
        "room_description": room_description,
        "furniture_type": furniture_type,
        "suggestions": search_result.get("products", []),
        "advice": (
            "Prefer leaving comfortable walking space around large furniture. "
            "In MR, place items at real scale first to check fit, clearance, and balance."
        )
    }


def detect_product_id(text: str) -> Optional[str]:
    match = re.search(r"\bprod-\d{3}\b", text.lower())
    return match.group(0) if match else None


def detect_category(text: str) -> Optional[str]:
    categories = ["sofa", "chair", "table", "lamp", "shelf", "bed", "desk", "decor"]
    text_lower = text.lower()
    for cat in categories:
        if cat in text_lower:
            return cat
    return None


def choose_tool(message: str) -> tuple[str, dict]:
    """
    Simple deterministic router.
    This is more reliable than depending on fast-moving LangChain agent APIs.
    """
    msg = message.lower()

    product_id = detect_product_id(message)
    category = detect_category(message)

    if product_id and any(x in msg for x in ["tell me about", "details", "more about", "what is", "price", "dimension"]):
        return "get_product_details", {"product_id": product_id}

    if any(x in msg for x in ["recommend", "suggest", "show me more", "what else", "for me"]):
        return "get_recommendations", {}

    if any(x in msg for x in ["fit", "room", "living room", "bedroom", "layout", "space", "apartment", "dimensions", "4m", "5m", "3m"]):
        return "analyze_room", {"room_description": message, "furniture_type": category}

    if any(x in msg for x in ["show me", "find", "search", "looking for", "browse", "do you have"]):
        return "search_products", {"query": message, "category": category}

    if product_id:
        return "get_product_details", {"product_id": product_id}

    return "search_products", {"query": message, "category": category}


def extract_product_ids_from_data(data) -> list[str]:
    found = set()

    def _walk(obj):
        if isinstance(obj, dict):
            if "product_id" in obj and isinstance(obj["product_id"], str):
                found.add(obj["product_id"])
            for v in obj.values():
                _walk(v)
        elif isinstance(obj, list):
            for item in obj:
                _walk(item)

    _walk(data)
    return list(found)


def build_llm_messages(
    message: str,
    conversation_history: Optional[list[dict]],
    tool_name: str,
    tool_result: dict
) -> list[dict]:
    system_prompt = (
        "You are ShopMR Assistant, a concise and helpful furniture shopping assistant for a "
        "mixed reality shopping app on Meta Quest 3. Users can place furniture in their real room. "
        "Keep answers short and useful, ideally 2-4 sentences. "
        "When mentioning products, include name, product_id, and price if available. "
        "Base your answer only on the provided tool result. "
        "If tool results are empty, say that clearly and offer a helpful next step."
    )

    messages = [{"role": "system", "content": system_prompt}]

    if conversation_history:
        for msg in conversation_history[-10:]:
            role = msg.get("role")
            content = msg.get("content", "")
            if role in {"user", "assistant"} and content:
                messages.append({"role": role, "content": content})

    messages.append({"role": "user", "content": message})
    messages.append({
        "role": "system",
        "content": f"Tool used: {tool_name}\nTool result:\n{json.dumps(tool_result, indent=2, default=str)}"
    })

    return messages


async def chat_with_agent(
    session_id: str,
    message: str,
    conversation_history: Optional[list[dict]] = None
) -> dict:
    """
    Main chat entry point.
    Returns:
    {
      "response": str,
      "products_mentioned": list[str],
      "tool_used": str | None
    }
    """
    try:
        tool_name, tool_args = choose_tool(message)

        if tool_name == "search_products":
            tool_result = search_products_tool(**tool_args)

        elif tool_name == "get_product_details":
            tool_result = get_product_details_tool(**tool_args)

        elif tool_name == "get_recommendations":
            tool_result = get_recommendations_tool(session_id=session_id, **tool_args)

        elif tool_name == "analyze_room":
            tool_result = analyze_room_tool(**tool_args)

        else:
            tool_name = "search_products"
            tool_result = search_products_tool(query=message)

        client = get_groq_client()
        messages = build_llm_messages(
            message=message,
            conversation_history=conversation_history,
            tool_name=tool_name,
            tool_result=tool_result
        )

        completion = client.chat.completions.create(
            model=settings.GROQ_MODEL,
            messages=messages,
            temperature=0.3,
            max_tokens=300
        )

        response_text = completion.choices[0].message.content.strip()
        products_mentioned = extract_product_ids_from_data(tool_result)

        return {
            "response": response_text,
            "products_mentioned": products_mentioned,
            "tool_used": tool_name
        }

    except Exception as e:
        logger.error(f"chat_with_agent error: {e}", exc_info=True)
        return {
            "response": "I'm having trouble right now. Please try again in a moment. 🛋️",
            "products_mentioned": [],
            "tool_used": None
        }
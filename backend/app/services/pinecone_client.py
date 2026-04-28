"""
ShopMR — Pinecone Vector DB Client
Handles embedding generation and vector search for product recommendations.
"""

import logging
from typing import Optional

from pinecone import Pinecone
from sentence_transformers import SentenceTransformer

from app.config import get_settings

settings = get_settings()
logger = logging.getLogger("shopmr.pinecone")

# ── Lazy-loaded singletons ───────────────────────────────────
_pinecone_client: Optional[Pinecone] = None
_pinecone_index = None
_embedding_model: Optional[SentenceTransformer] = None


def get_embedding_model() -> SentenceTransformer:
    """Load the embedding model once and reuse."""
    global _embedding_model
    if _embedding_model is None:
        logger.info(f"📥 Loading embedding model: {settings.EMBEDDING_MODEL}...")
        _embedding_model = SentenceTransformer(settings.EMBEDDING_MODEL)
        logger.info(f"✅ Embedding model loaded ({settings.EMBEDDING_DIMENSION}d)")
    return _embedding_model


def get_pinecone_index():
    """Connect to Pinecone index once and reuse."""
    global _pinecone_client, _pinecone_index
    if _pinecone_index is None:
        logger.info(f"🔌 Connecting to Pinecone index: {settings.PINECONE_INDEX_NAME}...")
        _pinecone_client = Pinecone(api_key=settings.PINECONE_API_KEY)
        _pinecone_index = _pinecone_client.Index(settings.PINECONE_INDEX_NAME)
        logger.info(f"✅ Pinecone connected")
    return _pinecone_index


def generate_embedding(text: str) -> list[float]:
    """
    Generate a 384-dimensional embedding for a text string.
    Used for both indexing products and querying.
    """
    model = get_embedding_model()
    embedding = model.encode(text, normalize_embeddings=True)
    return embedding.tolist()


def build_product_text(product: dict) -> str:
    """
    Build a rich text representation of a product for embedding.
    Combines name, category, description, and tags for maximum semantic signal.
    """
    parts = [
        product.get("name", ""),
        f"Category: {product.get('category', '')}",
        product.get("description", "") or "",
    ]

    tags = product.get("tags", [])
    if tags:
        parts.append(f"Tags: {', '.join(tags)}")

    dimensions = product.get("dimensions", {})
    if dimensions:
        parts.append(
            f"Size: {dimensions.get('width', 0)}m wide, "
            f"{dimensions.get('height', 0)}m tall, "
            f"{dimensions.get('depth', 0)}m deep"
        )

    price = product.get("price", 0)
    if price:
        parts.append(f"Price: ${price:.2f}")

    return " | ".join(parts)


def upsert_product(product: dict) -> str:
    """
    Embed a single product and upsert to Pinecone.
    Returns the embedding ID.
    """
    index = get_pinecone_index()

    product_text = build_product_text(product)
    embedding = generate_embedding(product_text)

    product_id = product["product_id"]

    # Metadata stored alongside the vector in Pinecone
    metadata = {
        "product_id": product_id,
        "name": product["name"],
        "category": product.get("category", ""),
        "price": float(product.get("price", 0)),
        "tags": product.get("tags", []),
    }

    index.upsert(vectors=[(product_id, embedding, metadata)])

    logger.debug(f"📌 Upserted: {product['name']} ({product_id})")
    return product_id


def upsert_products_batch(products: list[dict]) -> int:
    """
    Embed and upsert multiple products in batch.
    More efficient than one-by-one.
    """
    index = get_pinecone_index()
    model = get_embedding_model()

    # Build texts for all products
    texts = [build_product_text(p) for p in products]

    # Batch embed
    embeddings = model.encode(texts, normalize_embeddings=True, show_progress_bar=True)

    # Build vectors
    vectors = []
    for product, embedding in zip(products, embeddings):
        product_id = product["product_id"]
        metadata = {
            "product_id": product_id,
            "name": product["name"],
            "category": product.get("category", ""),
            "price": float(product.get("price", 0)),
            "tags": product.get("tags", []),
        }
        vectors.append((product_id, embedding.tolist(), metadata))

    # Upsert in batch
    index.upsert(vectors=vectors)

    logger.info(f"📌 Batch upserted {len(vectors)} products to Pinecone")
    return len(vectors)


def query_similar_products(
    query_text: str,
    top_k: int = 6,
    category_filter: str = None,
    exclude_ids: list[str] = None,
) -> list[dict]:
    """
    Find similar products by semantic search.
    Used by the treatment (AI) recommendation engine.

    Returns list of {product_id, score, metadata}
    """
    index = get_pinecone_index()
    query_embedding = generate_embedding(query_text)

    # Build Pinecone filter
    pinecone_filter = {}
    if category_filter:
        pinecone_filter["category"] = {"$eq": category_filter}

    results = index.query(
        vector=query_embedding,
        top_k=top_k + (len(exclude_ids) if exclude_ids else 0),
        include_metadata=True,
        filter=pinecone_filter if pinecone_filter else None,
    )

    # Process results
    similar_products = []
    for match in results.matches:
        if exclude_ids and match.id in exclude_ids:
            continue
        if len(similar_products) >= top_k:
            break

        similar_products.append({
            "product_id": match.id,
            "score": round(float(match.score), 4),
            "metadata": match.metadata,
        })

    logger.info(
        f"🔍 Semantic search: '{query_text[:50]}...' → {len(similar_products)} results"
    )
    return similar_products


def query_similar_to_product(
    product_id: str,
    top_k: int = 6,
) -> list[dict]:
    """
    Find products similar to a given product.
    Fetches the product's vector from Pinecone and queries for nearest neighbors.
    """
    index = get_pinecone_index()

    # Fetch the product's existing vector
    fetch_result = index.fetch(ids=[product_id])

    if product_id not in fetch_result.vectors:
        logger.warning(f"⚠️ Product {product_id} not found in Pinecone")
        return []

    product_vector = fetch_result.vectors[product_id].values

    # Query for similar (exclude self)
    results = index.query(
        vector=product_vector,
        top_k=top_k + 1,
        include_metadata=True,
    )

    similar_products = []
    for match in results.matches:
        if match.id == product_id:
            continue
        if len(similar_products) >= top_k:
            break

        similar_products.append({
            "product_id": match.id,
            "score": round(float(match.score), 4),
            "metadata": match.metadata,
        })

    return similar_products
"""
ShopMR — Products Router
Product catalog CRUD and search endpoints for Unity and dashboard.
"""

import logging
from typing import Optional

from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.orm import Session as DBSession
from sqlalchemy import or_

from app.models.database import get_db, Product
from app.models.schemas import (
    ProductCreate,
    ProductResponse,
    ProductListResponse,
)

router = APIRouter()
logger = logging.getLogger("shopmr.products")


@router.get("/", response_model=ProductListResponse)
def list_products(
    category: Optional[str] = Query(None, description="Filter by category (sofa, chair, table, etc.)"),
    min_price: Optional[float] = Query(None, ge=0, description="Minimum price"),
    max_price: Optional[float] = Query(None, ge=0, description="Maximum price"),
    tags: Optional[str] = Query(None, description="Comma-separated tags to filter by (e.g. 'modern,leather')"),
    search: Optional[str] = Query(None, description="Search product name and description"),
    sort_by: Optional[str] = Query("name", description="Sort by: name, price, category, created_at"),
    sort_order: Optional[str] = Query("asc", description="Sort order: asc or desc"),
    limit: int = Query(20, ge=1, le=100, description="Number of results"),
    offset: int = Query(0, ge=0, description="Pagination offset"),
    db: DBSession = Depends(get_db),
):
    """
    List and filter products from the catalog.
    Unity calls this to populate the product browser.

    Examples:
    - GET /api/products/?category=sofa
    - GET /api/products/?min_price=100&max_price=500
    - GET /api/products/?tags=modern,leather
    - GET /api/products/?search=wooden&sort_by=price&sort_order=asc
    """
    query = db.query(Product).filter(Product.is_active == True)

    # ── Category filter ──
    if category:
        query = query.filter(Product.category == category.lower())

    # ── Price range filter ──
    if min_price is not None:
        query = query.filter(Product.price >= min_price)
    if max_price is not None:
        query = query.filter(Product.price <= max_price)

    # ── Tag filter (any match) ──
    if tags:
        tag_list = [t.strip().lower() for t in tags.split(",")]
        # JSONB array contains any of the tags
        tag_conditions = [Product.tags.contains([tag]) for tag in tag_list]
        query = query.filter(or_(*tag_conditions))

    # ── Text search (name + description) ──
    if search:
        search_term = f"%{search.lower()}%"
        query = query.filter(
            or_(
                Product.name.ilike(search_term),
                Product.description.ilike(search_term),
            )
        )

    # ── Get total count before pagination ──
    total = query.count()

    # ── Sorting ──
    sort_column_map = {
        "name": Product.name,
        "price": Product.price,
        "category": Product.category,
        "created_at": Product.created_at,
    }
    sort_column = sort_column_map.get(sort_by, Product.name)

    if sort_order == "desc":
        query = query.order_by(sort_column.desc())
    else:
        query = query.order_by(sort_column.asc())

    # ── Pagination ──
    products = query.offset(offset).limit(limit).all()

    logger.info(
        f"📦 Products query: {total} results | "
        f"category={category} price={min_price}-{max_price} "
        f"tags={tags} search={search}"
    )

    return ProductListResponse(
        products=[ProductResponse.model_validate(p) for p in products],
        total=total,
    )


@router.get("/categories")
def list_categories(db: DBSession = Depends(get_db)):
    """
    Get all product categories with counts.
    Unity uses this to build the category filter UI.
    """
    from sqlalchemy import func

    results = db.query(
        Product.category,
        func.count(Product.product_id).label("count"),
    ).filter(
        Product.is_active == True
    ).group_by(Product.category).order_by(Product.category).all()

    return {
        "categories": [
            {"name": row.category, "count": row.count}
            for row in results
        ],
        "total_categories": len(results),
    }


@router.get("/{product_id}", response_model=ProductResponse)
def get_product(product_id: str, db: DBSession = Depends(get_db)):
    """
    Get a single product by ID.
    Unity calls this when user selects a product to view details / place in room.
    """
    product = db.query(Product).filter(
        Product.product_id == product_id,
        Product.is_active == True,
    ).first()

    if not product:
        raise HTTPException(status_code=404, detail="Product not found")

    logger.info(f"📦 Product viewed: {product.name} ({product_id})")

    return ProductResponse.model_validate(product)


@router.post("/", response_model=ProductResponse, status_code=201)
def create_product(product_data: ProductCreate, db: DBSession = Depends(get_db)):
    """
    Add a new product to the catalog.
    Admin endpoint — used for catalog management.
    """
    product = Product(
        name=product_data.name,
        category=product_data.category.lower(),
        price=product_data.price,
        description=product_data.description,
        model_url=product_data.model_url,
        thumbnail_url=product_data.thumbnail_url,
        dimensions=product_data.dimensions,
        tags=[t.lower() for t in product_data.tags] if product_data.tags else None,
    )

    db.add(product)
    db.commit()
    db.refresh(product)

    logger.info(f"✅ Product created: {product.name} ({product.product_id})")

    return ProductResponse.model_validate(product)


@router.delete("/{product_id}")
def delete_product(product_id: str, db: DBSession = Depends(get_db)):
    """
    Soft-delete a product (sets is_active = False).
    Product data is preserved for historical event references.
    """
    product = db.query(Product).filter(
        Product.product_id == product_id,
    ).first()

    if not product:
        raise HTTPException(status_code=404, detail="Product not found")

    product.is_active = False
    db.commit()

    logger.info(f"🗑️ Product deactivated: {product.name} ({product_id})")

    return {"message": f"Product '{product.name}' deactivated", "product_id": product_id}
"""
ShopMR — Product Catalog Seed Script
Populates the products table with 20 furniture items.
Run: docker exec shopmr-backend python -m scripts.seed_products
"""

import sys
import os

# Add the backend directory to path so we can import app modules
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.models.database import SessionLocal, Product, create_all_tables

PRODUCTS = [
    # ── Sofas ────────────────────────────────────
    {
        "product_id": "prod-001",
        "name": "Modern Leather Sofa",
        "category": "sofa",
        "price": 899.99,
        "description": "Sleek 3-seater leather sofa in charcoal grey with chrome legs. Perfect for modern living rooms.",
        "dimensions": {"width": 2.2, "height": 0.85, "depth": 0.95},
        "tags": ["modern", "leather", "grey", "living-room", "3-seater"],
    },
    {
        "product_id": "prod-002",
        "name": "Scandinavian Fabric Sofa",
        "category": "sofa",
        "price": 649.99,
        "description": "Minimalist 2-seater sofa in light beige fabric with wooden legs. Scandinavian design.",
        "dimensions": {"width": 1.8, "height": 0.80, "depth": 0.90},
        "tags": ["scandinavian", "fabric", "beige", "living-room", "2-seater", "minimalist"],
    },
    {
        "product_id": "prod-003",
        "name": "Velvet Sectional Sofa",
        "category": "sofa",
        "price": 1299.99,
        "description": "L-shaped sectional in deep navy velvet. Generous seating for the whole family.",
        "dimensions": {"width": 2.8, "height": 0.88, "depth": 1.7},
        "tags": ["velvet", "navy", "sectional", "living-room", "luxury", "l-shaped"],
    },
    # ── Chairs ───────────────────────────────────
    {
        "product_id": "prod-004",
        "name": "Ergonomic Office Chair",
        "category": "chair",
        "price": 349.99,
        "description": "Adjustable mesh office chair with lumbar support and headrest. Built for long work sessions.",
        "dimensions": {"width": 0.68, "height": 1.2, "depth": 0.68},
        "tags": ["ergonomic", "office", "mesh", "black", "adjustable"],
    },
    {
        "product_id": "prod-005",
        "name": "Mid-Century Accent Chair",
        "category": "chair",
        "price": 299.99,
        "description": "Retro-inspired accent chair in mustard yellow with walnut wood frame.",
        "dimensions": {"width": 0.72, "height": 0.82, "depth": 0.75},
        "tags": ["mid-century", "accent", "yellow", "walnut", "living-room", "retro"],
    },
    {
        "product_id": "prod-006",
        "name": "Rattan Lounge Chair",
        "category": "chair",
        "price": 199.99,
        "description": "Natural rattan lounge chair with white cushion. Boho style for any room.",
        "dimensions": {"width": 0.75, "height": 0.90, "depth": 0.80},
        "tags": ["rattan", "boho", "natural", "lounge", "white", "cushion"],
    },
    # ── Tables ───────────────────────────────────
    {
        "product_id": "prod-007",
        "name": "Oak Dining Table",
        "category": "table",
        "price": 599.99,
        "description": "Solid oak dining table seating 6. Warm natural finish with tapered legs.",
        "dimensions": {"width": 1.6, "height": 0.76, "depth": 0.90},
        "tags": ["oak", "dining", "natural", "6-seater", "solid-wood"],
    },
    {
        "product_id": "prod-008",
        "name": "Glass Coffee Table",
        "category": "table",
        "price": 249.99,
        "description": "Tempered glass coffee table with gold metal frame. Elegant centerpiece.",
        "dimensions": {"width": 1.1, "height": 0.45, "depth": 0.60},
        "tags": ["glass", "coffee-table", "gold", "elegant", "living-room"],
    },
    {
        "product_id": "prod-009",
        "name": "Marble Side Table",
        "category": "table",
        "price": 179.99,
        "description": "Round marble-top side table with black metal base. Compact and luxurious.",
        "dimensions": {"width": 0.45, "height": 0.55, "depth": 0.45},
        "tags": ["marble", "side-table", "black", "round", "compact", "luxury"],
    },
    # ── Lamps ────────────────────────────────────
    {
        "product_id": "prod-010",
        "name": "Arc Floor Lamp",
        "category": "lamp",
        "price": 149.99,
        "description": "Brushed nickel arc floor lamp with white drum shade. Reaches over your sofa.",
        "dimensions": {"width": 0.40, "height": 1.80, "depth": 0.40},
        "tags": ["arc", "floor-lamp", "nickel", "white", "modern"],
    },
    {
        "product_id": "prod-011",
        "name": "Ceramic Table Lamp",
        "category": "lamp",
        "price": 89.99,
        "description": "Handmade ceramic table lamp in terracotta with linen shade.",
        "dimensions": {"width": 0.25, "height": 0.50, "depth": 0.25},
        "tags": ["ceramic", "table-lamp", "terracotta", "handmade", "linen"],
    },
    # ── Shelves ──────────────────────────────────
    {
        "product_id": "prod-012",
        "name": "Industrial Bookshelf",
        "category": "shelf",
        "price": 329.99,
        "description": "5-tier bookshelf with reclaimed wood shelves and black iron frame.",
        "dimensions": {"width": 1.0, "height": 1.8, "depth": 0.35},
        "tags": ["industrial", "bookshelf", "reclaimed-wood", "iron", "5-tier"],
    },
    {
        "product_id": "prod-013",
        "name": "Floating Wall Shelves",
        "category": "shelf",
        "price": 79.99,
        "description": "Set of 3 floating walnut shelves. Clean, modern wall storage.",
        "dimensions": {"width": 0.80, "height": 0.03, "depth": 0.20},
        "tags": ["floating", "wall-shelf", "walnut", "set-of-3", "modern", "minimalist"],
    },
    # ── Beds ─────────────────────────────────────
    {
        "product_id": "prod-014",
        "name": "Upholstered Platform Bed",
        "category": "bed",
        "price": 749.99,
        "description": "Queen platform bed with tufted grey headboard. No box spring needed.",
        "dimensions": {"width": 1.65, "height": 1.2, "depth": 2.15},
        "tags": ["upholstered", "platform", "queen", "grey", "tufted", "bedroom"],
    },
    {
        "product_id": "prod-015",
        "name": "Minimalist Wooden Bed Frame",
        "category": "bed",
        "price": 499.99,
        "description": "King-size bed frame in light pine with clean lines. Japanese-inspired design.",
        "dimensions": {"width": 1.95, "height": 0.35, "depth": 2.15},
        "tags": ["minimalist", "wooden", "king", "pine", "japanese", "bedroom"],
    },
    # ── Desks ────────────────────────────────────
    {
        "product_id": "prod-016",
        "name": "Standing Desk",
        "category": "desk",
        "price": 449.99,
        "description": "Electric sit-stand desk with bamboo top. Adjustable height 70-120cm.",
        "dimensions": {"width": 1.4, "height": 1.2, "depth": 0.70},
        "tags": ["standing-desk", "electric", "bamboo", "adjustable", "office"],
    },
    {
        "product_id": "prod-017",
        "name": "Writing Desk",
        "category": "desk",
        "price": 279.99,
        "description": "Compact writing desk in white with two drawers. Perfect for small spaces.",
        "dimensions": {"width": 1.0, "height": 0.76, "depth": 0.50},
        "tags": ["writing-desk", "white", "compact", "drawers", "small-space"],
    },
    # ── Decor ────────────────────────────────────
    {
        "product_id": "prod-018",
        "name": "Large Indoor Planter",
        "category": "decor",
        "price": 59.99,
        "description": "Matte white ceramic planter with wooden stand. Fits plants up to 30cm pot.",
        "dimensions": {"width": 0.35, "height": 0.70, "depth": 0.35},
        "tags": ["planter", "ceramic", "white", "wooden-stand", "indoor"],
    },
    {
        "product_id": "prod-019",
        "name": "Abstract Wall Art",
        "category": "decor",
        "price": 129.99,
        "description": "Large canvas print with abstract blue and gold brushstrokes. 120x80cm.",
        "dimensions": {"width": 1.2, "height": 0.80, "depth": 0.04},
        "tags": ["wall-art", "abstract", "canvas", "blue", "gold", "large"],
    },
    {
        "product_id": "prod-020",
        "name": "Woven Area Rug",
        "category": "decor",
        "price": 199.99,
        "description": "Hand-woven jute area rug in natural tone. 200x300cm. Adds warmth to any floor.",
        "dimensions": {"width": 2.0, "height": 0.02, "depth": 3.0},
        "tags": ["rug", "jute", "woven", "natural", "hand-made", "large"],
    },
]


def seed_products():
    """Insert all products into the database."""
    db = SessionLocal()
    try:
        # Check if products already exist
        existing = db.query(Product).count()
        if existing > 0:
            print(f"⚠️  Products table already has {existing} items. Skipping seed.")
            print("   To re-seed, run: docker exec shopmr-backend python -c \"from app.models.database import SessionLocal, Product; db = SessionLocal(); db.query(Product).delete(); db.commit(); print('Cleared')\"")
            return

        # Insert all products
        for p in PRODUCTS:
            product = Product(**p)
            db.add(product)

        db.commit()
        print(f"✅ Seeded {len(PRODUCTS)} products into the database!")

        # Print summary
        categories = {}
        for p in PRODUCTS:
            cat = p["category"]
            categories[cat] = categories.get(cat, 0) + 1

        print("\n📦 Products by category:")
        for cat, count in sorted(categories.items()):
            print(f"   {cat}: {count}")

    except Exception as e:
        print(f"❌ Error seeding products: {e}")
        db.rollback()
    finally:
        db.close()


if __name__ == "__main__":
    seed_products()
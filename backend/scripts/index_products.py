"""
ShopMR — Product Indexing Script
Embeds all products using MiniLM and upserts to Pinecone.
Run: docker exec shopmr-backend python -m scripts.index_products
"""

import sys
import os
import time

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.models.database import SessionLocal, Product
from app.services.pinecone_client import upsert_products_batch, build_product_text


def index_all_products():
    """Embed and index all active products to Pinecone."""
    db = SessionLocal()

    try:
        products = db.query(Product).filter(Product.is_active == True).all()

        if not products:
            print("❌ No products found in database. Run seed_products first.")
            return

        print(f"📦 Found {len(products)} products to index")
        print(f"🔤 Building product texts...")

        # Convert to dicts
        product_dicts = []
        for p in products:
            product_dict = {
                "product_id": p.product_id,
                "name": p.name,
                "category": p.category,
                "price": p.price,
                "description": p.description,
                "tags": p.tags or [],
                "dimensions": p.dimensions or {},
            }
            product_dicts.append(product_dict)

            # Preview the text that will be embedded
            text = build_product_text(product_dict)
            print(f"  📝 {p.name}: {text[:80]}...")

        print(f"\n🚀 Embedding and uploading to Pinecone...")
        start = time.time()

        count = upsert_products_batch(product_dicts)

        elapsed = time.time() - start
        print(f"\n✅ Indexed {count} products in {elapsed:.1f}s")

        # Update embedding_id in database
        for p in products:
            p.embedding_id = p.product_id
        db.commit()
        print(f"✅ Updated embedding_id for all products in database")

    except Exception as e:
        print(f"❌ Error: {e}")
        import traceback
        traceback.print_exc()
    finally:
        db.close()


if __name__ == "__main__":
    index_all_products()
"""
ShopMR — Update model_url for all products
Sets the Unity Resources path (e.g. "Furniture/sofa_modern_leather")
so that FurniturePlacer.ResolvePrefab() can load real 3D models
instead of falling back to the placeholder cube.

Run: docker exec shopmr-backend python -m scripts.update_model_urls
  OR: cd backend && python -m scripts.update_model_urls
"""

import sys
import os

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.models.database import SessionLocal, Product

# ── Product ID → Unity Resources path mapping ──
# These paths correspond to prefabs in:
#   Assets/_ShopMR/Resources/Furniture/<name>.prefab
# Unity's Resources.Load() strips "Assets/.../Resources/" and the extension,
# so the runtime path is just "Furniture/<name>".

MODEL_URL_MAP = {
    "prod-001": "Furniture/sofa_modern_leather",
    "prod-002": "Furniture/sofa_scandinavian",
    "prod-003": "Furniture/sofa_velvet_sectional",
    "prod-004": "Furniture/chair_ergonomic",
    "prod-005": "Furniture/chair_accent",
    "prod-006": "Furniture/chair_rattan",
    "prod-007": "Furniture/table_oak_dining",
    "prod-008": "Furniture/table_glass_coffee",
    "prod-009": "Furniture/table_marble_side",
    "prod-010": "Furniture/lamp_arc_floor",
    "prod-011": "Furniture/lamp_ceramic_table",
    "prod-012": "Furniture/shelf_industrial",
    "prod-013": "Furniture/shelf_floating",
    "prod-014": "Furniture/bed_upholstered",
    "prod-015": "Furniture/bed_minimalist",
    "prod-016": "Furniture/desk_standing",
    "prod-017": "Furniture/desk_writing",
    "prod-018": "Furniture/decor_planter",
    "prod-019": "Furniture/decor_wall_art",
    "prod-020": "Furniture/decor_area_rug",
}


def update_model_urls():
    """Update model_url for all products in the database."""
    db = SessionLocal()
    try:
        updated = 0
        skipped = 0
        not_found = 0

        for product_id, model_url in MODEL_URL_MAP.items():
            product = db.query(Product).filter(
                Product.product_id == product_id
            ).first()

            if product is None:
                print(f"  ❌ NOT FOUND: {product_id}")
                not_found += 1
                continue

            if product.model_url == model_url:
                print(f"  ⏭️  SKIP: {product_id} ({product.name}) — already set to '{model_url}'")
                skipped += 1
                continue

            old_url = product.model_url
            product.model_url = model_url
            print(f"  ✅ UPDATE: {product_id} ({product.name}) — '{old_url}' → '{model_url}'")
            updated += 1

        db.commit()

        print(f"\n{'='*50}")
        print(f"Summary: {updated} updated, {skipped} already correct, {not_found} not found")
        print(f"Total mappings: {len(MODEL_URL_MAP)}")

        if not_found > 0:
            print(f"\n⚠️  {not_found} product(s) not found in database.")
            print("   Have you run seed_products?")
            print("   Run: docker exec shopmr-backend python -m scripts.seed_products")

        if updated > 0:
            print(f"\n🎉 {updated} product(s) updated with Unity Resource paths!")
            print("   Unity will now load real 3D furniture models instead of placeholder cubes.")

        # Verify all products have model_url set
        print(f"\n{'='*50}")
        print("Verification — all products with model_url:")
        all_products = db.query(Product).filter(Product.is_active == True).all()
        for p in all_products:
            status = "✅" if p.model_url else "❌ MISSING"
            print(f"  {status} {p.product_id} | {p.name:<30} | model_url: {p.model_url or '(null)'}")

    except Exception as e:
        print(f"❌ Error updating model URLs: {e}")
        db.rollback()
        import traceback
        traceback.print_exc()
    finally:
        db.close()


if __name__ == "__main__":
    update_model_urls()

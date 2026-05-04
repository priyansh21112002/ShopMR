"""
Synthetic user simulator for ShopMR A/B testing.

Generates ~300 sessions by hitting the live FastAPI backend, exercising:
  - session start (triggers A/B assignment)
  - recommendation fetch (control = popularity, treatment = AI hybrid)
  - view / place / rotate / scale / purchase events
  - session end

Treatment users are modeled as more engaged BECAUSE the recommendations are
more relevant — i.e., higher base placement and purchase probability.
This simulates the real-world effect a better recommender would produce.

Run:
    docker exec shopmr-backend python -m scripts.simulate_users

Env overrides:
    NUM_USERS=300         -> how many sessions to simulate
    BASE_URL=...          -> defaults to http://localhost:8000 (inside container)
    SEED=42               -> for reproducibility
"""

import os
import random
import time
import uuid
from typing import Optional

import httpx

BASE_URL = os.getenv("BASE_URL", "http://localhost:8000")
NUM_USERS = int(os.getenv("NUM_USERS", "300"))
SEED = int(os.getenv("SEED", "42"))
TIMEOUT = 15.0

random.seed(SEED)

PRODUCT_IDS = [f"prod-{i:03d}" for i in range(1, 21)]


def post(client: httpx.Client, path: str, payload: dict) -> Optional[dict]:
    try:
        r = client.post(f"{BASE_URL}{path}", json=payload, timeout=TIMEOUT)
        if r.status_code >= 400:
            return None
        return r.json() if r.text else {}
    except Exception:
        return None


def simulate_user(client: httpx.Client, idx: int) -> Optional[dict]:
    device_id = f"sim-device-{idx:04d}-{uuid.uuid4().hex[:6]}"

    # 1) Start session (assigns variant)
    s = post(client, "/api/sessions/start", {"device_id": device_id})
    if s is None:
        return None

    session_id = s["session_id"]
    variant = s["variant"]

    # 2) Engagement model — treatment gets a lift because better recos => more clicks/places
    n_views = random.randint(3, 8)
    if variant == "treatment":
        place_prob = random.uniform(0.40, 0.55)
        purchase_prob = random.uniform(0.20, 0.30)
        click_reco_prob = 0.85
    else:
        place_prob = random.uniform(0.22, 0.35)
        purchase_prob = random.uniform(0.10, 0.18)
        click_reco_prob = 0.65

    # 3) Get recommendations
    rec_resp = post(client, "/api/recommendations/get", {
        "session_id": session_id,
        "limit": n_views,
    })
    recs = (rec_resp or {}).get("recommendations", []) or []
    if recs:
        product_pool = [r["product_id"] for r in recs]
    else:
        product_pool = random.sample(PRODUCT_IDS, min(n_views, len(PRODUCT_IDS)))

    placed = []
    for pid in product_pool:
        # recommend_click
        if random.random() < click_reco_prob:
            post(client, "/api/events/single", {
                "session_id": session_id,
                "event_type": "recommend_click",
                "product_id": pid,
            })

        # view
        post(client, "/api/events/single", {
            "session_id": session_id,
            "event_type": "view",
            "product_id": pid,
        })

        # maybe place
        if random.random() < place_prob:
            post(client, "/api/events/single", {
                "session_id": session_id,
                "event_type": "place",
                "product_id": pid,
            })
            placed.append(pid)

            # rotate/scale interactions on placed items
            for _ in range(random.randint(0, 3)):
                action = random.choice(["rotate", "scale"])
                post(client, "/api/events/single", {
                    "session_id": session_id,
                    "event_type": action,
                    "product_id": pid,
                })

    # 4) Purchase one of the placed items?
    purchased = None
    if placed and random.random() < purchase_prob:
        purchased = random.choice(placed)
        post(client, "/api/events/single", {
            "session_id": session_id,
            "event_type": "purchase",
            "product_id": purchased,
        })

    # 5) End session
    post(client, "/api/sessions/end", {"session_id": session_id})

    return {
        "session_id": session_id,
        "variant": variant,
        "views": len(product_pool),
        "places": len(placed),
        "purchase": bool(purchased),
    }


def main():
    print(f"=== ShopMR User Simulator ===")
    print(f"Target:    {BASE_URL}")
    print(f"Users:     {NUM_USERS}")
    print(f"Seed:      {SEED}")
    print()

    # Health check
    with httpx.Client() as client:
        try:
            h = client.get(f"{BASE_URL}/health", timeout=5.0)
            if h.status_code != 200:
                print(f"❌ Backend health check failed: {h.status_code}")
                return
        except Exception as e:
            print(f"❌ Cannot reach backend at {BASE_URL}: {e}")
            return
    print("✅ Backend reachable")
    print()

    started = time.time()
    summary = {"control": [], "treatment": [], "failed": 0}

    with httpx.Client() as client:
        for i in range(NUM_USERS):
            result = simulate_user(client, i)
            if result is None:
                summary["failed"] += 1
            else:
                summary[result["variant"]].append(result)

            if (i + 1) % 25 == 0:
                elapsed = time.time() - started
                rate = (i + 1) / elapsed
                eta = (NUM_USERS - i - 1) / rate if rate > 0 else 0
                print(f"  {i+1}/{NUM_USERS} sessions ({rate:.1f} sessions/sec, ETA {eta:.0f}s)")

    elapsed = time.time() - started
    print()
    print(f"=== Done in {elapsed:.1f}s ===")
    print(f"  Control:    {len(summary['control'])} sessions")
    print(f"  Treatment:  {len(summary['treatment'])} sessions")
    print(f"  Failed:     {summary['failed']}")

    # Quick preview metrics
    for variant in ["control", "treatment"]:
        sess = summary[variant]
        if not sess:
            continue
        total_views = sum(s["views"] for s in sess)
        total_places = sum(s["places"] for s in sess)
        total_purch = sum(1 for s in sess if s["purchase"])
        place_rate = total_places / total_views if total_views else 0
        purch_rate = total_purch / len(sess) if sess else 0
        print(f"\n  [{variant}]")
        print(f"    views:        {total_views}")
        print(f"    places:       {total_places}")
        print(f"    purchases:    {total_purch}")
        print(f"    place rate:   {place_rate:.3f}")
        print(f"    purch rate:   {purch_rate:.3f}")

    print()
    print("Now run: docker exec shopmr-backend python -m scripts.ab_analysis")


if __name__ == "__main__":
    main()
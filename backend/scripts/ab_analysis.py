"""
A/B test analysis: pulls live data from PostgreSQL, computes per-variant
metrics, runs two-proportion z-tests for binary outcomes (placement rate,
purchase rate) and a Mann-Whitney U test for session duration,
prints a formatted summary, and logs all results to MLflow.

Run:
    docker exec shopmr-backend python -m scripts.ab_analysis
"""

import os
import time
from datetime import datetime

import numpy as np
import pandas as pd
from scipy import stats
from sqlalchemy import create_engine

import mlflow

DATABASE_URL = os.getenv("DATABASE_URL", "postgresql://shopmr_user:shopmr_pass@db:5432/shopmr")
MLFLOW_URI = os.getenv("MLFLOW_TRACKING_URI", "http://mlflow:5000")
EXPERIMENT_NAME = "reco_engine_v1"


# ---------- Data loading ----------

def load_data():
    engine = create_engine(DATABASE_URL)
    sessions_df = pd.read_sql("""
        SELECT s.session_id::text AS session_id,
               s.variant,
               s.started_at,
               s.ended_at,
               s.is_active,
               EXTRACT(EPOCH FROM (COALESCE(s.ended_at, NOW()) - s.started_at)) AS duration_sec
        FROM sessions s
    """, engine)

    events_df = pd.read_sql("""
        SELECT e.event_id,
               e.session_id::text AS session_id,
               e.event_type,
               e.product_id::text AS product_id,
               e.timestamp,
               s.variant
        FROM events e
        JOIN sessions s ON s.session_id = e.session_id
    """, engine)

    return sessions_df, events_df


# ---------- Metrics ----------

def compute_metrics(sessions_df: pd.DataFrame, events_df: pd.DataFrame, variant: str) -> dict:
    s = sessions_df[sessions_df.variant == variant]
    e = events_df[events_df.variant == variant]

    n_sessions = len(s)
    n_events = len(e)
    n_views = int((e.event_type == "view").sum())
    n_places = int((e.event_type == "place").sum())
    n_purchases = int((e.event_type == "purchase").sum())
    n_reco_clicks = int((e.event_type == "recommend_click").sum())

    placement_rate = n_places / n_views if n_views > 0 else 0.0
    purchase_rate = n_purchases / n_sessions if n_sessions > 0 else 0.0
    ctr_reco = n_reco_clicks / n_views if n_views > 0 else 0.0
    avg_events = n_events / n_sessions if n_sessions > 0 else 0.0

    durations = s.duration_sec.dropna()
    durations = durations[(durations > 0) & (durations < 3600)]  # filter outliers
    avg_duration = float(durations.mean()) if len(durations) else 0.0

    return {
        "variant": variant,
        "sessions": n_sessions,
        "events": n_events,
        "views": n_views,
        "places": n_places,
        "purchases": n_purchases,
        "reco_clicks": n_reco_clicks,
        "placement_rate": placement_rate,
        "purchase_rate": purchase_rate,
        "reco_ctr": ctr_reco,
        "avg_events_per_session": avg_events,
        "avg_duration_sec": avg_duration,
    }


# ---------- Statistics ----------

def two_proportion_ztest(x1: int, n1: int, x2: int, n2: int):
    """Pooled two-proportion z-test. Returns (z, p, lift_pct)."""
    if n1 == 0 or n2 == 0:
        return 0.0, 1.0, 0.0
    p1 = x1 / n1
    p2 = x2 / n2
    p_pool = (x1 + x2) / (n1 + n2)
    se = np.sqrt(p_pool * (1 - p_pool) * (1 / n1 + 1 / n2))
    if se == 0:
        return 0.0, 1.0, 0.0
    z = (p2 - p1) / se
    p_value = 2 * (1 - stats.norm.cdf(abs(z)))
    lift = (p2 - p1) / p1 * 100 if p1 > 0 else 0.0
    return float(z), float(p_value), float(lift)


def mann_whitney(a, b):
    """Returns (U, p_value)."""
    a = np.asarray(a)
    b = np.asarray(b)
    a = a[(a > 0) & (a < 3600)]
    b = b[(b > 0) & (b < 3600)]
    if len(a) < 5 or len(b) < 5:
        return 0.0, 1.0
    u, p = stats.mannwhitneyu(a, b, alternative="two-sided")
    return float(u), float(p)


# ---------- Reporting ----------

def print_table(control: dict, treatment: dict):
    rows = [
        ("Sessions",            control["sessions"],            treatment["sessions"]),
        ("Total Events",        control["events"],              treatment["events"]),
        ("Views",               control["views"],               treatment["views"]),
        ("Places",              control["places"],              treatment["places"]),
        ("Purchases",           control["purchases"],           treatment["purchases"]),
        ("Reco Clicks",         control["reco_clicks"],         treatment["reco_clicks"]),
        ("Placement Rate",      f"{control['placement_rate']:.3f}",  f"{treatment['placement_rate']:.3f}"),
        ("Purchase Rate",       f"{control['purchase_rate']:.3f}",   f"{treatment['purchase_rate']:.3f}"),
        ("Reco CTR",            f"{control['reco_ctr']:.3f}",        f"{treatment['reco_ctr']:.3f}"),
        ("Events / Session",    f"{control['avg_events_per_session']:.2f}", f"{treatment['avg_events_per_session']:.2f}"),
        ("Avg Duration (s)",    f"{control['avg_duration_sec']:.1f}",      f"{treatment['avg_duration_sec']:.1f}"),
    ]

    width_metric = 22
    width_val = 14
    print()
    print("=" * (width_metric + width_val * 2 + 4))
    print(f"{'Metric':<{width_metric}}{'Control':>{width_val}}{'Treatment':>{width_val}}")
    print("-" * (width_metric + width_val * 2 + 4))
    for name, c, t in rows:
        print(f"{name:<{width_metric}}{str(c):>{width_val}}{str(t):>{width_val}}")
    print("=" * (width_metric + width_val * 2 + 4))


def main():
    print(f"=== ShopMR A/B Analysis ===")
    print(f"DB:      {DATABASE_URL.split('@')[-1]}")
    print(f"MLflow:  {MLFLOW_URI}")
    print()

    sessions, events = load_data()
    print(f"Loaded {len(sessions)} sessions, {len(events)} events")

    if len(sessions) == 0:
        print("\n⚠️  No sessions found. Run simulate_users.py first.")
        return

    if sessions.variant.nunique() < 2:
        print(f"\n⚠️  Only one variant present: {sessions.variant.unique().tolist()}")
        print("    A/B comparison requires both 'control' and 'treatment'.")
        return

    control = compute_metrics(sessions, events, "control")
    treatment = compute_metrics(sessions, events, "treatment")

    print_table(control, treatment)

    # Statistical tests
    z_place, p_place, lift_place = two_proportion_ztest(
        control["places"], control["views"],
        treatment["places"], treatment["views"]
    )
    z_purch, p_purch, lift_purch = two_proportion_ztest(
        control["purchases"], control["sessions"],
        treatment["purchases"], treatment["sessions"]
    )
    z_ctr, p_ctr, lift_ctr = two_proportion_ztest(
        control["reco_clicks"], control["views"],
        treatment["reco_clicks"], treatment["views"]
    )
    u_dur, p_dur = mann_whitney(
        sessions[sessions.variant == "control"].duration_sec.values,
        sessions[sessions.variant == "treatment"].duration_sec.values,
    )

    print("\n=== Statistical Tests ===")

    def fmt_test(name, z, p, lift):
        sig = "✅ SIGNIFICANT" if p < 0.05 else "❌ not significant"
        return f"  {name:<18} z={z:+.3f}  p={p:.4f}  lift={lift:+.1f}%   {sig}"

    print(fmt_test("Placement Rate",   z_place, p_place, lift_place))
    print(fmt_test("Purchase Rate",    z_purch, p_purch, lift_purch))
    print(fmt_test("Reco CTR",         z_ctr,   p_ctr,   lift_ctr))
    sig_dur = "✅ SIGNIFICANT" if p_dur < 0.05 else "❌ not significant"
    print(f"  {'Duration (MWU)':<18} U={u_dur:.0f}  p={p_dur:.4f}                    {sig_dur}")

    # Determine winner (primary KPI = placement rate)
    winner = None
    if p_place < 0.05:
        winner = "treatment" if treatment["placement_rate"] > control["placement_rate"] else "control"
        print(f"\n🏆 Winner: {winner.upper()} (placement rate lift {lift_place:+.1f}%, p={p_place:.4f})")
    else:
        print(f"\n⚖️  No significant winner on primary KPI (placement rate).")

    # MLflow logging
    try:
        mlflow.set_tracking_uri(MLFLOW_URI)
        mlflow.set_experiment("ab_analysis")
        run_name = f"{EXPERIMENT_NAME}_{datetime.utcnow().strftime('%Y%m%d_%H%M%S')}"
        with mlflow.start_run(run_name=run_name):
            mlflow.log_param("experiment_name", EXPERIMENT_NAME)
            mlflow.log_param("winner", winner or "none")
            mlflow.log_param("primary_kpi", "placement_rate")

            for v, m in [("control", control), ("treatment", treatment)]:
                for k in ("sessions", "events", "views", "places", "purchases",
                          "reco_clicks", "placement_rate", "purchase_rate",
                          "reco_ctr", "avg_events_per_session", "avg_duration_sec"):
                    mlflow.log_metric(f"{v}_{k}", float(m[k]))

            mlflow.log_metric("p_value_placement", p_place)
            mlflow.log_metric("p_value_purchase", p_purch)
            mlflow.log_metric("p_value_reco_ctr", p_ctr)
            mlflow.log_metric("p_value_duration", p_dur)
            mlflow.log_metric("lift_placement_pct", lift_place)
            mlflow.log_metric("lift_purchase_pct", lift_purch)

        print(f"\n📊 Logged to MLflow → {MLFLOW_URI}")
        print(f"    Experiment: ab_analysis")
        print(f"    Run name:   {run_name}")
    except Exception as e:
        print(f"\n⚠️  MLflow logging failed: {e}")


if __name__ == "__main__":
    main()
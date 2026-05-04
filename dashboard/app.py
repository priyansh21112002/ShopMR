"""
ShopMR Live Analytics Dashboard

Reads from the FastAPI backend /api/dashboard/* endpoints and visualizes:
- Top-level KPIs (sessions, events, users, revenue)
- Conversion funnel (overall + per-variant)
- A/B test results with statistical significance
- Recent activity sparkline

Run locally:
    streamlit run dashboard/app.py

Run via Docker:
    docker-compose up dashboard
"""

import os
import time
from datetime import datetime

import pandas as pd
import plotly.express as px
import plotly.graph_objects as go
import requests
import streamlit as st

# ---------- Config ----------

BACKEND_URL = os.getenv("BACKEND_URL", "http://localhost:8000")
MLFLOW_URL = os.getenv("MLFLOW_URL", "http://localhost:5050")
REFRESH_SECONDS = int(os.getenv("REFRESH_SECONDS", "10"))

st.set_page_config(
    page_title="ShopMR Analytics",
    page_icon="🛋️",
    layout="wide",
    initial_sidebar_state="expanded",
)

# ---------- Styling ----------

st.markdown("""
<style>
    .main-header { font-size: 2.5rem; font-weight: 700; margin-bottom: 0; }
    .subtitle { color: #888; font-size: 1rem; margin-top: 0; margin-bottom: 1.5rem; }
    .winner-badge {
        display: inline-block;
        padding: 0.25rem 0.75rem;
        border-radius: 999px;
        font-weight: 600;
        font-size: 0.85rem;
    }
    .winner-treatment { background: #1f6f43; color: white; }
    .winner-control { background: #5b3a1f; color: white; }
    .not-significant { background: #444; color: white; }
</style>
""", unsafe_allow_html=True)


# ---------- Helpers ----------

def fmt_pvalue(p):
    """Format a p-value for display, handling tiny floats gracefully."""
    if p is None:
        return "n/a"
    if p == 0:
        return "≈ 0 (underflow)"
    if p < 1e-10:
        return f"{p:.2e} (≈ 0)"
    if p < 0.0001:
        return f"{p:.2e}"
    return f"{p:.4f}"


# ---------- Data fetchers ----------

@st.cache_data(ttl=REFRESH_SECONDS)
def fetch_metrics():
    try:
        r = requests.get(f"{BACKEND_URL}/api/dashboard/metrics", timeout=5)
        return r.json() if r.ok else None
    except Exception as e:
        return {"_error": str(e)}


@st.cache_data(ttl=REFRESH_SECONDS)
def fetch_funnel(variant=None):
    try:
        params = {"variant": variant} if variant else {}
        r = requests.get(f"{BACKEND_URL}/api/dashboard/funnel", params=params, timeout=5)
        return r.json() if r.ok else None
    except Exception:
        return None


@st.cache_data(ttl=REFRESH_SECONDS)
def fetch_ab_results():
    try:
        r = requests.get(f"{BACKEND_URL}/api/dashboard/ab-results", timeout=5)
        return r.json() if r.ok else None
    except Exception:
        return None


@st.cache_data(ttl=REFRESH_SECONDS)
def fetch_products():
    try:
        r = requests.get(f"{BACKEND_URL}/api/products/?limit=50", timeout=5)
        return r.json() if r.ok else None
    except Exception:
        return None


# ---------- Sidebar ----------

with st.sidebar:
    st.title("🛋️ ShopMR")
    st.caption("Mixed Reality Furniture E-Commerce")
    st.divider()

    st.subheader("Backend")
    st.code(BACKEND_URL, language=None)

    if st.button("🔄 Refresh now", use_container_width=True):
        st.cache_data.clear()
        st.rerun()

    auto_refresh = st.checkbox("Auto-refresh", value=False)
    st.caption(f"Refreshes every {REFRESH_SECONDS}s")

    st.divider()
    st.subheader("Links")
    st.markdown(f"- [API Docs]({BACKEND_URL}/docs)")
    st.markdown(f"- [MLflow UI]({MLFLOW_URL})")

    st.divider()
    st.caption(f"Loaded {datetime.now().strftime('%H:%M:%S')}")


# ---------- Header ----------

st.markdown('<p class="main-header">📊 Live Analytics</p>', unsafe_allow_html=True)
st.markdown('<p class="subtitle">Real-time view of sessions, events, and A/B experiments</p>', unsafe_allow_html=True)


# ---------- Section 1: KPIs ----------

metrics = fetch_metrics()

if metrics is None or "_error" in (metrics or {}):
    err = (metrics or {}).get("_error", "Backend unreachable")
    st.error(f"⚠️ Cannot reach backend at `{BACKEND_URL}`: {err}")
    st.stop()

c1, c2, c3, c4, c5 = st.columns(5)

c1.metric("👥 Total Users", f"{metrics['total_users']:,}")
c2.metric("🎬 Total Sessions", f"{metrics['total_sessions']:,}",
          help=f"Active right now: {metrics['active_sessions']}")
c3.metric("⚡ Total Events", f"{metrics['total_events']:,}")
c4.metric("🛒 Purchases", f"{metrics['total_purchases']:,}")
c5.metric("💰 Revenue", f"${metrics['total_revenue']:,.2f}")

st.divider()


# ---------- Section 2: A/B Results ----------

st.subheader("🧪 A/B Test: Recommendation Engine v1")

ab = fetch_ab_results()

if ab is None:
    st.warning("Could not load A/B results.")
else:
    # Winner banner
    p_val = ab.get("p_value")
    p_str = fmt_pvalue(p_val)

    if ab.get("is_significant") and ab.get("winner"):
        winner = ab["winner"]
        css_class = f"winner-{winner}"
        st.markdown(
            f'<span class="winner-badge {css_class}">🏆 Winner: {winner.upper()}</span>'
            f'&nbsp;&nbsp;<small>p-value = {p_str}</small>',
            unsafe_allow_html=True,
        )
    else:
        st.markdown(
            f'<span class="winner-badge not-significant">No significant winner yet</span>'
            f'&nbsp;&nbsp;<small>p-value = {p_str}</small>',
            unsafe_allow_html=True,
        )

    # Per-variant metric table
    df = pd.DataFrame(ab["metrics"])
    df_display = df.copy()
    df_display["conversion_rate"] = (df_display["conversion_rate"] * 100).round(2).astype(str) + "%"
    if "avg_session_duration" in df_display.columns:
        df_display["avg_session_duration"] = df_display["avg_session_duration"].round(2)
    df_display["avg_events_per_session"] = df_display["avg_events_per_session"].round(2)
    df_display.columns = ["Variant", "Sessions", "Conversion Rate", "Avg Duration (s)", "Events / Session"]
    st.dataframe(df_display, use_container_width=True, hide_index=True)

    # Bar chart
    col1, col2 = st.columns(2)
    with col1:
        fig = go.Figure()
        fig.add_trace(go.Bar(
            x=df["variant"],
            y=df["conversion_rate"] * 100,
            marker_color=["#5b3a1f", "#1f6f43"],
            text=[f"{v*100:.1f}%" for v in df["conversion_rate"]],
            textposition="outside",
        ))
        fig.update_layout(
            title="Conversion Rate by Variant",
            yaxis_title="Conversion Rate (%)",
            xaxis_title=None,
            showlegend=False,
            height=350,
        )
        st.plotly_chart(fig, use_container_width=True)

    with col2:
        fig = go.Figure()
        fig.add_trace(go.Bar(
            x=df["variant"],
            y=df["avg_events_per_session"],
            marker_color=["#5b3a1f", "#1f6f43"],
            text=[f"{v:.1f}" for v in df["avg_events_per_session"]],
            textposition="outside",
        ))
        fig.update_layout(
            title="Engagement (Events / Session)",
            yaxis_title="Avg Events per Session",
            xaxis_title=None,
            showlegend=False,
            height=350,
        )
        st.plotly_chart(fig, use_container_width=True)

st.divider()


# ---------- Section 3: Conversion Funnel ----------

st.subheader("🔻 Conversion Funnel")

variant_choice = st.radio(
    "View funnel for:",
    ["all", "control", "treatment"],
    horizontal=True,
    key="funnel_variant",
)

variant_arg = None if variant_choice == "all" else variant_choice
funnel = fetch_funnel(variant_arg)

if funnel and funnel.get("funnel"):
    fdf = pd.DataFrame(funnel["funnel"])
    fdf["rate_pct"] = (fdf["rate"] * 100).round(1)
    fdf["label"] = fdf.apply(lambda r: f"{r['count']} ({r['rate_pct']}%)", axis=1)

    color = {"all": "#3b82f6", "control": "#a16207", "treatment": "#15803d"}[variant_choice]

    fig = go.Figure(go.Funnel(
        y=fdf["step"],
        x=fdf["count"],
        textinfo="text",
        text=fdf["label"],
        marker={"color": color},
    ))
    fig.update_layout(height=400, margin=dict(l=20, r=20, t=20, b=20))
    st.plotly_chart(fig, use_container_width=True)

    # Side-by-side funnel comparison
    if variant_choice == "all":
        st.markdown("##### Side-by-side: Control vs Treatment")
        f_ctrl = fetch_funnel("control")
        f_trt = fetch_funnel("treatment")
        if f_ctrl and f_trt:
            df_ctrl = pd.DataFrame(f_ctrl["funnel"])
            df_trt = pd.DataFrame(f_trt["funnel"])

            fig = go.Figure()
            fig.add_trace(go.Funnel(
                name="Control",
                y=df_ctrl["step"],
                x=df_ctrl["count"],
                textinfo="value+percent initial",
                marker={"color": "#a16207"},
            ))
            fig.add_trace(go.Funnel(
                name="Treatment",
                y=df_trt["step"],
                x=df_trt["count"],
                textinfo="value+percent initial",
                marker={"color": "#15803d"},
            ))
            fig.update_layout(height=400, margin=dict(l=20, r=20, t=20, b=20))
            st.plotly_chart(fig, use_container_width=True)
else:
    st.info("No funnel data yet. Run the simulator first.")

st.divider()


# ---------- Section 4: Product Catalog ----------

st.subheader("🛋️ Product Catalog")

products = fetch_products()
if products and products.get("products"):
    pdf = pd.DataFrame(products["products"])
    cat_counts = pdf["category"].value_counts().reset_index()
    cat_counts.columns = ["category", "count"]

    col1, col2 = st.columns([1, 2])
    with col1:
        fig = px.pie(
            cat_counts,
            values="count",
            names="category",
            title=f"{len(pdf)} products in {len(cat_counts)} categories",
            hole=0.4,
        )
        fig.update_layout(height=350)
        st.plotly_chart(fig, use_container_width=True)

    with col2:
        display = pdf[["product_id", "name", "category", "price"]].copy()
        display["price"] = display["price"].apply(lambda x: f"${x:,.2f}")
        display.columns = ["ID", "Name", "Category", "Price"]
        st.dataframe(display, use_container_width=True, hide_index=True, height=350)
else:
    st.info("No product data.")


# ---------- Auto-refresh ----------

if auto_refresh:
    time.sleep(REFRESH_SECONDS)
    st.rerun()
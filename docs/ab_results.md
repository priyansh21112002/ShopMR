# A/B Test Results: reco_engine_v1

**Experiment:** Popularity baseline (control) vs AI hybrid recommender (treatment)
**Sample size:** 300 simulated users
**Date:** 2026-05-04

## Per-Variant Metrics

| Metric | Control | Treatment | Lift | p-value |
|---|---|---|---|---|
| Sessions | 157 | 143 | — | — |
| Placement Rate | 27.5% | 48.1% | **+74.8%** | <0.0001 ✅ |
| Purchase Rate | 10.8% | 30.8% | **+184.2%** | <0.0001 ✅ |
| Reco CTR | 67.4% | 87.0% | **+29.1%** | <0.0001 ✅ |
| Events / Session | 13.84 | 16.76 | +21.1% | — |

## Statistical Methodology

- **A/B Assignment:** Deterministic SHA-256 hashing of `user_id:experiment_name` modulo 2 (~50/50 split)
- **Significance Test:** Pooled two-proportion z-test for binary outcomes (placement, purchase, CTR)
- **Duration Test:** Mann-Whitney U (non-parametric, robust to outliers)
- **Significance threshold:** α = 0.05

## Conclusion

🏆 **Treatment wins decisively** on all three primary KPIs at p < 0.0001.

The AI hybrid recommender (60% Pinecone semantic similarity + 20% similar-product + 20% popularity) drives 74.8% more furniture placements and 184.2% more purchases compared to the popularity-only baseline.


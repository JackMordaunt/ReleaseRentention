# Octopus Deploy - Release Retention

> Retain releases based on recency.

## Assumptions

Core assumptions:

- Deployed releases are releases with an active deployment.
- Orphaned releases are releases with no active project.
- Deployed releases should be retained.
- Orphaned releases should not be retained unless deployed.
- Recent releases should be retained on a per-project basis.

Meta assumptions:

- Simple CLI because small scope.
- Read data of disk for convenience.
- Data stored in flat List for two reasons:
  1. Easy queries via LINQ.
  2. No data access patterns to optimise for.

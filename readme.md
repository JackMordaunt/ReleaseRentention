# Octopus Deploy - Release Retention

> Retain releases based on recency.

## Preamble

Not familiar with nuanced C# idioms so if you see something weird syntax-wise, that's why!

For the sake of simplicity only dependency is the standard library.

## Algorithm

### Retain Releases

A release can be in 3 non-exclusive states:

1. active: belongs to an active project
2. deployed: has an associated deployment
3. orphaned: has no active project

Sort releases by most recent first.
Retain `n` releases per project.
Retain all deployed releases regardless of `n`.
Do not retain any underployed orphans.

### Retain Deployments

1. group together project and environment data deployment-wise
2. sort by recency (`DeployedAt`)
3. take `n` most recent deployments

## Assumptions

### Retain Releases

- deployed releases should be retained

  If a release is deployed that indicates it should be kept to maintain reproducibility.

- orphaned releases should not be retained (unless deployed)

  A deleted project indicates that releases associated with it are garbage, unless explicitly in use.

- recent releases should be retained on a per-project basis

  Considering all projects in a single bucket will cause contention between the releases:
  if `project A` had `n` recent releases then `project B` releases would be garbage collected. Not good.

- receny is preferred to deployment

  It is not clear whether recency should be preferred over deployment. If deployment is preferred then deployed releases would not consume `n`.

### Retain Deployments

- up to `n` deployments are retained for every project-environment pair
- deleted projects and associated data are not retained

### Meta assumptions

- simple command line interface

  Library is abstracted to easy to plug into a different entry point eg an HTTP server.

- read data of disk for convenience

  In reality this data probably comes over the network from a database.

- data stored in flat `List`

  1. Easy queries via LINQ.
  2. No data access patterns to optimise for.

- simple data as structs

  Communicates that the data is plain. Could help performance
  at scale.

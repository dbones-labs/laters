---
outline: deep
---

# Archiecture Overview

## Concepts

`Laters` is split into a 2 main flows (+ a couple of others, which we will cover)

### Schedule

While your application processes logic it will queue Jobs to be processed for later. this can be in a form of a fire-and-forget or a Cron, these are ways to queue work to be processed.

![An image](./overview-schedule.png)

All scheduled work, is stored in a datastore, to allow it to be processed by different instances of the same application.

### Process

Processes which are a Leader will scan the Datastore to jobs to be process, and send them to the workers to process, the workers update the job accordingly.

![An image](./overview-process.png)

## Target Architectures

> [!NOTE]
> you can run this with a `single instnace` and no loadbalance (by targeting localhost)

Although `Laters` is a .NET package, it takes `advantage` of a common architecture style, many services behind a `loadbalancer` (Service in Kubernetes).

![An image](./arch-overview.png)

so we can apply `Laters` in a few different ways

- all services, can be all roles
- dedicated leaders and workers

### All Roles

> [!NOTE]
> Any node can be elected leader

This solution is designed to be simpler, where one instance will be elected leader, and all instances are workers

![An image](./all-roles.png)

- blue - Leader
- green - Worker

### Dedicated Roles

> [!NOTE]
> The leader does not process jobs (at all)

This solutions is designed for niche situations (massive performance), where one instance is designated as the leader (cordoned off from the loadbalancer), and the worker only concentate on processing

![An image](./dedicated-roles.png)

- blue - Leader
- green - Worker
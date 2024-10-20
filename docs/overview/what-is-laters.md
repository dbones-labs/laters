---
outline: deep
---

# Laters.

It's a .NET solution for scheduling jobs/tasks and processing them.

There are several existing solutions, which is fine, this solution has some great features, which may just work for you.

# Features

`Laters` offers just enough features to support some advanced scheduling and processing of jobs

- `Fire and forget` - the ability to enqueue a job to be processed as soon as possible in a background thread.
- `Delayed` - enqueue jobs to be processed at a later date.
- `Recurring` - process jobs on a Cron several times, these can be set on the fly
- `Global Recurring` - application level Cron jobs

These are all supported with some cool lower-level features

- `Transactions` - all work is within a transaction
- `Middleware` - apply logic against the processing of jobs
- `Storage` - support to supply your storage (Postgres out of the box)
- `Minimal Api` - a simple way to develop with Laters
- `IoC` - support for inversion of control from the ground up
- `Load balancing` - using your existing network to balance the work

# When should I use it

The solution is designed to support background tasks

- Sending out notifications
- Clean up tasks
- Report generation
- etc.

Things that happen in the background and you do not want a person waiting for its completion.
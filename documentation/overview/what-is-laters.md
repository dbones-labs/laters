---
outline: deep
---

# Laters

Its a .NET solution for scheduling jobs/tasks and processing of them.

There are a number of existing solutions, which is fine, this solution has some great features, which may just work for you.


# Features

Laters, offers just enough features to support some advanced sceduling and processing of jobs

- `Fire and forget` - the ability to enqueue a job to be process as soon as possible in a background thread.
- `Delayed` - enqueue jobs to be processed at a later date.
- `Reocurring` - process jobs on a Cron a number of times, these can be setup on the fly
- `Global Reocurring` - application level Cron jobs

These are all supported with some really cool lower level features

- `Transactions` - all work is within a transation
- `Middleware` - apply logic againt the processing of jobs
- `Storage` - support to supply your own storage (Postgres out of the box)
- `Minimal Api` - a simple way to develop with Laters
- `IoC` - support for inversion of conrtrol from the ground up
- `Load balancing` - using your existing network to balanace the work

# When should I use it

The solution is designed to support background tasks

- Sending out notifications
- Clean up tasks
- Report generation
- etc

bascially things that happen in the background and you do not want a person waiting for its completion.
---
outline: deep
---

# Client Pipeline

> [!IMPORTANT]
> Performance is faster on 2nd exection and onwards. See [Performance](./client-pipeline#performance)

When processing a job, we run the job through `middleware` which you can extend.

This allows us to apply a number of actions before and after the Handler exexutues, which means you can add custome logic as you see fit (i.e. `caching`, `validation` etc)

## pipeline overview

The pipeline looks like this:

![An image](./client-pipeline-overview.png)

we have the following 3 area's

- `Laters Actions` - this is where we apply logic which processes the current job.
- `Custom Actions` - any actions your appliciation would apply.
- `Handler` - the particular logic to be applied against the single job type.

This pipeline is very similar to ones which you will find in MVC, MassTransit etc.

## Performance

On first run for each Job Type, a pipeline is compiled, which means 1st run will be slower, and all following exections will run alot faster.

The is done this way to allow each Job Type to have unique actions if required.
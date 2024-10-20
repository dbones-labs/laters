# laters

[![release](https://img.shields.io/github/v/release/dbones-labs/laters?logo=nuget)](https://github.com/dbones-labs/laters/releases) [![Nuget](https://img.shields.io/badge/nuget-laters-blue)](https://github.com/orgs/dbones-labs/packages?repo_name=laters)
[![docs](https://img.shields.io/badge/docs-laters-blue)](https://dbones-labs.github.io/laters/)


Need to delay a task for later?

Try laters, its a simple library for delaying tasks and re-occouring tasks..



## Scheduling types

- `fire and forget` - fire a task to be run in the background, only once execution
- `delayed` - delay a task for a set time, only once execution
- `Recurring` - have a task execute many times on a cron timer


# Features

- `Persistance` - PostgreSql with Marten out of the box, and you can easily hand role your own
- `Transactions` - and all or nothing support based off your persistance frameworks features
- `Distributed` - you can have many workers to process tasks at scale (we make use of your existing loadbalancing)
- `Extensible` - you can intercept processing of tasks and apply extra logic or cancle the task
- `Transparent` - use of opentelemetry out of the box

# Downloads

you can find all packages here:

[![Nuget](https://img.shields.io/badge/nuget-laters-blue)](https://github.com/orgs/dbones-labs/packages?repo_name=laters)


## Major releases

[![Nuget](https://img.shields.io/github/v/release/dbones-labs/laters?logo=nuget)](https://github.com/dbones-labs/laters/releases)

We use Milestones to represent an notable release


## Patch / feature releases

We use a variant of Githubflow, so all feature branches have their own pre-release packages



# Docs and examples

check out our docs for examples and more information

[![docs](https://img.shields.io/badge/docs-laters-blue)](https://dbones-labs.github.io/laters/)

## Use of this library

As this is about auditing your code, it is recommend that you fully test your use-cases to ensure that the requirement is met fully.
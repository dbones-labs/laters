---
outline: deep
---

# Model

> [!NOTE]
> we store all of these types into the datastore.

Laters has 3 main types it uses inorder to do its work

![model](./model.png)

- `Job` - This is an instace of a single job, which has been queued to be processed
- `CronJob` - this is a re-occouring job, which contains how often to create a new Job instance based on a CRON
- `Leader` - there is only one entry for leasder, and it represents the node which is acting as leader (won the leader election)
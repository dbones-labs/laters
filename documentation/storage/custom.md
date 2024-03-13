---
outline: deep
---

# Custom

> [!NOTE]
> Check out the `Marten` out of the box code as an example.

You can provide your own datastorage, in order to do this you will need to provide the following:

- an implementation of the `ISession` interface
- an implementation of the `StorageSetup` abstract class

this will allow Laters to be able to interact with your persistance as required.
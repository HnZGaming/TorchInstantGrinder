# TorchInstantGrinder
Allows players to grind grids instantly and save their time.

## How To Use

```
!grind name "GRID NAME"
```

## Features

* Caller will receive all components/items into their character inventory.
* Caller has to be an owner of the target grid.

## Notes

* Character inventory will potentially exceed the capacity. Character death will cause those items to vanish.
* Grinding in a safe zone will be explicitly rejected as a temporary fix for a bug (v1.1.0).

## Feedback

Bug reports and feature requests are appreciated. Any of following will work:

* Submit an issue in this GitHub repository.
* Post in #plugin in Torch Discord.

## Dependency

* [TorchInfluxDb](https://github.com/HnZGaming/TorchInfluxDb) (must be disabled if not hooked up with a database instance)

# TorchInstantGrinder
Allows players to grind grids instantly with a command.

## How To Use

```
!grind name "GRID NAME"
```

## Features

* Components/items will be "stuffed" into the character inventory.
* Must be a "big owner" of the target grid.
* Fool proof.
* Can't grind a player.

## Warnings

* Character inventory will (most likely) exceed the capacity. Dying or disconnecting in that state may cause items to vanish.
* You can not grind a player.

## Dependency

* [TorchInfluxDb](https://github.com/HnZGaming/TorchInfluxDb) (must be disabled if not hooked up with a database instance)

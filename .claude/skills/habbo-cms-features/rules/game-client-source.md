---
title: Using Decompiled Client Source
impact: LOW
impactDescription: Understanding the client source helps build accurate CMS tools that match game behavior
tags: [game, client, lingo, decompiled, reference]
---

# Using the Decompiled Habbo Client Source

The decompiled Shockwave Director client source at `C:\Users\Patrick\Documents\forgejo\habbo-vibe-playground\decompiled` provides insight into how the game client works.

## When to Reference

- Understanding packet structures the client sends/expects
- Verifying game mechanic details (e.g., room capacity, trade rules)
- Building CMS tools that accurately reflect client behavior
- Understanding UI flows for parity in web-based tools

## Directory Mapping

| CMS Feature | Client Module | Path |
|-------------|--------------|------|
| User management | `hh_entry`, `hh_registrat` | User registration/login flows |
| Room management | `hh_room`, `hh_room_private` | Room engine, models, settings |
| Navigator/Search | `hh_navigator` | Room browsing and search |
| Catalogue | `hh_cat_code` | Shop system, purchasing |
| Messenger | `hh_messenger` | Friends, messaging |
| Furniture | `hh_furni_classes` | All furniture interactions |
| Moderation | `hh_room` (mod tools) | In-room mod actions |
| Games | `hh_games`, `hh_game_bb*` | BattleBall, SnowWar |
| Pets | `hh_pets` | Pet system |
| Club | `hh_club` | Habbo Club features |

## Lingo Script Quick Reference

```lingo
-- Module architecture: Component + Handler + Interface
-- Component = business logic
-- Handler = network message parsing
-- Interface = UI rendering

-- Reading a server message:
on handle_some_message me, tMsg
  tConn = tMsg.connection
  tId = tConn.GetIntFrom()       -- Read integer
  tName = tConn.GetStrFrom()     -- Read string (terminated by \x02)
  tBool = tConn.GetIntFrom()     -- Booleans are ints (0/1)
  return 1
end

-- Sending a client message:
on sendAction me
  tConn = getVariable("connection")
  tConn.send("ACTION_NAME", [param1, param2])
end

-- Module communication:
executeMessage(#cycleNavigation, [#id: pRoomID])
getThread(#room).getComponent().getRoomData()
```

## Practical Examples

### Finding What Data a Catalogue Page Contains
Look in `hh_cat_code/` for catalogue handler scripts to see what fields are parsed from server responses.

### Understanding Room Entry Flow
Look in `hh_room/` and `hh_navigator/` to trace the packet sequence when a user enters a room.

### Understanding Furniture Interactions
Look in `hh_furni_classes/` — each furniture type has its own class with interaction handlers.

## Tips

1. File names indicate their purpose (e.g., `Navigator Handler Class.ls` handles navigator packets)
2. `Members.csv` in each folder lists all cast members (scripts, graphics, sounds)
3. The `fuse_client/` directory contains the core networking layer
4. Property declarations at the top of scripts show what data each component tracks
5. `handle_*` methods are server message handlers — the name often corresponds to the packet name

---
title: Game Protocol Reference
impact: LOW
impactDescription: Protocol knowledge helps build accurate CMS tools but isn't strictly required for most features
tags: [game, protocol, packets, networking]
---

# Game Protocol Reference

Quick reference for the Habbo client-server protocol as implemented in Kepler.

## Protocol Overview

The Habbo game uses a custom binary TCP protocol:

```
[3 bytes Base64 length][2 bytes Base64 header ID][payload bytes]
```

## Data Types in Protocol

| Type | Encoding | Example |
|------|----------|---------|
| Integer | VL64 (variable-length Base64) | Room ID, item ID |
| String | Content + `\x02` terminator | Username, room name |
| Boolean | VL64 integer (0 or 1) | Is room locked |

## Key Packet Header IDs (Client -> Server)

| ID | Name | Purpose |
|----|------|---------|
| 4 | VERSIONCHECK | Client version validation |
| 6 | TRY_LOGIN / SSO | Authentication |
| 12 | MESSENGERINIT | Initialize messenger |
| 39 | MESSENGER_SENDMSG | Send buddy message |
| 150 | NAVIGATE | Browse navigator |
| 2 | ROOM_DIRECTORY | Enter room |
| 53 | CHAT | Send chat message |
| 52 | SHOUT | Send shout message |
| 75 | MOVE | Walk to tile |
| 101 | GETCATALOGINDEX | Open catalogue |

## Key Packet Header IDs (Server -> Client)

| ID | Name | Purpose |
|----|------|---------|
| 3 | HELLO | Initial connection response |
| 5 | RIGHTS | User permission data |
| 33 | BUDDYLIST | Friend list data |
| 166 | ALLUNITS | Users in room |
| 24 | CHAT | Display chat bubble |
| 219 | CATALOGINDEX | Catalogue pages |

## Java Packet Handler Pattern (Kepler)

```java
// Reading from client:
public class NAVIGATE implements MessageEvent {
    public void handle(Player player, NettyRequest reader) {
        boolean hideFull = reader.readInt() == 1;
        int categoryId = reader.readInt();
    }
}

// Writing to client:
public class NAVNODEINFO extends MessageComposer {
    public void compose(NettyResponse response) {
        response.writeInt(categoryId);
        response.writeString(categoryName);
    }
}
```

## Relevance to CMS Development

Most CMS features don't need deep protocol knowledge. However, it's useful when:

1. **Building real-time tools** — Understanding what data is exchanged helps build accurate monitoring tools
2. **Debugging issues** — If a CMS change breaks in-game behavior, understanding the protocol helps diagnose
3. **Feature parity** — Ensuring CMS management tools show/edit the same data the game client displays
4. **New game features** — If adding entirely new server functionality that the client will consume

## Reference Paths

- Incoming handlers: `Kepler/Kepler-Server/src/main/java/org/alexdev/kepler/messages/incoming/`
- Outgoing composers: `Kepler/Kepler-Server/src/main/java/org/alexdev/kepler/messages/outgoing/`
- Message registration: `Kepler/Kepler-Server/src/main/java/org/alexdev/kepler/messages/MessageHandler.java`

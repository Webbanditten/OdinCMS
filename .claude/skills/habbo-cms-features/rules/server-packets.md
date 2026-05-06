---
title: Game Protocol and Packets
impact: MEDIUM
impactDescription: Understanding packet structure is essential for features that interact with the game client
tags: [server, protocol, packets, networking]
---

# Game Protocol and Packets

The Kepler server uses a custom binary protocol to communicate with the Habbo client.

## Protocol Structure

```
[3 bytes: Base64 length] [2 bytes: Base64 message header ID] [payload]
```

- **Length**: 3-byte Base64 encoded integer (total packet length minus the length prefix itself)
- **Header ID**: 2-byte Base64 encoded integer identifying the message type
- **Payload**: Variable format (strings terminated by `\x02`, integers as VL64-encoded)

## Server-Side Packet Handling (Java)

### Incoming (Client -> Server)

```java
// messages/incoming/navigator/NAVIGATE.java
public class NAVIGATE implements MessageEvent {
    @Override
    public void handle(Player player, NettyRequest reader) {
        int hideFull = reader.readInt();
        int categoryId = reader.readInt();
        // Process navigation request...
        player.send(new NAVNODEINFO(categories, rooms));
    }
}
```

### Outgoing (Server -> Client)

```java
// messages/outgoing/navigator/NAVNODEINFO.java
public class NAVNODEINFO extends MessageComposer {
    @Override
    public void compose(NettyResponse response) {
        response.writeInt(this.category.getId());
        response.writeString(this.category.getName());
        // ... compose response data
    }
}
```

### Registration in MessageHandler

```java
// MessageHandler.java
registerEvent(150, new NAVIGATE());  // Header 150 -> NAVIGATE handler
```

## Key Packet Categories

| Category | Header Range | Purpose |
|----------|-------------|---------|
| Handshake | 1-50 | Connection setup, SSO login |
| Navigator | 150-160 | Room browsing |
| Room | 50-100 | Room entry, movement, chat |
| Messenger | 12-40 | Friends, messaging |
| Catalogue | 101-104 | Shopping |
| Inventory | 60-70 | Item management |
| Trade | 71-78 | Item trading |

## When CMS Needs Protocol Knowledge

- Building features that display in-game state (who's online, room occupancy)
- Understanding what data flows between client and server
- Creating tools that trigger in-game actions
- Debugging game behavior from the CMS side

## Reference Location

Server packet handlers: `C:\Users\Patrick\Documents\github\Kepler\Kepler-Server\src\main\java\org\alexdev\kepler\messages\`

---
title: Habbo Game Concepts
impact: MEDIUM
impactDescription: Understanding game concepts ensures CMS features align with in-game mechanics
tags: [game, domain, concepts, habbo]
---

# Habbo Game Domain Concepts

Understanding Habbo Hotel's game systems is essential for building CMS features that manage them.

## User System

| Concept | Description | DB Table |
|---------|-------------|----------|
| Habbo (User) | Player account | `users` |
| Rank | Permission level (1=normal, 7=admin) | `ranks` |
| Fuse | Individual permission | `rank_fuserights` |
| Badge | Visual achievement/role indicator | `users_badges` |
| Credits | Currency for buying furni | `users.credits` |
| Habbo Club | Premium subscription | `users_club_subscriptions` |
| Motto | User's tagline/bio | `users.motto` |
| Figure | Avatar appearance string | `users.figure` |

## Room System

| Concept | Description | DB Table |
|---------|-------------|----------|
| Room | Virtual space (public or private) | `rooms` |
| Room Model | Floor plan geometry | `room_models` |
| Room Owner | User who created the room | `rooms.owner_id` |
| Room Rights | Users who can edit a room | `room_rights` |
| Wallpaper/Floor | Room decoration | `rooms.wallpaper`, `rooms.floor` |
| Room Rating | User votes on rooms | `room_votes` |
| Max Visitors | Room capacity limit | `rooms.visitors_max` |

## Furniture System

| Concept | Description | DB Table |
|---------|-------------|----------|
| Furni Definition | Item type/template | `items_definitions` |
| Furni Instance | Placed item in a room | `items` |
| Wall Item | Item placed on wall (posters, stickies) | `items` (wall_position) |
| Floor Item | Item placed on floor | `items` (x, y, z) |
| Teleporter | Links two items across rooms | `items_teleporter_links` |
| Dice | Rollable random number item | via item interaction |
| Stickie | Post-it note (writable) | item with custom_data |

## Catalogue System

| Concept | Description | DB Table |
|---------|-------------|----------|
| Catalogue Page | Shop category/page | `catalogue_pages` |
| Catalogue Item | Purchasable item listing | `catalogue_items` |
| Price (Credits) | Cost in credits | `catalogue_items.price_coins` |
| Price (Pixels) | Cost in activity points | `catalogue_items.price_pixels` |
| Club Only | Requires HC membership | `catalogue_items.is_club_only` |

## Moderation System

| Concept | Description | DB Table |
|---------|-------------|----------|
| Ban | User prohibition | `bans` |
| IP Ban | Block by IP address | `bans` (ban_type) |
| Machine Ban | Block by hardware ID | `bans` (ban_type) |
| Call for Help | User report | `cms_help_requests` |
| Chat Log | Recorded room chat | `room_chatlogs` |
| Hobba | Volunteer moderator | rank with moderation fuses |

## Messenger (Friends) System

| Concept | Description | DB Table |
|---------|-------------|----------|
| Buddy | Friend connection | `messenger_friends` |
| Buddy Request | Pending friend request | `messenger_requests` |
| Message | Offline message | `messenger_messages` |
| Campaign Message | Staff broadcast to all | via messenger system |

## Economy

| Concept | Description |
|---------|-------------|
| Credits | Main currency, bought with real money or earned |
| Pixels/Activity Points | Earned through in-game activity |
| Trading | Peer-to-peer item exchange |
| Recycler | Convert furni into rewards |
| Rare Items | Limited-edition high-value furniture |

## Key CMS Management Scenarios

1. **User Management**: Search users, view profiles, edit ranks/badges, ban/unban
2. **Room Management**: View all rooms, edit settings, delete rooms, manage models
3. **Catalogue Management**: Add/edit pages, manage items/prices, toggle visibility
4. **Moderation**: View reports, ban users, read chat logs, send alerts
5. **News/Content**: Create news articles, manage static pages, edit promos
6. **Economy**: View credit balances, manage rare items, handle recycler rewards

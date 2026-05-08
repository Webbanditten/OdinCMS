-- Create tables with fuses
create table fuses
(
    fuse        varchar(255)                                             not null
        primary key,
    user_group  enum ('ANYONE', 'HABBO_CLUB', 'CUSTOM') default 'ANYONE' not null,
    description varchar(255)                                             not null
);

insert into fuses (fuse, user_group, description) values ('default', 'ANYONE', 'Grants access to basic commands');
insert into fuses (fuse, user_group, description) values ('fuse_administrator_access', 'CUSTOM', 'Grants Admin commands');
insert into fuses (fuse, user_group, description) values ('fuse_any_room_controller', 'CUSTOM', 'Grants admin rights in any room.');
insert into fuses (fuse, user_group, description) values ('fuse_badges', 'CUSTOM', 'Can manage users badges');
insert into fuses (fuse, user_group, description) values ('fuse_ban', 'CUSTOM', 'Can ban people');
insert into fuses (fuse, user_group, description) values ('fuse_buy_credits', 'ANYONE', 'Used client side');
insert into fuses (fuse, user_group, description) values ('fuse_catalogue_administrator', 'CUSTOM', 'Can see the remaining catalogue pages');
insert into fuses (fuse, user_group, description) values ('fuse_catalogue_easter', 'CUSTOM', 'Can purchase easter season furni');
insert into fuses (fuse, user_group, description) values ('fuse_catalogue_habboclub', 'CUSTOM', 'Can purchase HC Furni');
insert into fuses (fuse, user_group, description) values ('fuse_catalogue_halloween', 'CUSTOM', 'Can purchase Habboween furni');
insert into fuses (fuse, user_group, description) values ('fuse_catalogue_summer', 'CUSTOM', 'Can purchase summer season furni');
insert into fuses (fuse, user_group, description) values ('fuse_catalogue_trophies', 'CUSTOM', 'Can purchase trophies');
insert into fuses (fuse, user_group, description) values ('fuse_catalogue_xmas', 'CUSTOM', 'Can purchase Christmas furni');
insert into fuses (fuse, user_group, description) values ('fuse_debug_window', 'CUSTOM', 'Used for debug commands ex. :reload');
insert into fuses (fuse, user_group, description) values ('fuse_enter_full_rooms', 'CUSTOM', 'Can enter full rooms');
insert into fuses (fuse, user_group, description) values ('fuse_enter_locked_rooms', 'CUSTOM', 'Can enter locked rooms');
insert into fuses (fuse, user_group, description) values ('fuse_extended_buddylist', 'HABBO_CLUB', 'Extended buddy list');
insert into fuses (fuse, user_group, description) values ('fuse_furni_chooser', 'HABBO_CLUB', 'Can use :furni command');
insert into fuses (fuse, user_group, description) values ('fuse_give_credits', 'CUSTOM', 'Can give credits');
insert into fuses (fuse, user_group, description) values ('fuse_habbo_chooser', 'HABBO_CLUB', 'Can use :chooser command');
insert into fuses (fuse, user_group, description) values ('fuse_hidden_rooms', 'CUSTOM', 'Ability to see hidden public rooms. ');
insert into fuses (fuse, user_group, description) values ('fuse_ignore_room_owner', 'CUSTOM', 'Ability to ignore room owner');
insert into fuses (fuse, user_group, description) values ('fuse_kick', 'CUSTOM', 'Allows kicking and shows the mod icon within the client');
insert into fuses (fuse, user_group, description) values ('fuse_pick_up_any_furni', 'CUSTOM', 'Can pickup any furni in any private room');
insert into fuses (fuse, user_group, description) values ('fuse_receive_calls_for_help', 'CUSTOM', 'Receive call for help');
insert into fuses (fuse, user_group, description) values ('fuse_remove_photos', 'CUSTOM', 'Can remove photos');
insert into fuses (fuse, user_group, description) values ('fuse_remove_stickies', 'CUSTOM', 'Remove stickies');
insert into fuses (fuse, user_group, description) values ('fuse_room_alert', 'CUSTOM', 'Room alert');
insert into fuses (fuse, user_group, description) values ('fuse_room_cat_special', 'CUSTOM', 'Can create rooms in special room categories like staff rooms etc.');
insert into fuses (fuse, user_group, description) values ('fuse_room_kick', 'CUSTOM', 'Room kick');
insert into fuses (fuse, user_group, description) values ('fuse_see_chat_log_link ', 'CUSTOM', 'Displays link to housekeeping page with chat log');
insert into fuses (fuse, user_group, description) values ('fuse_see_flat_ids', 'CUSTOM', 'Displays id of the room next to room name');
insert into fuses (fuse, user_group, description) values ('fuse_trade', 'ANYONE', 'Used client side');
insert into fuses (fuse, user_group, description) values ('fuse_use_club_dance', 'HABBO_CLUB', 'Can use HC dances');
insert into fuses (fuse, user_group, description) values ('fuse_use_special_room_layouts', 'HABBO_CLUB', 'Can use Habbo Club room layouts');
insert into fuses (fuse, user_group, description) values ('housekeeping', 'CUSTOM', 'General access to housekeeping');
insert into fuses (fuse, user_group, description) values ('housekeeping_campaign', 'CUSTOM', 'Can send campaign messages through the website');
insert into fuses (fuse, user_group, description) values ('housekeeping_localization', 'CUSTOM', 'Manage localization');
insert into fuses (fuse, user_group, description) values ('housekeeping_menu', 'CUSTOM', 'Manage the website menu');
insert into fuses (fuse, user_group, description) values ('housekeeping_news', 'CUSTOM', 'Manage news');
insert into fuses (fuse, user_group, description) values ('housekeeping_pages', 'CUSTOM', 'Manage pages');
insert into fuses (fuse, user_group, description) values ('housekeeping_ranks', 'CUSTOM', 'Manage ranks. !!DANGEROUS!!');
insert into fuses (fuse, user_group, description) values ('housekeeping_upload', 'CUSTOM', 'Upload files');
insert into fuses (fuse, user_group, description) values ('housekeeping_website', 'CUSTOM', 'Display website menu tab');


alter table catalogue_pages
    add fuse varchar(255) default 'DEFAULT' not null;

update catalogue_pages set fuse = 'fuse_catalogue_administrator' where min_role > 1;

create table ranks
(
    id   int auto_increment
        primary key,
    name varchar(255) not null
);

create table rank_rights
(
    rank_id int          not null,
    fuse    varchar(255) not null,
    primary key (rank_id, fuse),
    constraint rank_rights_FK_1
        foreign key (rank_id) references ranks (id)
            on delete cascade,
    constraint rank_rights_FK_2
        foreign key (fuse) references fuses (fuse)
            on delete cascade
);


alter table rooms_categories
    add fuse_access varchar(255) default 'DEFAULT' not null;
alter table rooms_categories
    add fuse_setflatcat varchar(255) default 'DEFAULT' not null;

update rooms_categories set fuse_access = 'fuse_hidden_rooms' where minrole_access > 1;
update rooms_categories set fuse_setflatcat = 'fuse_room_cat_special' where minrole_access > 1;


UPDATE users set `rank` = 0;


﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.WorldObjects;

namespace ClassicUO.Game
{
    public static class World
    {
        public static HashSet<Item> ToAdd { get; } = new HashSet<Item>();
        public static EntityCollection<Item> Items { get; } = new EntityCollection<Item>();
        public static EntityCollection<Mobile> Mobiles { get; } = new EntityCollection<Mobile>();
        public static PlayerMobile Player { get; set; }

        private static Map.Facet _map;
        public static Map.Facet Map
        {
            get => _map;
            set
            {
                if (value == null)
                    return;
                if (_map == null || _map.Index != value.Index)
                {
                    _map = null;
                    _map = value;

                    var position = Player.Position;
                    Player.Position = Position.Invalid;
                    Player.Position = position;
                    Player.ProcessDelta();
                }
            }
        }

        public static bool InGame => Player != null && Map != null;

        private static readonly ConcurrentDictionary<Serial, House> _houses = new ConcurrentDictionary<Serial, House>();

        public static long Ticks { get; set; }
        public static Renderer.IsometricLight Light { get; } = new Renderer.IsometricLight();

        const int DISTANCE_POV = 24;



        public static void Update(in double frameMS)
        {
            if (Player != null)
            {
                foreach (Mobile mob in Mobiles)
                {
                    mob.ViewObject.Update(frameMS);

                    if (mob.Distance > DISTANCE_POV)
                        RemoveMobile(mob);
                }

                foreach (Item item in Items)
                {
                    item.ViewObject.Update(frameMS);

                    if (item.Distance > DISTANCE_POV)
                        RemoveItem(item);
                }
            }
        }



        public static House GetHouse(in Serial serial)
        {
            _houses.TryGetValue(serial, out var h);
            return h;
        }

        public static House GetOrCreateHouse(in Serial serial)
        {
            if (_houses.TryGetValue(serial, out var house))
                return house;
            return new House(serial);
        }

        public static void AddOrUpdateHouse(in House house)
        {
            _houses.TryAdd(house.Serial, house);
        }

        public static  void RemoveHouse(in Serial house)
        {
            _houses.TryRemove(house, out var h);
        }


        public static bool Contains(in Serial serial)
        {
            if (serial.IsItem)
                return Items.Contains(serial);
            if (serial.IsMobile)
                return Mobiles.Contains(serial);
            return false;
        }

        public static Entity Get(in Serial serial)
        {
            if (serial.IsItem)
                return Items.Get(serial);
            if (serial.IsMobile)
                return Mobiles.Get(serial);
            return null;
        }

        public static Item GetOrCreateItem(in Serial serial)
        {
            return Items.Get(serial) ?? new Item(serial);
        }

        public static Mobile GetOrCreateMobile(in Serial serial)
        {
            return Mobiles.Get(serial) ?? new Mobile(serial);
        }

        public static bool RemoveItem(Serial serial)
        {
            Item item = Items.Remove(serial);
            if (item == null)
            {
                ToAdd.RemoveWhere(i => i == serial);
                return false;
            }

            foreach (Item i in item.Items)
                RemoveItem(i);
            item.Items.Clear();
            item.Tile = null;
            return true;
        }

        public static bool RemoveMobile(in Serial serial)
        {
            Mobile mobile = Mobiles.Remove(serial);
            if (mobile == null)
                return false;

            foreach (Item i in mobile.Items)
                RemoveItem(i);
            mobile.Items.Clear();
            mobile.Tile = null;
            return true;
        }

        public static void Clear()
        {
            Map = null;
            Player = null;
            Items.Clear();
            Mobiles.Clear();
        }
    }
}

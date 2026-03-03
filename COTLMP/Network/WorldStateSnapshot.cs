/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Serializable world-state snapshot for host-to-client sync
 * COPYRIGHT:   Copyright 2025 COTLMP Contributors
 */

/* IMPORTS ********************************************************************/

using System;
using System.IO;
using System.IO.Compression;
using System.Text;

/* CLASSES & CODE *************************************************************/

namespace COTLMP.Network
{
    /**
     * @brief
     * Holds a point-in-time snapshot of the host's world state.
     * Designed for binary serialization followed by GZip compression
     * so it can be sent over the network as a single message payload.
     *
     * @field FormatVersion
     * Incremented when the binary layout changes (backwards-compat guard).
     */
    internal sealed class WorldStateSnapshot
    {
        public const int FormatVersion = 1;

        /* ------------------------------------------------------------------ */
        /* Header                                                               */
        /* ------------------------------------------------------------------ */

        public string SceneName = "";
        public float  TimeOfDay;
        public int    CurrentDay;

        /* ------------------------------------------------------------------ */
        /* Sections                                                             */
        /* ------------------------------------------------------------------ */

        public FollowerEntry[]    Followers    = Array.Empty<FollowerEntry>();
        public StructureEntry[]   Structures   = Array.Empty<StructureEntry>();
        public EnemyEntry[]       Enemies      = Array.Empty<EnemyEntry>();
        public DroppedItemEntry[] DroppedItems = Array.Empty<DroppedItemEntry>();
        public ResourceEntry[]    Resources    = Array.Empty<ResourceEntry>();

        /* ------------------------------------------------------------------ */
        /* Inner data types                                                     */
        /* ------------------------------------------------------------------ */

        public struct FollowerEntry
        {
            public int    ID;
            public float  X, Y;
            public int    TaskType;
            public int    Role;
            public string Name;
        }

        public struct StructureEntry
        {
            public int   ID;
            public int   Type;
            public float X, Y;
            public bool  IsCollapsed;
            public bool  IsAflame;
        }

        public struct EnemyEntry
        {
            public int    InstanceID;
            public string TypeName;
            public float  X, Y;
            public float  HP, TotalHP;
            public bool   IsDead;
        }

        public struct DroppedItemEntry
        {
            public int   InstanceID;
            public int   Type;
            public int   Quantity;
            public float X, Y;
        }

        public struct ResourceEntry
        {
            public int   InstanceID;
            public float X, Y;
            public bool  IsDestroyed;
        }

        /* ------------------------------------------------------------------ */
        /* Binary serialization                                                 */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Writes the snapshot into a raw byte array.
         *
         * @return
         * The serialized bytes (not yet compressed).
         */
        public byte[] Serialize()
        {
            using (var ms = new MemoryStream())
            using (var w  = new BinaryWriter(ms, Encoding.UTF8, true))
            {
                w.Write(FormatVersion);
                WriteStr(w, SceneName);
                w.Write(TimeOfDay);
                w.Write(CurrentDay);

                /* Followers */
                w.Write(Followers.Length);
                foreach (var f in Followers)
                {
                    w.Write(f.ID);
                    w.Write(f.X); w.Write(f.Y);
                    w.Write(f.TaskType);
                    w.Write(f.Role);
                    WriteStr(w, f.Name);
                }

                /* Structures */
                w.Write(Structures.Length);
                foreach (var s in Structures)
                {
                    w.Write(s.ID); w.Write(s.Type);
                    w.Write(s.X); w.Write(s.Y);
                    w.Write(s.IsCollapsed); w.Write(s.IsAflame);
                }

                /* Enemies */
                w.Write(Enemies.Length);
                foreach (var e in Enemies)
                {
                    w.Write(e.InstanceID);
                    WriteStr(w, e.TypeName);
                    w.Write(e.X); w.Write(e.Y);
                    w.Write(e.HP); w.Write(e.TotalHP);
                    w.Write(e.IsDead);
                }

                /* Dropped items */
                w.Write(DroppedItems.Length);
                foreach (var i in DroppedItems)
                {
                    w.Write(i.InstanceID); w.Write(i.Type);
                    w.Write(i.Quantity);
                    w.Write(i.X); w.Write(i.Y);
                }

                /* Resources */
                w.Write(Resources.Length);
                foreach (var r in Resources)
                {
                    w.Write(r.InstanceID);
                    w.Write(r.X); w.Write(r.Y);
                    w.Write(r.IsDestroyed);
                }

                return ms.ToArray();
            }
        }

        /**
         * @brief
         * Reads a snapshot from a raw byte array.
         *
         * @param[in] data
         * The serialized bytes (not compressed).
         *
         * @return
         * The deserialized WorldStateSnapshot.
         */
        public static WorldStateSnapshot Deserialize(byte[] data)
        {
            var snap = new WorldStateSnapshot();
            using (var ms = new MemoryStream(data))
            using (var r  = new BinaryReader(ms, Encoding.UTF8, true))
            {
                int version = r.ReadInt32();
                if (version != FormatVersion)
                    throw new InvalidDataException($"Unsupported snapshot version {version}");

                snap.SceneName  = ReadStr(r);
                snap.TimeOfDay  = r.ReadSingle();
                snap.CurrentDay = r.ReadInt32();

                /* Followers */
                int count = r.ReadInt32();
                snap.Followers = new FollowerEntry[count];
                for (int i = 0; i < count; i++)
                {
                    snap.Followers[i] = new FollowerEntry
                    {
                        ID       = r.ReadInt32(),
                        X        = r.ReadSingle(), Y = r.ReadSingle(),
                        TaskType = r.ReadInt32(),
                        Role     = r.ReadInt32(),
                        Name     = ReadStr(r)
                    };
                }

                /* Structures */
                count = r.ReadInt32();
                snap.Structures = new StructureEntry[count];
                for (int i = 0; i < count; i++)
                {
                    snap.Structures[i] = new StructureEntry
                    {
                        ID          = r.ReadInt32(), Type = r.ReadInt32(),
                        X           = r.ReadSingle(), Y = r.ReadSingle(),
                        IsCollapsed = r.ReadBoolean(), IsAflame = r.ReadBoolean()
                    };
                }

                /* Enemies */
                count = r.ReadInt32();
                snap.Enemies = new EnemyEntry[count];
                for (int i = 0; i < count; i++)
                {
                    snap.Enemies[i] = new EnemyEntry
                    {
                        InstanceID = r.ReadInt32(),
                        TypeName   = ReadStr(r),
                        X          = r.ReadSingle(), Y = r.ReadSingle(),
                        HP         = r.ReadSingle(), TotalHP = r.ReadSingle(),
                        IsDead     = r.ReadBoolean()
                    };
                }

                /* Dropped items */
                count = r.ReadInt32();
                snap.DroppedItems = new DroppedItemEntry[count];
                for (int i = 0; i < count; i++)
                {
                    snap.DroppedItems[i] = new DroppedItemEntry
                    {
                        InstanceID = r.ReadInt32(), Type = r.ReadInt32(),
                        Quantity   = r.ReadInt32(),
                        X          = r.ReadSingle(), Y = r.ReadSingle()
                    };
                }

                /* Resources */
                count = r.ReadInt32();
                snap.Resources = new ResourceEntry[count];
                for (int i = 0; i < count; i++)
                {
                    snap.Resources[i] = new ResourceEntry
                    {
                        InstanceID  = r.ReadInt32(),
                        X           = r.ReadSingle(), Y = r.ReadSingle(),
                        IsDestroyed = r.ReadBoolean()
                    };
                }
            }
            return snap;
        }

        /* ------------------------------------------------------------------ */
        /* GZip compression                                                     */
        /* ------------------------------------------------------------------ */

        /**
         * @brief
         * Compresses raw bytes using GZip.
         */
        public static byte[] Compress(byte[] raw)
        {
            using (var output = new MemoryStream())
            {
                using (var gz = new GZipStream(output, CompressionMode.Compress, true))
                    gz.Write(raw, 0, raw.Length);
                return output.ToArray();
            }
        }

        /**
         * @brief
         * Decompresses GZip bytes back to raw.
         */
        public static byte[] Decompress(byte[] compressed)
        {
            using (var input  = new MemoryStream(compressed))
            using (var gz     = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gz.CopyTo(output);
                return output.ToArray();
            }
        }

        /* ------------------------------------------------------------------ */
        /* String helpers                                                       */
        /* ------------------------------------------------------------------ */

        private static void WriteStr(BinaryWriter w, string s)
        {
            if (string.IsNullOrEmpty(s)) { w.Write(0); return; }
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            w.Write(bytes.Length);
            w.Write(bytes);
        }

        private static string ReadStr(BinaryReader r)
        {
            int len = r.ReadInt32();
            return len <= 0 ? string.Empty : Encoding.UTF8.GetString(r.ReadBytes(len));
        }
    }
}

/* EOF */

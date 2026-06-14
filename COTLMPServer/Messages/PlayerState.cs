/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define the PlayerState class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using System;
using System.IO;

/* CLASSES & CODE *************************************************************/

namespace COTLMPServer.Messages
{
    /// <summary>
    /// Represents the state of a player
    /// </summary>
    public class PlayerState
    {
        public State Current;
        public float Facing;
        public float Look;
        public bool Defending;
        public float Timer;
        public Vector3 Position;

        /// <summary>
        /// The magic number to be used for verification when sent over the network
        /// </summary>
        public const int MagicNumber = 0xAB3245;

        /// <summary>
        /// The minimum amount of bytes the structure will take up serialized
        /// </summary>
        public const int SerializedSize = (sizeof(int) * 2 + sizeof(float) * 3 + sizeof(byte));

        public PlayerState(State state, float facing, float look, bool defending, float timer, Vector3 position)
        {
            Current = state;
            Facing = facing;
            Look = look;
            Defending = defending;
            Timer = timer;
            Position = position;
        }

        /// <summary>
        /// Serialize the object into a byte array
        /// </summary>
        /// <returns>
        /// The resulting byte array
        /// </returns>
        /// <exception cref="InvalidDataException">
        /// If the data in the object is invalid
        /// </exception>
        public byte[] Serialize()
        {
            if (!Enum.IsDefined(typeof(State), Current))
                throw new InvalidDataException("State is invalid");

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(MagicNumber);
                writer.Write((int)Current);
                writer.Write(Facing);
                writer.Write(Look);
                writer.Write(Defending);
                writer.Write(Timer);
                byte[] vectorBytes = Position.Serialize();
                writer.Write(vectorBytes.Length);
                writer.Write(vectorBytes);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Deserializes the byte array back into an object
        /// </summary>
        /// <param name="data">
        /// The byte array to be processed
        /// </param>
        /// <returns>
        /// The resulting object
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// When data is null
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// When the data contained in the byte array is invalid
        /// </exception>
        public static PlayerState Deserialize(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length < SerializedSize)
                throw new InvalidDataException("Data too small");

            using (MemoryStream stream = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != MagicNumber)
                    throw new InvalidDataException("Magic number doesn't match");

                State state = (State)reader.ReadInt32();
                if (!Enum.IsDefined(typeof(State), state))
                    throw new InvalidDataException("State not defined in enum");
                return new PlayerState(state, reader.ReadSingle(), reader.ReadSingle(), reader.ReadBoolean(), reader.ReadSingle(), Vector3.Deserialize(Utils.ReadBytes(reader), 0, out _));
            }
        }

        /// <summary>
        /// An enum of all possible player states
        /// </summary>
        public enum State
        {
            Idle,
            Moving,
            Attacking,
            Defending,
            SignPostAttack,
            RecoverFromAttack,
            AimDodge,
            Dodging,
            Fleeing,
            Inventory,
            Map,
            WeaponSelect,
            CustomAction0,
            InActive,
            RaiseAlarm,
            Casting,
            TimedAction,
            Worshipping,
            Sleeping,
            BeingCarried,
            HitThrown,
            HitLeft,
            HitRight,
            HitRecover,
            Teleporting,
            SignPostCounterAttack,
            RecoverFromCounterAttack,
            Charging,
            Vulnerable,
            Converting,
            Unconverted,
            FoundItem,
            Dieing,
            Dead,
            Building,
            Respawning,
            AwaitRecruit,
            PickedUp,
            SacrificeRecruit,
            Recruited,
            Dancing,
            SpawnIn,
            SpawnOut,
            CrowdWorship,
            Grapple,
            DashAcrossIsland,
            ChargingHeavyAttack,
            Elevator,
            Grabbed,
            CustomAnimation,
            Preach,
            Stealth,
            GameOver,
            KnockBack,
            Aiming,
            Meditate,
            Resurrecting,
            Idle_CarryingBody,
            Moving_CarryingBody,
            Heal,
            Reeling,
            TiedToAltar,
            FinalGameOver,
            KnockedOut,
            CoopReviving,
        }
    }
}

/* EOF */

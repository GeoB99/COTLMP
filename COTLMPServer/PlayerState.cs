/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define the PlayerState class
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* IMPORTS ********************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/* CLASSES & CODE *************************************************************/

/**
 * @brief
 * Contains the classes/structs/enums for the server
 */
namespace COTLMPServer
{
    /**
     * @brief
     * Represents the state of a player
     * 
     * @field Current
     * the current state of the player
     * 
     * @field Facing
     * Where the player is facing
     * 
     * @field Look
     * Where the player is looking
     * 
     * @field Defending
     * Whether the player is defending or not
     * 
     * @field Timer
     * Some kind of timer
     * 
     * @field Position
     * The player position
     * 
     * @field MagicNumber
     * The magic number to be used for verification when sent over the network
     */
    public class PlayerState
    {
        public State Current;
        public float Facing;
        public float Look;
        public bool Defending;
        public float Timer;
        public Vector3 Position;
        public const int MagicNumber = 0xAB3245;

        /**
         * @brief
         * The constructor
         * 
         * @param[in] Current
         * the current state of the player
         * 
        * @param[in] Facing
        * Where the player is facing
        * 
         * @param[in] Look
        * Where the player is looking
        * 
        * @param[in] Defending
        * Whether the player is defending or not
         * 
         * @param[in] Timer
         * Some kind of timer
         * 
         * @param[in] Position
        * The player position
         */
        public PlayerState(State state, float facing, float look, bool defending, float timer, Vector3 position)
        {
            Current = state;
            Facing = facing;
            Look = look;
            Defending = defending;
            Timer = timer;
            Position = position;
        }

        /**
         * @brief
         * Serialize the object into a byte array
         * 
         * @returns
         * The resulting byte array
         * 
         * @throws InvalidDataException
         * If the data in the object is invalid
         */
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

        /**
         * @brief
         * Deserializes the byte array back into an object
         * 
         * @param[in] data
         * The byte array to be processed
         * 
         * @returns
         * The resulting object
         * 
         * @throws ArgumentNullException
         * When data is null
         * 
         * @throws InvalidDataException
         * When the data contained in the byte array is invalid
         */
        public static PlayerState Deserialize(IReadOnlyList<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Count < (sizeof(int) * 2 +
                sizeof(float) * 3 +
                sizeof(byte)))
                throw new InvalidDataException("Data too small");

            byte[] buffer = data as byte[] ?? data.ToArray();

            using (MemoryStream stream = new MemoryStream(buffer, false))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != MagicNumber)
                    throw new InvalidDataException("Magic number doesn't match");

                State state = (State)reader.ReadInt32();
                if (!Enum.IsDefined(typeof(State), state))
                    throw new InvalidDataException("State not defined in enum");
                return new PlayerState(state, reader.ReadSingle(), reader.ReadSingle(), reader.ReadBoolean(), reader.ReadSingle(), Vector3.Deserialize(Messages.Utils.ReadBytes(reader)));
            }
        }

        /**
         * @brief
         * An enum of all possible player states
         */
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

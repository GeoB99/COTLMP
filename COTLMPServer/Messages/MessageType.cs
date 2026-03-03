/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define the MessageTypes enum
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 */

/* CLASSES & CODE *************************************************************/

/**
 * @brief
 * Contains all classes/structs/enums associated with network messages
 */
namespace COTLMPServer.Messages
{
    /**
     * @brief
     * Message type enum
     */
    public enum MessageType
    {
        Test             = 0,
        PlayerJoin       = 1,   // C->S: join with name;         S->All: new player joined
        PlayerJoinAck    = 2,   // S->C: assigned ID + success flag
        PlayerLeave      = 3,   // C->S: graceful leave;         S->All: player left
        PlayerPosition   = 4,   // C->S: position + angle;       S->Others: relay
        PlayerState      = 5,   // C->S: StateMachine.State;     S->Others: relay
        PlayerHealth     = 6,   // C->S: hp + totalHp;           S->Others: relay
        PlayerAnimation  = 7,   // C->S: animation name;         S->Others: relay
        ChatMessage      = 8,   // C->S: chat string;            S->All: relay
        Heartbeat        = 9,   // C->S: keepalive ping
        HeartbeatAck     = 10,  // S->C: keepalive pong
        PlayerKick       = 11,  // S->C: kicked with reason string
        DiscoveryRequest = 12,  // Broadcast: LAN discovery probe (not routed through server game port)
        DiscoveryResponse = 13, // Response: server info payload
        HostSaveData     = 14,  // S->C: host's save data (small, single packet)
        WorldStateHeartbeat = 15, // Host->S->Others: periodic world-state snapshot
        HostSaveDataChunk = 16, // S->C: chunked host save data [chunkIdx(4) totalChunks(4) totalSize(4) data...]
        SceneChange      = 17, // Host->S->Others: host changed scene, clients should follow
        SaveResync       = 18  // Host->S->Others: updated save data after host saves
    }
}

/* EOF */

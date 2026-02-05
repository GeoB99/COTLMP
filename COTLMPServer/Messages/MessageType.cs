/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Define the MessageTypes enum
 * COPYRIGHT:	Copyright 2025 Neco-Arc <neco-arc@inbox.ru>
 *              Copyright 2026 GeoB99 <geobman1999@gmail.com>
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
     * Message events dispatched to the server by connected
     * clients. The server is tasked to perform actions based
     * on the nature of the said event.
     * 
     * @field TeleportDungeon
     * Alerts the server that a player has entered a new dungeon
     * and that every player must be teleported.
     * 
     * @field RitualPerform
     * Alerts the server that a player is performing a ritual
     * (sermons also count) and every player must be notified of it.
     * 
     * @field PlayerJoin
     * Alerts the server that a player has recently joined the server.
     * 
     * @field PlayerLeft
     * Alerts the server that a player has recently left the server.
     * 
     * @field ChatNotify
     * Alerts the server that a player has sent a chat message and that
     * it must be broadcasted across all other players.
     */
    public enum MessageType
    {
        TeleportDungeon = 0,
        RitualPerform,
        PlayerJoin,
        PlayerLeft,
        ChatNotify
    }
}

/* EOF */

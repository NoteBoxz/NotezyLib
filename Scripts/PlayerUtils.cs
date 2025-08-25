using DunGen.Graph;
using GameNetcodeStuff;
using LethalLevelLoader;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace NotezyLib.Utils
{
    public static class PlayerUtils
    {
        public static bool IsPlayerConnected(PlayerControllerB player)
        {
            bool Valid;

            PlayerControllerB ServerPlayer = StartOfRound.Instance.allPlayerScripts[0];

            Valid = player == ServerPlayer
            || player != ServerPlayer && player.OwnerClientId != NetworkManager.ServerClientId
            && StartOfRound.Instance.ClientPlayerList.ContainsKey(player.OwnerClientId);

            return Valid;
        }

        public static List<PlayerControllerB> GetAllConnectedPlayers()
        {
            List<PlayerControllerB> Players = new List<PlayerControllerB>();

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (IsPlayerConnected(player))
                    Players.Add(player);
            }

            return Players;
        }

        public static PlayerControllerB? GetPlayerByClientId(ulong clientId)
        {
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.OwnerClientId == clientId && IsPlayerConnected(player))
                    return player;
            }

            return null;
        }

        public static void SetPlayerInside(PlayerControllerB player, bool Inside)
        {
            try
            {
                bool wasInside = player.isInsideFactory;
                player.isInsideFactory = Inside;
                EntranceTeleport Main = RoundManager.FindMainEntranceScript(!wasInside);
                if (Main == null || Main.audioReverbPreset == -1)
                {
                    return;
                }
                Object.FindObjectOfType<AudioReverbPresets>().audioPresets[Main.audioReverbPreset].ChangeAudioReverbForPlayer(player);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error setting player inside state: {e.Message}");
            }
        }
    }
}
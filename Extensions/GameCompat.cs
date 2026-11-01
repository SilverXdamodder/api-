/*
 * ii's Stupid Menu  Extensions/GameCompat.cs
 * A mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Goldentrophy Software
 * https://github.com/iiDk-the-actual/iis.Stupid.Menu
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using GorillaLocomotion;
using GorillaLocomotion.Swimming;
using Photon.Pun;
using UnityEngine;

namespace iiMenu.Extensions
{
    /// <summary>
    /// Shims for Gorilla Tag members that were renamed or reworked after this menu was written.
    /// </summary>
    public static class GameCompat
    {
        /// <summary>GTPlayer.swimmingParams was replaced by per-liquid parameter sets.</summary>
        public static PlayerSwimmingParameters SwimmingParams =>
            GTPlayer.Instance.GetSwimmingParams(GTPlayer.LiquidType.Water);

        /// <summary>CustomMapsTerminal.mapTerminalNetworkObject was replaced by a public state setter.</summary>
        public static void SendTerminalControlStatusRPC(int actorNumber, bool locked) =>
            CustomMapsTerminal.SetTerminalControlStatus(locked, actorNumber, true);

        /// <summary>The PhotonView backing the custom maps terminal.</summary>
        public static PhotonView TerminalView =>
            CustomMapsTerminal.instance.GetComponentInParent<PhotonView>();
    }
}

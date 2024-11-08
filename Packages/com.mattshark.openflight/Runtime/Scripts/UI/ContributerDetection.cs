﻿/**
 * @ Maintainer: Happyrobot33
 */

using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

namespace OpenFlightVRC.UI
{
	/// <summary>
	/// Handles detecting if players are contributers or not
	/// </summary>
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class ContributerDetection : LoggableUdonSharpBehaviour
	{
		public override string _logCategory { get => "Contributer Detection"; }
		public AvatarListLoader AvatarListLoader;
		/// <summary>
		/// Will be true if there is at least one contributer in the instance
		/// </summary>
		[ReadOnlyInspector]
		public bool contributerInWorld = false;
		private bool _localPlayerIsContributer = false;
		/// <summary>
		/// If the local player is a contributer
		/// </summary>
		public bool localPlayerIsContributer
		{
			get
			{
				//check if we should hide the local player's contributer status
				return !hideLocalPlayerContributerStatus && _localPlayerIsContributer;
			}
			set { _localPlayerIsContributer = value; }
		}

		/// <summary> If true, the local player will not have their contributer status broadcasted to everyone else </summary>
		/// <remarks> This does not affect the contributer in world boolean, but does affect the <see cref="localPlayerIsContributer"/> boolean </remarks>
		public bool hideLocalPlayerContributerStatus = false;

		/// <summary>
		/// A formatted list of all the openflight contributers
		/// </summary>
		[ReadOnlyInspector]
		public string contributersString = "";

		private DataList _contributers = new DataList();
		private DataList _contributersInWorld = new DataList();

		void Start()
		{
			//subscribe to the avatar list loader callback
			AvatarListLoader.AddCallback(AvatarListLoaderCallback.AvatarListReady, this, nameof(GetContributersList));
		}

		public void GetContributersList()
		{
			//deserialize
			bool success = VRCJson.TryDeserializeFromJson(AvatarListLoader.Output, out DataToken json);

			//grab the Contributers array
			_contributers = json.DataDictionary["Contributers"].DataList;

			//quickly check if the local player is a contributer
			if (_contributers.Contains(Networking.LocalPlayer.displayName))
			{
				localPlayerIsContributer = true;
				Log(LogLevel.Info, "Local player is a contributer!");
				contributerInWorld = true;
				_contributersInWorld.Add(Networking.LocalPlayer.displayName);
			}

			//check all players for contributer status
			CheckForContributers();
		}

		public override void OnPlayerJoined(VRCPlayerApi player)
		{
			//check if the player is a contributer
			if (_contributers.Contains(player.displayName))
			{
				Log(LogLevel.Info, player.displayName + " is a contributer!");
				contributerInWorld = true;
				_contributersInWorld.Add(player.displayName);
			}
		}

		public override void OnPlayerLeft(VRCPlayerApi player)
		{
			//check if the user that left was a contributer
			if (_contributers.Contains(player.displayName) && contributerInWorld)
			{
				Log(LogLevel.Info, "Player that left was a contributer! Checking for remaining contributers...");
				_contributersInWorld.RemoveAll(player.displayName);

				//check if there are any contributers left
				if (_contributersInWorld.Count == 0)
				{
					Log(LogLevel.Info, "No contributers left!");
					contributerInWorld = false;
				}
				else
				{
					Log(LogLevel.Info, "Contributers left: " + _contributersInWorld.Count);
				}
			}
		}

		/// <summary>
		/// Checks if there are any contributers in the instance
		/// </summary>
		public void CheckForContributers()
		{
			Log(LogLevel.Info, "Checking for contributers...");

			//format them into a string
			contributersString = "";
			for (int i = 0; i < _contributers.Count; i++)
			{
				contributersString += _contributers[i].String + ", ";
			}

			//populate our player list
			VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
			VRCPlayerApi.GetPlayers(players);

			//loop through each user in the instance and check if they are a contributer
			foreach (VRCPlayerApi player in players)
			{
				//check if the player is valid
				if (!player.IsValid())
				{
					continue;
				}

				//check if the player is a contributer
				if (_contributers.Contains(player.displayName))
				{
					Log(LogLevel.Info, player.displayName + " is a contributer!");
					contributerInWorld = true;
					_contributersInWorld.Add(player.displayName);
				}
			}

			//check if there are any contributers in the instance
			if (!contributerInWorld)
			{
				Log(LogLevel.Info, "No contributers in the instance!");
			}
		}
	}
}

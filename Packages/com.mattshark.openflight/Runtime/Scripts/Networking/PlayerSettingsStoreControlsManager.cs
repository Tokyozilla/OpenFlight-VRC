﻿/**
 * @ Maintainer: Happyrobot33
 */

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;
using TMPro;
using OpenFlightVRC.UI;

namespace OpenFlightVRC.Net
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerSettingsStoreControlsManager : LoggableUdonSharpBehaviour
    {

        #region UI References
        public InputField slotNameInput;
        public Toggle useWorldDefaultsToggle;
        public InputField importSlot;
        public InputField exportSlot;
        public InputField importDB;
        public InputField exportDB;
        public GameObject RemoteDifferencesPanel;
        public PlayerUIDropdown playerDropdown;
        /// <summary>
        /// The objects that should be disabled when the player is not the owner of the reference player store
        /// </summary>
        public Selectable[] DisabledWhenNotOwner;
        public TextMeshProUGUI defaultSlotNameText;
        public TextMeshProUGUI StorageInfo;
        #endregion


        /// <summary>
        /// The current slot that the local player is viewing on the reference player store
        /// </summary>
        private string m_CurrentSlot = "";

        /// <summary>
        /// The local player settings store
        /// </summary>
        private PlayerSettings m_LocalPlayerStore;

        /// <summary>
        /// The setting store that we are currently referencing
        /// </summary>
        private PlayerSettings m_ReferencePlayerStore;
        void Start()
        {
            m_LocalPlayerStore = Util.GetPlayerObjectOfType<PlayerSettings>(Networking.LocalPlayer);
            //start the reference off as the local players store
            m_ReferencePlayerStore = m_LocalPlayerStore;
            //subscribe to the world join callback
            m_ReferencePlayerStore.AddCallback(PlayerSettingsCallback.OnLocalDataReady, this, nameof(InitialWorldJoin));
            m_ReferencePlayerStore.AddCallback(PlayerSettingsCallback.useWorldDefaultsWhenLoadingChanged, this, nameof(UpdateUI));
            m_ReferencePlayerStore.AddCallback(PlayerSettingsCallback.OnRemoteDifferencesDetected, this, nameof(RemoteDifferencesDetected));
            m_ReferencePlayerStore.AddCallback(PlayerSettingsCallback.OnRemoteDifferencesResolved, this, nameof(RemoteDifferencesResolved));

            m_ReferencePlayerStore.AddCallback(PlayerSettingsCallback.OnLocalDataReady, this, nameof(UpdateStorageText));
            m_ReferencePlayerStore.AddCallback(PlayerSettingsCallback.OnRemoteDifferencesDetected, this, nameof(UpdateStorageText));
            m_ReferencePlayerStore.AddCallback(PlayerSettingsCallback.OnRemoteDifferencesResolved, this, nameof(UpdateStorageText));
            m_ReferencePlayerStore.AddCallback(PlayerSettingsCallback.OnStorageFull, this, nameof(UpdateStorageText));
            m_ReferencePlayerStore.AddCallback(PlayerSettingsCallback.OnStorageFree, this, nameof(UpdateStorageText));
            playerDropdown.AddCallback(PlayerUIDropdownCallback.ValueChanged, this, nameof(SelectedPlayerChanged));
        }

        public void SelectedPlayerChanged()
        {
            SetReferencePlayerStore(playerDropdown.selectedPlayer);
        }

        #region Callback Triggered Functions
        public void RemoteDifferencesDetected()
        {
            RemoteDifferencesPanel.SetActive(true);
        }

        public void RemoteDifferencesResolved()
        {
            RemoteDifferencesPanel.SetActive(false);
        }

        public void UpdateStorageText()
        {
            StorageInfo.text = m_ReferencePlayerStore._GetStorageInfo();
        }

        public void InitialWorldJoin()
        {
            //make sure our current slot is the one that is loaded by default
            m_CurrentSlot = m_ReferencePlayerStore._GetDefaultSlot();

            UpdateUI();
        }
        #endregion

        #region UI Functions
        public void UploadSettings()
        {
            m_LocalPlayerStore._UploadSettings();
        }

        public void RevertSettings()
        {
            m_LocalPlayerStore._RevertSettings();

            //validate the current slot
            m_CurrentSlot = m_ReferencePlayerStore._ValidateSlot(m_CurrentSlot);

            UpdateUI();
        }

        public void Save()
        {
            //check if empty, which would mean a fail
            if (m_ReferencePlayerStore._SaveSlot(m_CurrentSlot, out string newName))
            {
                m_CurrentSlot = newName;
                UpdateUI();
            }
        }

        public void Load()
        {
            //m_ReferencePlayerStore._LoadSlotBool(m_CurrentSlot);
            m_ReferencePlayerStore._LoadSlot(m_CurrentSlot, out var DISCARD);
        }

        public void Rename()
        {
            if (m_ReferencePlayerStore._RenameSlot(m_CurrentSlot, slotNameInput.text))
            {
                m_CurrentSlot = slotNameInput.text;
            }

            UpdateUI();
        }

        public void NewSlot()
        {
            //m_CurrentSlot = m_ReferencePlayerStore._NewSlot();
            if (m_ReferencePlayerStore._NewSlot(out string newName))
            {
                m_CurrentSlot = newName;
                UpdateUI();
            }
        }

        public void DeleteSlot()
        {
            if (m_ReferencePlayerStore._DeleteSlot(m_CurrentSlot))
            {
                //validate the current slot
                m_CurrentSlot = m_ReferencePlayerStore._ValidateSlot(m_CurrentSlot);
            }

            UpdateUI();
        }

        public void PreviousSlot()
        {
            //advance the slot index
            m_CurrentSlot = m_ReferencePlayerStore._GetSlotName(m_ReferencePlayerStore._GetSlotIndex(m_CurrentSlot) - 1);

            //validate the slot index
            m_CurrentSlot = m_ReferencePlayerStore._ValidateSlot(m_CurrentSlot);

            UpdateUI();
        }

        public void NextSlot()
        {
            //advance the slot index
            m_CurrentSlot = m_ReferencePlayerStore._GetSlotName(m_ReferencePlayerStore._GetSlotIndex(m_CurrentSlot) + 1);

            //validate the slot index
            m_CurrentSlot = m_ReferencePlayerStore._ValidateSlot(m_CurrentSlot);

            UpdateUI();
        }

        public void SetAsDefaultSlot()
        {
            //check if we can edit
            if (!m_ReferencePlayerStore.CanEdit)
            {
                return;
            }

            //ReferencePlayerStore.slotToLoadByDefault = currentSlot;
            //ReferencePlayerStore.useWorldDefaultsWhenLoading = false;
            m_ReferencePlayerStore._SetGlobalSetting(PlayerSettings.slotToLoadByDefaultKey, m_CurrentSlot);
            m_ReferencePlayerStore._SetGlobalSetting(PlayerSettings.useWorldDefaultsWhenLoadingKey, false);

            UpdateUI();
        }

        public void WorldDefaultToggleChanged()
        {
            //ReferencePlayerStore.useWorldDefaultsWhenLoading = useWorldDefaultsToggle.isOn;
            m_ReferencePlayerStore._SetGlobalSetting(PlayerSettings.useWorldDefaultsWhenLoadingKey, useWorldDefaultsToggle.isOn);
        }

        public void Duplicate()
        {
            //determine if we are viewing ours or someone elses
            if (m_ReferencePlayerStore != m_LocalPlayerStore)
            {
                //Duplicating will *always* duplicate to our own player store
                string currentName = m_CurrentSlot;

                //get the settings of the reference player store
                m_ReferencePlayerStore._LoadSlot(m_CurrentSlot, out DataDictionary settings);

                //save the current slot to the local store
                m_LocalPlayerStore._SaveSlot(settings, currentName, out var DISCARD);
            }
            else
            {
                if (m_ReferencePlayerStore._DuplicateSlot(m_CurrentSlot, out string newName))
                {
                    m_CurrentSlot = newName;
                }
            }

            UpdateUI();
        }

        #region import/export methods
        public void ImportSlotChanged()
        {
            if (importSlot.text == "")
            {
                return;
            }

            //change references over to us
            SetReferencePlayerStore(Networking.LocalPlayer);

            m_LocalPlayerStore._ImportSlot(importSlot.text, out string newName);

            if (newName != "")
            {
                //set the slot to the new name
                m_CurrentSlot = newName;
            }

            SetReferencePlayerStore(Networking.LocalPlayer);
        }

        public void ImportDBChanged()
        {
            if (importDB.text == "")
            {
                return;
            }

            m_LocalPlayerStore._ImportDB(importDB.text);

            //validate the slot
            m_CurrentSlot = m_LocalPlayerStore._ValidateSlot(m_CurrentSlot);

            //ensures everything is up to date
            SetReferencePlayerStore(Networking.LocalPlayer);
        }
        #endregion
        #endregion

        private void UpdateUI()
        {
            //set the text
            slotNameInput.text = m_CurrentSlot;

            if (m_ReferencePlayerStore._GetSlotExport(m_CurrentSlot, out string export))
            {
                exportSlot.text = export;
            }
            if (m_ReferencePlayerStore._GetDBExport(out string dbExport))
            {
                exportDB.text = dbExport;
            }

            importDB.text = "";
            importSlot.text = "";

            //useWorldDefaultsToggle.isOn = ReferencePlayerStore.useWorldDefaultsWhenLoading;
            //useWorldDefaultsToggle.isOn = m_ReferencePlayerStore._GetGlobalSetting(PlayerSettings.useWorldDefaultsWhenLoadingKey).Boolean;
            if (m_ReferencePlayerStore._GetGlobalSetting(PlayerSettings.useWorldDefaultsWhenLoadingKey, out DataToken value))
            {
                useWorldDefaultsToggle.isOn = value.Boolean;
            }

            defaultSlotNameText.text = m_ReferencePlayerStore._GetDefaultSlot();
        }

        /// <summary>
        /// Sets the reference player store to the given player
        /// </summary>
        /// <param name="player"></param>
        public void SetReferencePlayerStore(VRCPlayerApi player)
        {
            //get the reference manager
            PoolObjectReferenceManager referenceManager = Util.GetPlayerObjectOfType<PoolObjectReferenceManager>(player);

            //get the store
            PlayerSettings store = referenceManager.PlayerSettingsStore;

            //check if its even a different player
            if (store == m_ReferencePlayerStore)
            {
                return;
            }

            //check if the store is initialized
            if (!store.IsInitialized)
            {
                Logger.LogWarning("Player store is not initialized, cannot set as reference", this);
                return;
            }

            m_ReferencePlayerStore = store;
            //set the slot to 0
            m_CurrentSlot = m_ReferencePlayerStore._GetSlotName(0);

            //if the reference store is not the local player, disable the UI elements
            foreach (Selectable element in DisabledWhenNotOwner)
            {
                element.interactable = m_ReferencePlayerStore.CanEdit;
            }

            //get all the button objects
            /* PlayerButton[] buttons = Util.GetAllPlayerObjectsOfType<PlayerButton>();
            foreach (PlayerButton button in buttons)
            {
                button.UpdateHighlight(player);
            } */

            UpdateUI();
        }

        // public override void OnPlayerLeft(VRCPlayerApi player)
        // {
        //     //TODO: Once fixed, use this method and remove the owner variable. Current bug does not allow this to work, and will return the local player instead
        //     //we need to verify that the player that left was not the reference player, and if it is, we need to set the reference player to the local player
        //     /*if (Networking.GetOwner(ReferencePlayerStore.gameObject) == player)
        //     {
        //         Logger.Log("Reference player left, setting to local player", this);
        //         SetReferencePlayerStore(Networking.LocalPlayer);
        //     }
        //     */

        //     //get the player 
        //     VRCPlayerApi playerref = m_ReferencePlayerStore.TEMPOWNERDOREMOVEWHENFIXED;
        //     if (playerref == player)
        //     {
        //         Logger.Log("Reference player left, setting to local player", this);
        //         SetReferencePlayerStore(Networking.LocalPlayer);
        //     }
        // }
    }
}

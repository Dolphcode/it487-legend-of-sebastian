using System;
using System.Collections.Generic;
using UnityEngine;

namespace SQZL.Entity.Player {
    public class PlayerInventory : MonoBehaviour
    {
        /// <summary>
        /// Populate this list in the editor to create the list of consumable items in the game. This allows us to specify a list of
        /// consumable types without hard coding it in a script.
        /// TODO: Maybe we should be storing this info in a JSON or a text file somewhere? mah probably fine if not
        /// </summary>
        [Header("Item Entries")]
        [SerializeField] private List<PlayerConsumableEntry> consumableEntries;

        /// <summary>
        /// Populate this list in the editor to create the list of unlockable items in the game (i.e. the sword, bow). This script
        /// is designed to be agnostic of the UI representation of Link's inventory. This script is purely data driven, a separate
        /// script should be created to read the data in this script and update the UI accordingly.
        /// </summary>
        [SerializeField] private List<PlayerUnlockableEntry> unlockableEntries;

        /// <summary>
        /// Populate this list to specify the durability data bindings for items with durability
        /// </summary>
        [SerializeField] private List<PlayerDurabilityEntry> durabilityTrackEntries;
        
        /// <summary>
        /// Subscribe to this event to be notified when an unlockable is toggled. The first argument is the identifier of the unlockable
        /// while the second argument is the new state (<c>true</c> if it was just unlocked, and <c>false</c> if it was not). This will
        /// ONLY fire if the unlockable's value is actually changed.
        /// </summary>
        /// <example>
        /// The following code will fire off the OnUnlockableUpdated event handler:
        /// <code>
        /// // ... storing PlayerInventory component in playerInv variable
        /// playerInv.ToggleUnlockable(!playerInv.HasUnlockable());
        /// </code>
        /// While the following code will not:
        /// <code>
        /// // Sets the unlockable's value to the unlockable's original value, which means no change
        /// playerInv.ToggleUnlockable(playerInv.HasUnlockable();
        /// </code>
        /// </example>
        public event Action<string, bool> OnUnlockableUpdated;

        /// <summary>
        /// Subscribe to this event to be notified when a consumable's value is changed. The first argument is the identifier of the
        /// consumable. The second argument is the updated value of the consumable. The third argument is the change in the amount of the consumable.
        /// </summary>
        /// <example>
        /// <code>
        /// public void OnConsumableUpdatedHandler(string id, int updated, int accum) {
        ///     int original = updated - accum;
        ///     Debug.Log($"The consumable {id} has been updated! Its new value is {updated} and its old value is {original}. The change in value was {accum}");
        /// }
        /// </code>
        /// </example>
        public event Action<string, int, int> OnConsumableUpdated;
        
        private Dictionary<string, int> _consumableInventory = new();
        private Dictionary<string, bool> _unlockableInventory = new();
        private Dictionary<string, PlayerDurabilityEntry> _durabilityTracker = new();

        private void Awake()
        {
            foreach (PlayerConsumableEntry entry in consumableEntries) _consumableInventory[entry.name] = entry.startingCount;
            foreach (PlayerUnlockableEntry entry in unlockableEntries) _unlockableInventory[entry.name] = entry.startsWith;
            foreach (PlayerDurabilityEntry entry in durabilityTrackEntries) _durabilityTracker[entry.name] = entry;
        }

        #region UNLOCKABLE
        /// <summary>
        /// Call this to unlock or remove an unlockable item from the player's inventory. This function will do nothing if the unlockable name
        /// specified does not exist (or will throw a warning if in <c>DEBUG</c> mode).
        /// </summary>
        /// <param name="name">The index/name of the unlockable specified in the <c>unlockableEntries</c> list</param>
        /// <param name="flag"><c>true</c> to unlock, <c>false</c> to lock</param>
        public void ToggleUnlockable(string name, bool flag)
        {
            // Check if unlockable exists
            if (!_unlockableInventory.ContainsKey(name))
            {
#if DEBUG
                if (_consumableInventory.ContainsKey(name)) Debug.Log(
                    $"WARNING: Unlockable with identifier {name} does not exist. This function call will do nothing. As {name} is a consumable, did you mean to call AccumulateConsumable?");
                else Debug.LogWarning(
                    $"WARNING: Unlockable with identifier {name} does not exist. This function call will do nothing");
#endif
                return;
            }
            // Check if changed, then invoke handler
            bool oldValue = _unlockableInventory[name];
            _unlockableInventory[name] = flag;
            
            if (oldValue != flag) OnUnlockableUpdated?.Invoke(name, flag);
        }

        /// <summary>
        /// Used to check if the player has an unlockable
        /// </summary>
        /// <param name="name">The identifier of the unlockable to check</param>
        /// <returns><c>false</c> if the player does not have the unlockable OR if the identifier does not exist</returns>
        public bool HasUnlockable(string name)
        {
            // Check if unlockable exists
            if (!_unlockableInventory.ContainsKey(name))
            {
#if DEBUG
                if (_consumableInventory.ContainsKey(name)) Debug.Log(
                    $"WARNING: Unlockable with identifier {name} does not exist. As {name} is a consumable, did you mean to call GetConsumableAmount?");
                else Debug.LogWarning(
                    $"WARNING: Unlockable with identifier {name} does not exist.");
#endif
                return false;
            }
            
            // Then get value
            return _unlockableInventory[name];
        }
        #endregion
        #region CONSUMABLE
        /// <summary>
        /// Call this to consume or add to a consumable in the player's inventory. This function will do nothing if the consumable name
        /// specified does not exist (or will throw a warning if in <c>DEBUG</c> mode).
        /// </summary>
        /// <param name="name">The index/name of the consumable specified in the <c>consumableEntries</c> list</param>
        /// <param name="amount">The amount to accumulate, positive integer to add, and negative integer to consume</param>
        public void AccumulateConsumable(string name, int amount)
        {
            // Check if consumable exists
            if (!_consumableInventory.ContainsKey(name))
            {
#if DEBUG
                if (_unlockableInventory.ContainsKey(name)) Debug.Log(
                    $"WARNING: Consumable with identifier {name} does not exist. This function call will do nothing. As {name} is an unlockable, did you mean to call ToggleUnlockable?");
                else Debug.LogWarning(
                    $"WARNING: Consumable with identifier {name} does not exist. This function call will do nothing");
#endif
                return;
            }

            // Update
            _consumableInventory[name] += amount;
            OnConsumableUpdated?.Invoke(name, _consumableInventory[name], amount);
        }

        /// <summary>
        /// Used to get the amount of a consumable the player has
        /// </summary>
        /// <param name="name">The identifier of the consumable to check</param>
        /// <returns><c>-1</c> if the consumable does not exist, otherwise the amount the player has</returns>
        public int GetConsumableAmount(string name)
        {
            // Check if consumable exists
            if (!_consumableInventory.ContainsKey(name))
            {
#if DEBUG
                if (_unlockableInventory.ContainsKey(name)) Debug.Log(
                    $"WARNING: Consumable with identifier {name} does not exist. As {name} is an unlockable, did you mean to call GetConsumableAmount?");
                else Debug.LogWarning(
                    $"WARNING: Consumable with identifier {name} does not exist.");
#endif
                return -1;
            }
            
            // Then get value
            return _consumableInventory[name];
        }
        #endregion
        #region DURABILITY

        /// <summary>
        /// Call this function to consume the durability of an entry with durability
        /// </summary>
        /// <param name="name">The name of the durability entry to be modified</param>
        public void ConsumeDurability(string name)
        {
            #if DEBUG
            if (!_durabilityTracker.ContainsKey(name))
            {
                Debug.LogWarning($"WARNING: No durability entry called {name}, this function call will do nothing");
                return;
            }
            #endif
            PlayerDurabilityEntry ent = _durabilityTracker[name];
            ent.currDurability--;
            _durabilityTracker[name] = ent;
        }

        public const int DURABILITY_REPAIR_SUCCESS = 0;
        public const int DURABILITY_REPAIR_UNNEEDED = 1;
        public const int DURABILITY_REPAIR_FAILURE = -1;
        
        /// <summary>
        /// A complete function to easily repair an item's durability. Specify the item and this
        /// function will check if the player has hammers, this function will also refrain from
        /// </summary>
        /// <param name="name">Entry to repair</param>
        /// <returns><c>PlayerInventory.DURABILITY_REPAIR_SUCCESS</c> if successful, <c>PlayerInventory.DURABILITY_REPAIR_UNNEEDED if item does not need repairing, no hammers will be consumed, highest priority <c>PlayerInventory.DURABILITY_REPAIR_FAILURE if error or no hammers</c></returns>
        public int RepairDurability(string name)
        {
            #if DEBUG
            if (!_durabilityTracker.ContainsKey(name))
            {
                Debug.LogWarning($"WARNING: No durability entry called {name}, this function call will return {DURABILITY_REPAIR_FAILURE}");
                return DURABILITY_REPAIR_FAILURE;
            }
            #endif

            if (GetConsumableAmount("hammer") <= 0) return DURABILITY_REPAIR_FAILURE;
            PlayerDurabilityEntry ent = _durabilityTracker[name];
            if (ent.currDurability >= ent.maxDurability) return DURABILITY_REPAIR_UNNEEDED;
            AccumulateConsumable("hammer", -1);
            ent.currDurability = ent.maxDurability;
            _durabilityTracker[name] = ent;
            return DURABILITY_REPAIR_SUCCESS;
        }

        /// <summary>
        /// Call this function to check the durability of an item/entry.
        /// </summary>
        /// <param name="name">The durability entry to be checked</param>
        /// <returns>true if the durability entry called for has a current durability greater than 0</returns>
        public bool HasDurability(string name)
        {
            #if DEBUG
            if (!_durabilityTracker.ContainsKey(name))
            {
                Debug.LogWarning($"WARNING: No durability entry called {name}, this function call will return false");
                return false;
            }
            #endif
            return _durabilityTracker[name].currDurability > 0;
        }
        #endregion
        
        /// <summary>
        /// This serializable struct represents an entry for a type of consumable.
        /// </summary>
        [System.Serializable]
        internal struct PlayerConsumableEntry {
            [SerializeField] internal string name;
            [SerializeField] internal int startingCount;
        }

        [System.Serializable]
        internal struct PlayerUnlockableEntry
        {
            [SerializeField] internal string name;
            [SerializeField] internal bool startsWith;
        }

        [System.Serializable]
        internal struct PlayerDurabilityEntry
        {
            [SerializeField] internal string name;
            [SerializeField] internal int maxDurability;
            [SerializeField] internal int currDurability;
        }
    }
}

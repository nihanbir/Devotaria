using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace NodeGraph
{
    [CreateAssetMenu(fileName = "RoomNodeTypeListSO", menuName = "Scriptable Objects/Dungeon/Room Node Type List")]
    public class RoomNodeTypeListSO : ScriptableObject
    {
        #region Header ROOM NODE TYPE LIST
        [Space(10)]
        [Header("ROOM NODE TYPE LIST")]
        #endregion
        #region Tooltip
        [Tooltip("This list should be populated with all the RoomNodeTypes that are used in the game - " +
                 "it is used instead of enumerations to allow for easy editing of the list in the editor.")]
        #endregion
        public List<RoomNodeTypeSO> list;

        #region Validation
#if UNITY_EDITOR
        private void OnValidate()
        {
            HelperUtilities.ValidateCheckEnumerableValues(this, nameof(list), list);
        }
#endif
        #endregion
    }
}

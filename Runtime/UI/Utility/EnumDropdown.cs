using System;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Binds the values of an enum to a Dropdown control.</summary>
    public class EnumDropdown<TEnum> : EnumDropdownBase
        where TEnum : struct, IConvertible
    {
        // ---------[ Functionality ]---------
        /// <summary>Returns the selected value of the dropdown as an enum representation.</summary>
        public bool TryGetSelectedValue(out TEnum enumValue)
        {
            int selectionIndex = this.GetComponent<Dropdown>().value;

            EnumDropdownBase.EnumSelectionPair pair;
            if(this.TryGetPairForSelection(selectionIndex, out pair)
               && Enum.IsDefined(typeof(TEnum), pair.enumValue))
            {
                enumValue = (TEnum)Enum.ToObject(typeof(TEnum), pair.enumValue);
                return true;
            }

            enumValue = default(TEnum);
            return false;
        }

        // ---------[ Utility ]---------
        /// <summary>Gets the names of the enum options.</summary>
        public override string[] GetEnumNames()
        {
            return Enum.GetNames(typeof(TEnum));
        }

        /// <summary>Gets the values of the enum options.</summary>
        public override int[] GetEnumValues()
        {
            return (int[])Enum.GetValues(typeof(TEnum));
        }
    }

    /// <summary>Binds the values of an enum to a Dropdown control.</summary>
    [DisallowMultipleComponent]
    public abstract class EnumDropdownBase : MonoBehaviour
    {
        // ---------[ Nested Data-Types ]---------
        /// <summary>Enum-Dropdown Selection index pairing.</summary>
        [Serializable]
        public struct EnumSelectionPair
        {
            public int selectionIndex;
            public int enumValue;
        }

        // ---------[ Fields ]---------
        /// <summary>Enum-Dropdown Selection index pairing.</summary>
        public EnumSelectionPair[] enumSelectionPairings = new EnumSelectionPair[0];

        // ---------[ Interface ]---------
        /// <summary>Gets the names of the enum options.</summary>
        public abstract string[] GetEnumNames();

        /// <summary>Gets the values of the enum options.</summary>
        public abstract int[] GetEnumValues();

        // ---------[ Functionality ]---------
        /// <summary>Gets the enum-selection pair for the given selection index.</summary>
        public bool TryGetPairForSelection(int selectionIndex, out EnumSelectionPair result)
        {
            if(this.enumSelectionPairings != null && this.enumSelectionPairings.Length > 0)
            {
                foreach(var pair in this.enumSelectionPairings)
                {
                    if(pair.selectionIndex == selectionIndex)
                    {
                        result = pair;
                        return true;
                    }
                }
            }

            result = new EnumSelectionPair() { selectionIndex = -1, enumValue = -1 };
            return false;
        }

        /// <summary>Gets the enum-selection pair for the given enum value.</summary>
        public bool TryGetPairForEnum(int enumValue, out EnumSelectionPair result)
        {
            if(this.enumSelectionPairings != null && this.enumSelectionPairings.Length > 0)
            {
                foreach(var pair in this.enumSelectionPairings)
                {
                    if(pair.enumValue == enumValue)
                    {
                        result = pair;
                        return true;
                    }
                }
            }

            result = new EnumSelectionPair() { selectionIndex = -1, enumValue = -1 };
            return false;
        }
    }
}

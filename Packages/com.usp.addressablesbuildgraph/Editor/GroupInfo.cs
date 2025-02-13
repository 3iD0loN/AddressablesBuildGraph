using System;
using System.Collections.Generic;

using UnityEditor.AddressableAssets.Settings;

namespace USP.AddressablesBuildGraph
{
    public class GroupInfo : IEqualityComparer<GroupInfo>
    {
        #region Static Methods
        public static bool operator ==(GroupInfo leftHand, GroupInfo rightHand)
        {
            var lhs = (object)leftHand;
            var rhs = (object)rightHand;

            if (lhs == rhs)
            {
                return true;
            }

            if (rhs == null || lhs == null)
            {
                return false;
            }

            return string.Compare(leftHand.Guid, rightHand.Guid, StringComparison.Ordinal) == 0;
        }

        public static bool operator !=(GroupInfo lhs, GroupInfo rhs)
        {
            return !(lhs == rhs);
        }
        #endregion

        #region Properties
        public string Guid { get; }

        public string Name { get; }

        public bool IsDefault { get; }

        public bool IsReadOnly { get; }

        /// <summary>
        /// Gets a unique set of asset bundles that are generated from the group by the build.
        /// </summary>
        public HashSet<AssetBundleInfo> AssetBundles { get; }
        #endregion

        #region Methods
        #region Constructors
        public GroupInfo(AddressableAssetGroup group) :
            this(group.Name, group.Guid, group.Default, group.ReadOnly)
        {
        }

        public GroupInfo(string name, string guid, bool isDefault = false, bool isReadOnly = false)
        {
            this.Guid = guid;
            this.Name = name;
            this.IsDefault = isDefault;
            this.IsReadOnly = isReadOnly;
            this.AssetBundles = new HashSet<AssetBundleInfo>();
        }
        #endregion

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is not GroupInfo asset)
            {
                return false;
            }

            return this == asset;
        }

        public int GetHashCode(GroupInfo obj)
        {
            return obj.GetHashCode();
        }

        public bool Equals(GroupInfo lhs, GroupInfo rhs)
        {
            return lhs == rhs;
        }

        public override string ToString()
        {
            return Name;
        }
        #endregion
    }
}
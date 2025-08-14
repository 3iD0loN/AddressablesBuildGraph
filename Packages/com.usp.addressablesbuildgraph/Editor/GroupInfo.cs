using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace USP.AddressablesBuildGraph
{
    [Serializable]
    public class GroupInfo : IEqualityComparer<GroupInfo>, ISerializationCallbackReceiver
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

        #region Fields
        [SerializeReference]
        private string guid;

        [SerializeReference]
        private string name;

        [SerializeReference]
        private bool isDefault;

        [SerializeReference]
        private bool isReadOnly;

        /// <summary>
        ///  A unique set of asset bundles that are generated from the group by the build.
        /// </summary>
        [SerializeReference]
        private AssetBundleInfo[] assetBundles;
        #endregion

        #region Properties
        public string Guid => guid;

        public string Name => name;

        public bool IsDefault => isDefault;

        public bool IsReadOnly => isReadOnly;

        /// <summary>
        /// Gets a unique set of asset bundles that are generated from the group by the build.
        /// </summary>
        public HashSet<AssetBundleInfo> AssetBundles { get; private set; }
        #endregion

        #region Methods
        #region Constructors
        public GroupInfo(AddressableAssetGroup group) :
            this(group.Name, group.Guid, group.Default, group.ReadOnly, null)
        {
        }

        public GroupInfo(string name, string guid, bool isDefault = false, bool isReadOnly = false, HashSet<AssetBundleInfo> assetBundles = null)
        {
            this.guid = guid;
            this.name = name;
            this.isDefault = isDefault;
            this.isReadOnly = isReadOnly;
            this.AssetBundles = assetBundles != null ? new HashSet<AssetBundleInfo>(assetBundles) : new HashSet<AssetBundleInfo>();
        }
        #endregion

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is not GroupInfo group)
            {
                return false;
            }

            return Equals(this, group);
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
            return EditorJsonUtility.ToJson(this, true);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            assetBundles = new AssetBundleInfo[AssetBundles.Count];
            AssetBundles.CopyTo(assetBundles);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            AssetBundles = new HashSet<AssetBundleInfo>(assetBundles);
        }
        #endregion
    }
}
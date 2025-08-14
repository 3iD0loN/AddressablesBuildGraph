using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace USP.AddressablesBuildGraph
{
    [Serializable]
    public class AssetBundleInfo : IEqualityComparer<AssetBundleInfo>, ISerializationCallbackReceiver
    {
        #region Static Methods
        public static bool operator ==(AssetBundleInfo leftHand, AssetBundleInfo rightHand)
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

            return string.Compare(leftHand.AssetBundleName, rightHand.AssetBundleName, StringComparison.Ordinal) == 0;
        }

        public static bool operator !=(AssetBundleInfo lhs, AssetBundleInfo rhs)
        {
            return !(lhs == rhs);
        }
        #endregion

        #region Fields
        [SerializeReference]
        private string assetBundleName;

        //[SerializeReference]
        //private string assetBundleVariant { get; }

        /// <summary>
        /// A unique set of all assets that are packed into a bundle.
        /// </summary>
        [SerializeReference]
        private AssetInfo[] assets;

        /// <summary>
        /// The Addressbles group used to generate the asset bundle.
        /// </summary>
        [SerializeReference]
        private GroupInfo group;
        #endregion

        #region Properties
        public string AssetBundleName => assetBundleName;

        //public string AssetBundleVariant => assetBundleVariant;

        /// <summary>
        /// Gets a unique set of all assets that are packed into a bundle.
        /// </summary>
        public HashSet<AssetInfo> Assets { get; private set; }

        /// <summary>
        /// Gets the Addressables group used to generate the asset bundle.
        /// </summary>
        public GroupInfo Group
        {
            get => group;
            set => group = value;
        }
        #endregion

        #region Methods
        public AssetBundleInfo(string assetBundleName, HashSet<AssetInfo> assets = null)
        {
            this.assetBundleName = assetBundleName;
            this.Assets = assets != null ? new HashSet<AssetInfo>(assets) : new HashSet<AssetInfo>();
        }

        public override int GetHashCode()
        {
            return AssetBundleName.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is not AssetBundleInfo assetBundle)
            {
                return false;
            }

            return Equals(this, assetBundle);
        }

        public int GetHashCode(AssetBundleInfo obj)
        {
            return obj.GetHashCode();
        }

        public bool Equals(AssetBundleInfo lhs, AssetBundleInfo rhs)
        {
            return lhs == rhs;
        }

        public override string ToString()
        {
            return EditorJsonUtility.ToJson(this, true);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            assets = new AssetInfo[Assets.Count];
            Assets.CopyTo(assets);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Assets = new HashSet<AssetInfo>(assets);
        }
        #endregion
    }
}
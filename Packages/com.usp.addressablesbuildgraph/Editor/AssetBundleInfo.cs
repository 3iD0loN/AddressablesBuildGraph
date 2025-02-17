using System;
using System.Collections.Generic;

namespace USP.AddressablesBuildGraph
{
    public class AssetBundleInfo : IEqualityComparer<AssetBundleInfo>
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

        #region Properties
        public string AssetBundleName { get; }

        //public string assetBundleVariant { get; }

        /// <summary>
        /// Gets a unique set of all assets that are packed into a bundle.
        /// </summary>
        public HashSet<AssetInfo> Assets { get; }

        /// <summary>
        /// Gets the Addressbles group used to generate the asset bundle.
        /// </summary>
        public GroupInfo Group { get; set; }
        #endregion

        #region Methods
        public AssetBundleInfo(string assetBundleName)
        {
            this.AssetBundleName = assetBundleName;
            this.Assets = new HashSet<AssetInfo>();
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
            return AssetBundleName;
        }
        #endregion
    }
}
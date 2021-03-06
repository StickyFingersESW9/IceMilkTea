﻿// zlib/libpng License
//
// Copyright (c) 2018 Sinoa
//
// This software is provided 'as-is', without any express or implied warranty.
// In no event will the authors be held liable for any damages arising from the use of this software.
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it freely,
// subject to the following restrictions:
//
// 1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software.
//    If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.

namespace IceMilkTea.Service
{
    /// <summary>
    /// 特定コンテンツグループで利用するアセットバンドル情報を保持した構造体です
    /// </summary>
    [System.Serializable]
    public struct AssetBundleContentGroup
    {
        /// <summary>
        /// コンテンツグループ名
        /// </summary>
        public string Name;


        /// <summary>
        /// コンテンツグループが保持しているアセットバンドル情報の配列
        /// </summary>
        public AssetBundleInfo[] AssetBundleInfos;



        /// <summary>
        /// コンテンツグループが保持している、アセットバンドル情報全てのサイズの合計値
        /// </summary>
        public long TotalAssetBundleSize
        {
            get
            {
                // トータルサイズを記憶する変数を宣言
                var totalSize = 0L;


                // コンテンツグループが保持しているアセットバンドル情報分回る
                for (int i = 0; i < AssetBundleInfos.Length; ++i)
                {
                    // サイズを加算する
                    totalSize += AssetBundleInfos[i].Size;
                }


                // 結果を返す
                return totalSize;
            }
        }
    }
}
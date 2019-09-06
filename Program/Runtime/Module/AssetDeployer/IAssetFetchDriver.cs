﻿// zlib/libpng License
//
// Copyright (c) 2019 Sinoa
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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IceMilkTea.Module
{
    /// <summary>
    /// アセットの実際のフェッチを行うインターフェイスです
    /// </summary>
    public interface IAssetFetchDriver : IDisposable
    {
        /// <summary>
        /// アセットのフェッチを非同期で行い対象のストリームに出力します
        /// </summary>
        /// <param name="outStream">出力先のストリーム</param>
        /// <param name="cancellationToken">キャンセル要求を監視するためのトークン</param>
        /// <returns>フェッチ処理を実行しているタスクを返します</returns>
        Task FetchAsync(Stream outStream, CancellationToken cancellationToken);
    }
}
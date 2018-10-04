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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using IceMilkTea.Core;
using UnityEngine.Networking;

namespace IceMilkTea.Service
{
    #region サービス本体の実装
    /// <summary>
    /// ゲームアセットをゲームで利用できるようにするために取り込む機能を提供するサービスクラスです
    /// </summary>
    public class AssetFetchService : GameService
    {
        // クラス変数宣言
        private static readonly IProgress<AssetFetchProgressInfo> DefaultProgress = new Progress<AssetFetchProgressInfo>();

        // メンバ変数定義
        private AssetFetcherProvider fetcherProvider;
        private AssetInstallerProvider installerProvider;



        /// <summary>
        /// AssetFetchService のインスタンスを初期化します
        /// </summary>
        public AssetFetchService()
        {
            // サブシステムの生成
            fetcherProvider = new AssetFetcherProvider();
            installerProvider = new AssetInstallerProvider();
        }


        /// <summary>
        /// AssetFetcher を登録します
        /// </summary>
        /// <param name="fetcher">登録する AssetFetcher</param>
        public void RegisterFetcher(AssetFetcher fetcher)
        {
            // プロバイダにそのまま流し込む
            fetcherProvider.AddFetcher(fetcher);
        }


        /// <summary>
        /// AssetInstaller を登録します
        /// </summary>
        /// <param name="installer">登録する AssetInstaller</param>
        public void RegisterInstaller(AssetInstaller installer)
        {
            // プロバイダにそのまま流し込む
            installerProvider.AddInstaller(installer);
        }


        /// <summary>
        /// 指定されたフェッチURLからインストールURLに、アセットを非同期でフェッチします。
        /// </summary>
        /// <param name="fetchUrl">フェッチする基になるフェッチURL</param>
        /// <param name="installUrl">フェッチしたアセットをインストールするインストール先URL</param>
        /// <param name="progress">フェッチの進捗通知を受ける IProgress 不要な場合は null の指定が可能です</param>
        /// <returns>アセットのフェッチを非同期操作しているタスクを返します</returns>
        public IAwaitable FetchAssetAsync(string fetchUrl, string installUrl, IProgress<AssetFetchProgressInfo> progress)
        {
            // assetUrlが文字列として不適切なら
            if (string.IsNullOrWhiteSpace(fetchUrl))
            {
                // 例外を吐く
                throw new ArgumentException("指定されたフェッチURLが無効です", nameof(fetchUrl));
            }


            // installUrlが文字列として不適切なら
            if (string.IsNullOrWhiteSpace(installUrl))
            {
                // 例外を吐く
                throw new ArgumentException("指定されたインストールURLが無効です", nameof(installUrl));
            }


            // フェッチURLとインストールURLのインスタンスを生成
            var fetchUri = new Uri(fetchUrl);
            var installUri = new Uri(installUrl);


            // フェッチURLからアセットフェッチするフェッチャーを取得するが、担当が見つからなかったら
            var fetcher = fetcherProvider.GetFetcher(fetchUri);
            if (fetcher == null)
            {
                // ごめんなさい、フェッチ出来ません
                throw new InvalidOperationException("指定されたフェッチURLの対応が可能な fetcher が見つかりませんでした");
            }


            // インストールURLからアセットのインストーラを取得するが、担当が見つからなかったら
            var installer = installerProvider.GetInstaller(installUri);
            if (installer == null)
            {
                // ごめんなさい、インストールできません
                throw new InvalidOperationException("指定されたインストールURLの対応が可能な installer が見つかりませんでした");
            }


            // インストーラからインストールストリームを開いてもらい、フェッチャーに渡してアセットフェッチを開始
            var installStream = installer.Open(installUri);
            var fetchTask = fetcher.FetchAssetAsync(fetchUri, installStream, progress ?? DefaultProgress);


            // クリーンアップ作業を非同期的に行いながら非同期操作タスクを返す
            DoCleanupAsync(installer, fetchTask);
            return fetchTask;
        }


        /// <summary>
        /// フェッチが終わったときのクリーンアップを非同期に行います
        /// </summary>
        /// <param name="installer">クリーンアップするインストーラ</param>
        /// <param name="fetchTask">フェッチャーのフェッチタスク</param>
        private async void DoCleanupAsync(AssetInstaller installer, IAwaitable fetchTask)
        {
            // フェッチが終わるまでまって、終わったらインストーラを閉じる
            await fetchTask;
            installer.Close();
        }
    }
    #endregion



    #region 進捗通知情報の定義
    /// <summary>
    /// アセットフェッチ進捗の通知内容を保持した構造体です
    /// </summary>
    public struct AssetFetchProgressInfo
    {
        /// <summary>
        /// フェッチ中のフェッチURL
        /// </summary>
        public string FetchUrl { get; private set; }


        /// <summary>
        /// アセットのフェッチ進捗率を正規化した値
        /// </summary>
        public double Progress { get; private set; }



        /// <summary>
        /// AssetFetchProgressInfo のインスタンスを初期化します
        /// </summary>
        /// <param name="fetchUrl">フェッチするURL</param>
        /// <param name="progress">フェッチ進捗</param>
        public AssetFetchProgressInfo(string fetchUrl, double progress)
        {
            // 受け取った値をそのまま受ける
            FetchUrl = fetchUrl;
            Progress = progress;
        }
    }
    #endregion



    #region AssetInstallerの抽象クラスとProviderの実装
    /// <summary>
    /// 複数の AssetInstaller を保持し指定されたインストールURLから AssetInstaller を提供するクラスです
    /// </summary>
    internal class AssetInstallerProvider
    {
        // メンバ変数定義
        private List<AssetInstaller> installerList;



        /// <summary>
        /// AssetInstallerProvider のインスタンスを初期化します
        /// </summary>
        public AssetInstallerProvider()
        {
            // インストーラリストの生成
            installerList = new List<AssetInstaller>();
        }


        /// <summary>
        /// 指定されたインストーラを管理リストに追加します
        /// </summary>
        /// <param name="installer">追加するインストーラ</param>
        /// <exception cref="ArgumentNullException">installer が null です</exception>
        /// <exception cref="InvalidOperationException">既に登録済みの installer です</exception>
        public void AddInstaller(AssetInstaller installer)
        {
            // null を渡されたら
            if (installer == null)
            {
                // 何もできない
                throw new ArgumentNullException(nameof(installer));
            }


            // 既に追加済みの installer なら
            if (installerList.Contains(installer))
            {
                // もう追加出来ない
                throw new InvalidOperationException("既に登録済みの installer です");
            }


            // インストーラを追加する
            installerList.Add(installer);
        }


        /// <summary>
        /// 指定されたインストールURLから対応可能なら AssetInstaller を取得します
        /// </summary>
        /// <param name="url">インストールする予定のインストールURL</param>
        /// <returns>対応可能な AssetInstaller のインスタンスを返しますが、見つからなかった場合は null を返します</returns>
        /// <exception cref="ArgumentNullException">url が null です</exception>
        public AssetInstaller GetInstaller(Uri url)
        {
            // null を渡されていたら
            if (url == null)
            {
                // どんなインストーラをご所望ですか
                throw new ArgumentNullException(nameof(url));
            }


            // 管理しているインストーラの数分ループ
            foreach (var installer in installerList)
            {
                // インストーラから指定されたURLは対応可能と返却されたら
                if (installer.CanResolve(url))
                {
                    // このインストーラを返す
                    return installer;
                }
            }


            // 結局見つからなかった
            return null;
        }
    }



    /// <summary>
    /// アセットを実際にインストールするインストーラクラスです
    /// </summary>
    public abstract class AssetInstaller
    {
        /// <summary>
        /// 要求されたインストールURLの解決が可能かどうかを判断します
        /// </summary>
        /// <param name="installUrl">要求されているインストールURL</param>
        /// <returns>要求されているURLのインストールが可能な場合は true を、不可能であれば false を返します</returns>
        public abstract bool CanResolve(Uri installUrl);


        /// <summary>
        /// 指定されたインストールURLのインストールストリームを開きます
        /// </summary>
        /// <param name="installUrl">要求されているインストールURL</param>
        /// <returns>指定されたインストールURLにインストールするためのストリームインスタンスを返します</returns>
        public abstract Stream Open(Uri installUrl);


        /// <summary>
        /// 開いたインストールストリームを閉じます
        /// </summary>
        public abstract void Close();
    }
    #endregion



    #region AssetFetcherの抽象クラスとProviderの実装
    /// <summary>
    /// 複数の AssetFetcher を保持し指定されたフェッチURLから AssetFetcher を提供するクラスです
    /// </summary>
    internal class AssetFetcherProvider
    {
        // メンバ変数定義
        private List<AssetFetcher> fetcherList;



        /// <summary>
        /// AssetFetcherProvider のインスタンスを初期化します
        /// </summary>
        public AssetFetcherProvider()
        {
            // フェッチャーリストの初期化
            fetcherList = new List<AssetFetcher>();
        }


        /// <summary>
        /// 指定された AssetFetcher を管理リストに追加します
        /// </summary>
        /// <param name="fetcher">追加する AssetFetcher</param>
        /// <exception cref="ArgumentNullException">fetcher が null です</exception>
        /// <exception cref="InvalidOperationException">既に登録済みの fetcher です</exception>
        public void AddFetcher(AssetFetcher fetcher)
        {
            // null が渡されたら
            if (fetcher == null)
            {
                // 何も出来ません
                throw new ArgumentNullException(nameof(fetcher));
            }


            // 既に追加済みなら
            if (fetcherList.Contains(fetcher))
            {
                // 多重登録は許されない
                throw new InvalidOperationException("既に登録済みの fetcher です");
            }


            // AssetFetcherを追加する
            fetcherList.Add(fetcher);
        }


        /// <summary>
        /// 指定されたフェッチURLの対応可能な AssetFetcher を取得します
        /// </summary>
        /// <param name="url">要求されているフェッチURL</param>
        /// <returns>対応可能な AssetFetcher のインスタンスを返しますが、見つからなかった場合は null を返します</returns>
        /// <exception cref="ArgumentNullException">url が null です</exception>
        public AssetFetcher GetFetcher(Uri url)
        {
            // null を渡されたら
            if (url == null)
            {
                // どんなフェッチャーがお望みですか
                throw new ArgumentNullException(nameof(url));
            }


            // 管理しているフェッチャーの数分ループする
            foreach (var fetcher in fetcherList)
            {
                // 対応可能と返答が来たのなら
                if (fetcher.CanResolve(url))
                {
                    // この AssetFetcher を返す
                    return fetcher;
                }
            }


            // 対応可能な AssetFetcher がいなかった
            return null;
        }
    }



    /// <summary>
    /// アセットを実際にフェッチするフェッチャークラスです
    /// </summary>
    public abstract class AssetFetcher
    {
        /// <summary>
        /// 指定されたフェッチURLが対応可能かどうか判断します
        /// </summary>
        /// <param name="fetchUrl">要求されているフェッチURL</param>
        /// <returns>要求されているフェッチURLの対応が可能な場合は true を、不可能の場合は false を返します</returns>
        public abstract bool CanResolve(Uri fetchUrl);


        /// <summary>
        /// 非同期に、指定されたフェッチURLからアセットをフェッチし、インストーラから渡されたストリームに書き込みます
        /// </summary>
        /// <param name="fetchUrl">フェッチするURL</param>
        /// <param name="installStream">フェッチしたデータを書き込むインストーラが開いたインストールストリーム</param>
        /// <param name="progress">フェッチ状況の進捗通知をする IProgress</param>
        /// <returns>アセットのフェッチを非同期しているタスクを返します</returns>
        public abstract IAwaitable FetchAssetAsync(Uri fetchUrl, Stream installStream, IProgress<AssetFetchProgressInfo> progress);
    }
    #endregion



    #region AssetInstaller for FileStream
    /// <summary>
    /// ファイルストリームを使った比較的単純なインストーラクラスです
    /// </summary>
    public class FileStreamAssetInstaller : AssetInstaller
    {
        // 定数定義
        private const string InstallSchemeName = "install";
        private const string FileStreamHostName = "filestream";

        // メンバ変数定義
        private DirectoryInfo baseDirectoryInfo;
        private FileStream fileStream;



        /// <summary>
        /// FileStreamAssetInstaller のインスタンスを初期化します
        /// </summary>
        /// <param name="baseDirectoryPath">インストールする先のベースディレクトリパス</param>
        /// <exception cref="ArgumentNullException"></exception>
        public FileStreamAssetInstaller(string baseDirectoryPath)
        {
            // nullを渡されたら
            if (baseDirectoryPath == null)
            {
                // 流石にインストールできない
                throw new ArgumentNullException(nameof(baseDirectoryPath));
            }


            // DirectoryInfoとしてパスを覚える
            baseDirectoryInfo = new DirectoryInfo(baseDirectoryPath);
        }


        /// <summary>
        /// 指定されたインストールURLが対応可能かどうかを判断します
        /// </summary>
        /// <param name="installUrl">対応するインストールURL</param>
        /// <returns>対応可能な場合は true を、不可能な場合は false を返します</returns>
        public override bool CanResolve(Uri installUrl)
        {
            // スキーム名とホスト名が対応できる文字列なら
            if (installUrl.Scheme == InstallSchemeName && installUrl.Host == FileStreamHostName)
            {
                // 対応できるとして返す
                return true;
            }


            // 対応出来ない
            return false;
        }


        /// <summary>
        /// インストールをするために、インストールストリームを開きます
        /// </summary>
        /// <param name="installUrl">インストールする先のURL</param>
        /// <returns>指定されたインストールURLに対して開いたストリームを返します</returns>
        public override Stream Open(Uri installUrl)
        {
            // 最終的なインストールファイル情報を用意する
            var installFilePath = Path.Combine(baseDirectoryInfo.FullName, installUrl.LocalPath.TrimStart('/'));
            var installFileInfo = new FileInfo(installFilePath);


            // ディレクトリが存在しないなら
            if (!installFileInfo.Directory.Exists)
            {
                // ディレクトリを作成する
                installFileInfo.Directory.Create();
            }


            // ローカルパスに対して書き込みのファイルストリームを開いて返す
            return fileStream = installFileInfo.OpenWrite();
        }


        /// <summary>
        /// 開いたインストールストリームを閉じます
        /// </summary>
        public override void Close()
        {
            // ファイルストリームを閉じる
            fileStream?.Dispose();
            fileStream = null;
        }
    }
    #endregion



    #region AssetFetcher for UnityWebRequest
    /// <summary>
    /// UnityのUnityWebRequestを使ったアセットデータのフェッチを行うフェッチャークラスです
    /// </summary>
    public class UnityWebRequestAssetFetcher : AssetFetcher
    {
        /// <summary>
        /// 指定されたフェッチURLが対応可能かどうか判断します
        /// </summary>
        /// <param name="fetchUrl">対応するフェッチURL</param>
        /// <returns>対応可能な場合は true を、不可能な場合は false を返します</returns>
        public override bool CanResolve(Uri fetchUrl)
        {
            // HTTP系スキームなら
            if (fetchUrl.IsHttpScheme())
            {
                // わりといける
                return true;
            }


            // HTTP以外はお断り
            return false;
        }


        /// <summary>
        /// 非同期に、指定されたフェッチURLからアセットフェッチします
        /// </summary>
        /// <param name="fetchUrl">フェッチするアセットがあるフェッチURL</param>
        /// <param name="installStream">フェッチしたアセットを書き込むインストールストリーム</param>
        /// <param name="progress">フェッチ進捗を通知する IProgress</param>
        /// <returns>アセットのフェッチを非同期操作しているタスクを返します</returns>
        public override IAwaitable FetchAssetAsync(Uri fetchUrl, Stream installStream, IProgress<AssetFetchProgressInfo> progress)
        {
            // 非同期タスクを生成して返す
            return new ImtTask(async () =>
            {
                // UnityWebRequestでダウンロードをするためのインスタンスを生成し初期化をする
                var request = UnityWebRequest.Get(fetchUrl);
                request.downloadHandler = new DownloadHandlerStream(installStream);


                // 非同期にダウンロードを開始する
                await request.SendWebRequest()
                    .ToAwaitable(new Progress<float>(currentProgress =>
                    {
                        // 現在の進捗状態を通知する
                        progress.Report(new AssetFetchProgressInfo(fetchUrl.ToString(), currentProgress));
                    }));
            })
            .Run();
        }



        /// <summary>
        /// UnityWebRequestによってダウンロードされたデータを、指定されたストリームに書き込んでいくハンドラクラスです
        /// </summary>
        private sealed class DownloadHandlerStream : DownloadHandlerScript
        {
            // メンバ変数定義
            private Stream installStream;
            private int contentLength;
            private int downloadedLength;



            /// <summary>
            /// DownloadHandlerStream のインスタンスを初期化します
            /// </summary>
            /// <param name="installStream">インストーラによって渡された書き込むべき先のストリーム</param>
            public DownloadHandlerStream(Stream installStream) : base(new byte[4 << 10])
            {
                // ストリームを受け取る
                this.installStream = installStream;
            }


            /// <summary>
            /// コンテンツサイズを受け取ったハンドリングを行います
            /// </summary>
            /// <param name="contentLength">受信したコンテンツサイズ</param>
            protected override void ReceiveContentLength(int contentLength)
            {
                // コンテンツの大きさを覚える
                this.contentLength = contentLength;
            }


            /// <summary>
            /// データを受け取ったハンドリングを行います
            /// </summary>
            /// <param name="data">受け取ったデータバッファ</param>
            /// <param name="dataLength">実際に受け取ったサイズ</param>
            /// <returns>ダウンロードを継続する場合は true を、中断する場合は false を返します</returns>
            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                // ストリームにデータを書き込んで継続する
                downloadedLength += dataLength;
                installStream.Write(data, 0, dataLength);
                return true;
            }


            /// <summary>
            /// 現在の進捗を取得します
            /// </summary>
            /// <returns>現在の進捗を返します</returns>
            protected override float GetProgress()
            {
                return downloadedLength / (float)contentLength;
            }
        }
    }
    #endregion



    #region AssetFetcher for WebRequest
    /// <summary>
    /// C#の純粋なWebRequestを使ったアセットフェッチを行うフェッチャークラスです
    /// </summary>
    public class WebRequestAssetFetcher : AssetFetcher
    {
        // 定数定義
        private const long ProgressNotifyIntervalTime = 125;
        private const int DefaultTimeoutTime = 5000;

        // メンバ変数定義
        private int timeoutTime;



        /// <summary>
        /// WebRequestAssetFetcher のインスタンスを既定値で初期化します
        /// </summary>
        public WebRequestAssetFetcher() : this(DefaultTimeoutTime)
        {
        }


        /// <summary>
        /// WebRequestAssetFetcher のインスタンスを初期化します
        /// </summary>
        /// <param name="timeoutTime">リクエストがタイムアウトするまでの時間（ミリ秒）</param>
        /// <exception cref="ArgumentOutOfRangeException">タイムアウトの時間に負の値は利用できません</exception>
        public WebRequestAssetFetcher(int timeoutTime)
        {
            // もしタイムアウトの時間が0未満なら
            if (timeoutTime < 0)
            {
                // 流石に負の世界にはいけない
                throw new ArgumentOutOfRangeException(nameof(timeoutTime), "タイムアウトの時間に負の値は利用できません");
            }


            // タイムアウトの時間を覚える
            this.timeoutTime = timeoutTime;
        }


        /// <summary>
        /// 指定されたフェッチURLが対応可能かどうか判断します
        /// </summary>
        /// <param name="fetchUrl">対応するフェッチURL</param>
        /// <returns>対応可能な場合は true を、不可能な場合は false を返します</returns>
        public override bool CanResolve(Uri fetchUrl)
        {
            // HTTP系スキームなら
            if (fetchUrl.IsHttpScheme())
            {
                // わりといける
                return true;
            }


            // HTTP以外はお断り
            return false;
        }


        /// <summary>
        /// 非同期に、指定されたフェッチURLからアセットフェッチします
        /// </summary>
        /// <param name="fetchUrl">フェッチするアセットがあるフェッチURL</param>
        /// <param name="installStream">フェッチしたアセットを書き込むインストールストリーム</param>
        /// <param name="progress">フェッチ進捗を通知する IProgress</param>
        /// <returns>アセットのフェッチを非同期操作しているタスクを返します</returns>
        public override IAwaitable FetchAssetAsync(Uri fetchUrl, Stream installStream, IProgress<AssetFetchProgressInfo> progress)
        {
            // 非同期タスクを生成して返す
            return new ImtTask(async () =>
            {
                // WebRequestでまずはHTTPリクエストとタイムアウト用タスクの用意
                var request = WebRequest.CreateHttp(fetchUrl);
                var responseTask = request.GetResponseAsync();
                var timeoutTask = Task.Delay(timeoutTime);


                // レスポンスタスクとタイムアウトタスクの2つを待機してもしタイムアウトが先に終了したら
                var finishedTask = await Task.WhenAny(responseTask, timeoutTask);
                if (finishedTask == timeoutTask)
                {
                    // リクエストを中止して終了
                    request.Abort();
                    return;
                }


                // ここまで来たのならレスポンスが先に返ってきたということで結果を受け取る
                var response = (HttpWebResponse)responseTask.Result;


                // もしステータスコードでOK以外が返ってきたら
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    // レスポンスを破棄して終了
                    response.Dispose();
                    return;
                }


                // コンテンツの長さを拾う（念の為コンテンツ長が取り出せないヘッダが返ってきた場合はlongMaxでごまかす）
                var contentLength = response.ContentLength;
                contentLength = contentLength > 0 ? contentLength : long.MaxValue;


                // 読み書き用バッファと書き込みトータル数通知用判定タイマーの宣言
                var judgeNotifyTimer = Stopwatch.StartNew();
                var buffer = new byte[4 << 10];
                var writeTotal = 0L;


                // ネットワークストリームを取得
                using (var networkStream = response.GetResponseStream())
                {
                    // 非同期で読み込んで、読み込みの長さが0になるまでループ
                    var readSize = 0;
                    while ((readSize = await networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        // 読み込まれたデータを非同期で書き込んで書き込んだトータルにも加算
                        await installStream.WriteAsync(buffer, 0, readSize);
                        writeTotal += readSize;


                        // もし通知するための時間が経過していたら
                        if (judgeNotifyTimer.ElapsedMilliseconds >= ProgressNotifyIntervalTime)
                        {
                            // 通知用データを作って通知して、タイマーをリスタート
                            progress.Report(new AssetFetchProgressInfo(fetchUrl.ToString(), writeTotal / (double)contentLength));
                            judgeNotifyTimer.Restart();
                        }
                    }
                }


                // インストールが完了したらレスポンスを破棄
                response.Dispose();
            })
            .Run();
        }
    }
    #endregion



    #region AssetFetcher for StreamingAssetReader
    /// <summary>
    /// UnityのStreamingAssetsに含まれるデータからアセットをフェッチするフェッチャークラスです
    /// </summary>
    public class StreamingAssetReaderAssetFetcher : AssetFetcher
    {
        // 定数定義
        private const string FetcherSchemeName = "fetch";
        private const string StreamingAssetsHostName = "streamingassets";
        private const long ProgressNotifyIntervalTime = 125;



        /// <summary>
        /// 指定されたフェッチURLが対応可能かどうか判断します
        /// </summary>
        /// <param name="fetchUrl">対応するフェッチURL</param>
        /// <returns>対応可能な場合は true を、不可能な場合は false を返します</returns>
        public override bool CanResolve(Uri fetchUrl)
        {
            // スキームとホスト名が対応可能な文字列なら
            if (fetchUrl.Scheme == FetcherSchemeName && fetchUrl.Host == StreamingAssetsHostName)
            {
                // 対応可能
                return true;
            }


            // そうでないなら対応不可
            return false;
        }


        /// <summary>
        /// 非同期に、指定されたフェッチURLからアセットフェッチします
        /// </summary>
        /// <param name="fetchUrl">フェッチするアセットがあるフェッチURL</param>
        /// <param name="installStream">フェッチしたアセットを書き込むインストールストリーム</param>
        /// <param name="progress">フェッチ進捗を通知する IProgress</param>
        /// <returns>アセットのフェッチを非同期操作しているタスクを返します</returns>
        public override IAwaitable FetchAssetAsync(Uri fetchUrl, Stream installStream, IProgress<AssetFetchProgressInfo> progress)
        {
            // 非同期タスクを生成して返す
            return new ImtTask(async () =>
            {
                // 読み書き用バッファと書き込みトータル数通知用判定タイマーの宣言
                var judgeNotifyTimer = Stopwatch.StartNew();
                var buffer = new byte[4 << 10];
                var writeTotal = 0L;


                // ストリーミングアセットを開く
                using (var stream = await StreamingAssetReader.OpenAsync(fetchUrl.LocalPath))
                {
                    // 非同期で読み込んで、読み込みの長さが0になるまでループ
                    var readSize = 0;
                    while ((readSize = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        // 読み込まれたデータを非同期で書き込んで書き込んだトータルにも加算
                        await installStream.WriteAsync(buffer, 0, readSize);
                        writeTotal += readSize;


                        // もし通知するための時間が経過していたら
                        if (judgeNotifyTimer.ElapsedMilliseconds >= ProgressNotifyIntervalTime)
                        {
                            // 通知用データを作って通知して、タイマーをリスタート（ストリーミングアセットはサイズの最大長が取り出せないので0.0進行率として通知）
                            progress.Report(new AssetFetchProgressInfo(fetchUrl.ToString(), 0.0));
                            judgeNotifyTimer.Restart();
                        }
                    }
                }
            })
            .Run();
        }
    }
    #endregion
}
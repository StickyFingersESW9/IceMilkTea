# 重要なお知らせ（Important Notice）

IceMilkTeaがこれから Ver1.0.0 に向けた最終的な姿になるため、これまで長い間様々な実験的実装や設計をしてきました。  
  
そして出た結論としては、IceMilkTeaは「ゲーム基板を構築するためのカーネルフレームワーク」と位置付けることにしました。  

具体的には「GameMain」及び「GameService」「PlayerLoopSystem」「SynchronizationContext」と言った、
ゲームアプリケーションルートオブジェクトとフローの制御を提供する事がIceMilkTeaの姿とすることにしました。

## 大幅な破壊的変更

Ver1.0.0に向けたIceMilkTeaは既存の実装から大幅に変更が入る為今まで実装していただいたバージョンとの互換性は"一切"ありません。  
そのため、パッケージを既にご利用の方はそのままアップデートを行うとコンパイルエラーなどが発生します。  
また、もともと存在していたいくつかの実装などもパッケージから削除されます。

## 設計方針について

設計変更に伴いもともとあったサブシステムや他のソースコード「特にImtStateMachineなど」は、別のパッケージとして提供する予定です。  
IceMilkTeaは今後「ゲーム基板を構築するためのカーネルフレームワーク」として提供し、そのフレームワークに搭載されるあらゆるサブシステムは「別のパッケージ」として提供するとともに、全く別の作者が実装したサブシステムを搭載出来るような仕組みになります。  
今後の設計に関するコメントがあればぜひIssueをご利用下さいませ。

# IceMilkTea（Unity Project）

## 説明(Description)

リポジトリルートに配置されたディレクトリ構造は Unity3D ゲームエンジン向け構造のため  
本体へのアクセスをする場合は、 [IceMilkTea（Packages/IceMilkTea/）](Packages/IceMilkTea/README.md) へアクセスしてください。  
もともと、Unityプロジェクトと IceMilkTea のリポジトリを分けていましたが、今後の IceMilkTea はパッケージ配信が主体になるため  
これを機にリポジトリ構成も変更という形になりました。

## 始め方(Getting Started)

未記入

### 導入方法(Installing)

未記入

## IceMilkTeaについて(About IceMilkTea)

### 作者(Author)

* Sinoa <sinoa.dev@gmail.com>

### ライセンス(License)

* zlib/libpng ライセンス
[zlib/libpng](https://opensource.org/licenses/Zlib)

## 履歴(Version)

未記入

## 開発貢献者(Development Contributor)

IceMilkTea（Unity Project）の、開発の協力や貢献をしてくれた方々のリストになります。

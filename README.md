# HandsOnMaterials

## Ex1_PlanCheck

用意されている関数は以下の通りです。

### 1. `GetPlanInfo(PlanSetup plan)`

Planの情報を取得し、文字列を返します。

### 2. `CheckImageFunc(PlanSetup plan)`

Imageに関するチェックを実行し、その結果を文字列で返します。

### 実行するチェック項目

- CT-EDテーブル名の一致の判定
- CT画像・計画体位の一致の判定
- CT画像の撮影日時の確認

### 3. `CheckPlanFunc(PlanSetup plan)`

Planに関するチェックを実行し、その結果を文字列で返します。

- プランIDの命名規則の判定
- 線量計算パラメータの判定

### 4. `CheckFieldFunc(PlanSetup plan)`

Fieldに関するチェックを実行し、その結果を文字列で返します。

- 治療装置名の判定
- Field IDの命名規則の判定
- アイソセンタ座標の判定
- 最小MU値の判定
- Jaw/MLC位置の判定
- 線量率の判定
- クリアランス（コリジョン）の判定

### 5. `CheckRPFunc(PlanSetup plan)`

Reference Pointに関するチェックを実行し、その結果を文字列で返します。

- ID命名規則の判定
- 座標の確認（体表面からの距離）
- CT値の判定

### 6. `CheckStructureFunc(PlanSetup plan)`

Structureに関するチェックを実行し、その結果を文字列で返します。

- 輪郭命名規則の判定
- 割り当てHU値の判定
- カウチ座標の判定

### 7. `CheckDoseFunc(PlanSetup plan)`

線量に関するチェックを実行し、その結果を文字列で返します。

- ターゲット線量の確認
- リスク臓器線量の確認

### 8. `MakeFormatText(bool judge, string checkName, string paraText)`

出力する文字列を整形する補助関数です。  
判定をbool値で `judge` に、チェックしている項目名を `checkName` に、判定がNGの場合のパラメータ文字列を `paraText` に渡すと、整形された文字列を返します。

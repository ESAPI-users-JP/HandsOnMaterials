# タイトル

## 目的

現在開いているプランについて、患者IDおよび患者名を取得し、Messageboxに表示する。

## 必要な情報

患者ID、患者名

## 与えられている引数

`PlanSetup`クラスのインスタンス`plan`

## 必要な情報へのアクセス方法

現在開いているプランから患者情報へアクセスするには、以下のようにします。

```csharp
var patient = plan.Course.Patient;
```

## 実装

上記に基づき、患者IDと患者名の取得を実装します。

```csharp
var patient = plan.Course.Patient;
var pid = patient.Id;
var pname = patient.LastName + " " + patient.FirstName;
```

細かいロジックが必要な場合は、都度説明を入れながらコードを分割してOKです。

## その他

### 改行
行末に半角スペースを2ついれるか、1行空行を入れると改行します。

### 画像
画像を貼り付ける場合は、画像を`img`フォルダに入れて

![fig1](../img/denkyuu_on.png)

のようにします。  
画像の名前がかぶらないように、`1_1.md`で使用する場合は`1_1`をファイル名の先頭に足しておいてください。

### 数式
数式は以下のように $$ で囲んでLaTeX記法で記述できます。

$$
A=\int_{a}^b (1+x+x^2)dx
$$

### 箇条書き
箇条書きは以下

- 箇条書き1
    - ネスト
- 箇条書き2

### 強調
**太字**, *斜体*, ~~否定~~, ==色付け== が可能。

### テーブル
Tableは以下

| Left align | Right align | Center align |
|:-----------|------------:|:------------:|
| This | This | This |
| column | column | column |
| will | will | will |
| be | be | be |
| left | right | center |
| aligned | aligned | aligned |

### その他の記法

[リンク先](https://qiita.com/mebiusbox2/items/a61d42878266af969e3c) にMkdocsで使用可能な記法の解説があります。

[この辺](https://qiita.com/mebiusbox2/items/a61d42878266af969e3c#-%E8%AD%A6%E5%91%8A%E6%96%87)はけっこう便利かもです。


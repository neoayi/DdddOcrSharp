# DdddOcrSharp
- [ddddocr-python(带带弟弟)](https://github.com/sml2h3/ddddocr) C#/NET移植版
-采用C#10.0标准。适配net6，net7，net8版本
- 请勿违反所在地区法律法规 合理合法使用本项目
- 本项目仅作为移植项目未经过大量测试 生产环境谨慎使用

`特别说明`
- c#代码参考以下作者库
- [ddddocr-Sharp-Ex](https://github.com/MadLongTom/ddddocr-Sharp-Ex)
- [DdddOcr.Net](https://github.com/itbencn/DdddOcr.Net)
- 同时修复2个库中部分BUG。移植slide_match，slide_comparison 2个函数，使项目整体功能与sml2h3大佬的python功能保持一致。

`移植进度`
- [x] classification 分类/OCR识别/文字识别
- [x] detection 目标检测
- [x] slide_match 滑块
- [x] slide_comparison 滑块

## 异步 API

本库提供异步封装（`ClassifyAsync` / `DetectAsync` / `Slide_ComparisonAsync` / `SlideMatchAsync`），可在 ASP.NET Core、WPF、WinForms 等场景下避免阻塞调用线程。

> OCR 推理本身是 CPU 密集型操作，异步 API 的主要价值在于**释放调用线程**，而不是加速推理本身。
> 同一 `DDDDOCR` 实例不保证多线程并发调用安全，并发场景请勿共享单一实例。

```csharp
using var ocr = new DDDDOCR(DdddOcrMode.ClassifyBeta);
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

byte[] imageBytes = File.ReadAllBytes("captcha.png");
string text = await ocr.ClassifyAsync(imageBytes, pngFix: false, cts.Token);
Console.WriteLine(text);
```

## 近期修复（非破坏性）

- **Detect 检测坐标修正**：`MakeGrid` 中索引公式由 `(i * hsize + j) * 2` 修正为 `(i * wsize + j) * 2`，解决目标检测在非正方形输入尺寸下的坐标错位问题。
- **张量预处理性能优化**：`Classify` / `Detect` 的像素→张量填充从逐像素 `Mat.Get<T>(y, x)` 改为 `GetGenericIndexer<T>()`，并将通道分支外提，预处理显著加速。
- **资源释放修复**：
  - `InferenceSession.Run(...)` 结果使用 `using` 释放，修复每次推理产生的原生资源泄漏。
  - `PngRgbaToRgbWhiteBackground` / `SlideMatch` / `Slide_Comparison` 中的 `Mat` 中间对象全部改为 `using` 释放。
  - `Slide_Comparison` 不再就地覆盖入参 `Mat`（原实现会把调用方的 `target` / `background` 强制转灰度）。
- **损坏图片校验**：`Classify` / `Detect` 中 `image.Width == 0 && image.Height == 0` 修正为 `||`。

## SlideMatch 对齐 Python 官方算法（⚠️ 破坏性变更）

本仓库 `master` 分支中的 `SlideMatch` 在某些样本（例如 `tg1/bg1`）上会把红框定位到背景顶部（`Top=5` 类似的明显错误位置）。排查后发现这是**仓库历史代码层面的缺陷**，并非 .NET 版本升级引入，具体表现为：

1. **算法与 Python `ddddocr` 官方实现不一致**：原实现额外加入了 `GaussianBlur → Threshold(128) → MorphologyEx(Close) → Canny(100,200) → GRAY2BGR` 的重预处理，在带花纹或高对比前景的背景图上容易将前景边缘误判为缺口边缘；Python 官方 `slide_match` 则只做 `cvtColor→Canny(50,150)→matchTemplate` 三步。
2. **返回坐标口径错乱**：`simpleTarget=false` 分支丢弃 `maxLoc.Y`、`simpleTarget=true` 分支又未加回裁剪偏移，两路返回值含义不一致。
3. **`GetTarget` 辅助函数** 在 Python v2 版本（`ddddocr/core/slide_engine.py`）中已不存在。

本仓库 `net10_upgrade` 分支**按 Python 官方算法重写了 `SlideMatch`**：

### API 变化

| 项 | 旧签名（master） | 新签名（net10_upgrade） |
|---|---|---|
| `SlideMatch` | `(Mat targetMat, Mat backgroundMat, int target_y = 0, bool simpleTarget = false, bool flag = false) → (int, Rect)` | `(Mat target, Mat background, bool simpleTarget = false) → SlideMatchResult` |
| `SlideMatchAsync` | 同上 + `CancellationToken` | `(Mat, Mat, bool = false, CancellationToken) → Task<SlideMatchResult>` |
| `GetTarget(Mat)` | `public static (Mat, Point)` | **已移除**（与 Python v2 保持一致） |

`SlideMatchResult` 结构（字段对应 Python 返回字典键）：

```csharp
public readonly struct SlideMatchResult
{
    public Rect   Target     { get; } // 滑块在背景中的矩形（左上角坐标 + 滑块尺寸）
    public int    TargetX    { get; } // 中心点 X（= Target.X + Target.Width / 2）
    public int    TargetY    { get; } // 中心点 Y（= Target.Y + Target.Height / 2）
    public double Confidence { get; } // matchTemplate 的 maxVal，范围 [-1, 1]
}
```

### 使用示例

```csharp
using var target     = new Mat("tg1.png", ImreadModes.AnyColor);
using var background = new Mat("bg1.png", ImreadModes.AnyColor);

// 复杂滑块（默认）：Canny 边缘后模板匹配
var r = DDDDOCR.SlideMatch(target, background);
// 简单滑块：直接灰度模板匹配
// var r = DDDDOCR.SlideMatch(target, background, simpleTarget: true);

Console.WriteLine($"rect={r.Target}, center=({r.TargetX},{r.TargetY}), conf={r.Confidence:F3}");
```

### 迁移指引（从旧签名）

```csharp
// 旧：
var (ty, rect) = DDDDOCR.SlideMatch(tg, bg, target_y: 44, simpleTarget: true);

// 新：
var r = DDDDOCR.SlideMatch(tg, bg, simpleTarget: true);
int ty = r.Target.Y;         // 如需原 target_y 语义可自行从 Target.Y 派生
Rect rect = r.Target;
```

> 备注：新实现对"输入的滑块图 `tg` 是完整全高条图（例如 65×170 带透明通道）"这种形态会返回"列式匹配"（X 正确、Y 为 0、Height 等于背景高度），与 Python 官方算法行为一致。若你的滑块图是这种形态，请在调用前自行裁剪 `tg` 到最小外接矩形（例如依据 alpha 通道 `Cv2.BoundingRect` 非透明区域）。

`感谢`
- [ddddocr-python(带带弟弟)](https://github.com/sml2h3/ddddocr)
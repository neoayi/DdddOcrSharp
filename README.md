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

`感谢`
- [ddddocr-python(带带弟弟)](https://github.com/sml2h3/ddddocr)
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using QRCoder;
using corel = Corel.Interop.VGCore;

namespace QRCodeGenerator
{
    /// <summary>
    /// 工具类
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// 读取文本文件的所有行
        /// </summary>
        /// <param name="path">文本文件的路径</param>
        /// <returns>包含文件中所有行的字符串列表</returns>
        public static List<string> ReadTxtLines(string path)
        {
            try
            {
                return new List<string>(File.ReadAllLines(path));
            }
            catch (IOException)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 只生成 SVG 字符串（无边框、只带 viewBox）
        /// </summary>
        public static string GenerateQrCode(string content)
        {
            var qrGen = new QRCoder.QRCodeGenerator();
            var qrData = qrGen.CreateQrCode(content, QRCoder.QRCodeGenerator.ECCLevel.Q);
            var svgMaker = new SvgQRCode(qrData);
            return svgMaker.GetGraphic(
                pixelsPerModule: 1, // 每模块 1px
                darkColorHex: "#000000", // 黑色前景
                lightColorHex: "#ffffff", // 白色背景
                drawQuietZones: false, // 不画静默区（边框）
                sizingMode: SvgQRCode.SizingMode.ViewBoxAttribute // 只输出 viewBox
            );
        }

        /// <summary>
        /// 生成二维码并插入到当前文档，设置大小为 385.68×385.68，并放置到 (x,y)。
        /// </summary>
        public static corel.Shape GenerateAndPlaceQrCode(
            corel.Document doc,
            corel.Layer layer,
            string content,
            double cx,
            double y,
            double qrSize
        )
        {
            double qsize = qrSize * 0.653;
            double qx = cx - qsize / 2;
            double qy = y + qrSize - qrSize * 0.129;

            // 1. 生成 SVG
            string svg = GenerateQrCode(content);

            // 2. 写入临时文件
            string tempSvg = Path.Combine(Path.GetTempPath(), $"temp_qr_{Guid.NewGuid()}.svg");
            File.WriteAllText(tempSvg, svg, Encoding.UTF8);

            // 3. 导入到当前文档
            layer.Import(tempSvg);

            // 4. 拿到刚导入的 Shape，调整大小和位置
            var sel = doc.Selection();
            corel.Shape qrShape = null;
            if (sel.Shapes.Count > 0)
            {
                qrShape = sel.Shapes[1];
                qrShape.SetSize(qsize, qsize);
                qrShape.SetPosition(qx, qy);
            }

            // 5. 删除临时文件
            File.Delete(tempSvg);
            return qrShape;
        }


        /// <summary>
        /// 绘制一个带有圆角的边框矩形
        /// </summary>
        /// <param name="layer">目标图层，用于绘制矩形</param>
        /// <param name="x">矩形左上角的x坐标</param>
        /// <param name="y">矩形左上角的y坐标</param>
        /// <param name="size">矩形的边长</param>
        /// <returns>一个表示边框矩形的Shape对象</returns>
        public static corel.Shape DrawBorderRectangle(
            corel.Layer layer,
            int x,
            int y,
            int size
        )
        {
            double radius = size * 0.0384;
            // 绘制一个方框
            corel.Shape rect = layer.CreateRectangle2(
                x,
                y,
                size,
                size,
                radius,
                radius,
                radius,
                radius
            );
            // 背景色透明
            rect.Fill.ApplyNoFill();

            // 设置描边颜色为黑色
            rect.Outline.Color.RGBAssign(0, 0, 0);

            return rect;
        }


        /// <summary>
        /// 在矩形四角绘制圆形
        /// </summary>
        /// <param name="layer">目标图层，用于绘制圆形</param>
        /// <param name="rect">边框矩形</param>
        /// <param name="x">矩形左上角的x坐标</param>
        /// <param name="y">矩形左上角的y坐标</param>
        /// <param name="size">矩形的边长</param>
        /// <returns>包含所有四个角的圆形的列表</returns>
        private static List<corel.Shape> DrawCornerCircles(
            corel.Layer layer,
            corel.Shape rect,
            int x,
            int y,
            int size
        )
        {
            double pad = size * 0.0334;
            double d = size * 0.064;
            double r = d / 2;

            // 计算四个角的圆心位置
            var centers = new[]
            {
                new { X = x + pad + r, Y = y + pad + r }, // 左下
                new { X = x + pad + r, Y = y + size - pad - r }, // 左上
                new { X = x + size - pad - r, Y = y + pad + r }, // 右下
                new { X = x + size - pad - r, Y = y + size - pad - r }, // 右上
            };

            // 把所有要分组的 Shape 放到一个数组里
            var shapesToGroup = new List<corel.Shape>();
            shapesToGroup.Add(rect);

            foreach (var pt in centers)
            {
                // 在每个角创建直径为 37.8 的圆
                var circle = layer.CreateEllipse2(
                    pt.X,
                    pt.Y,
                    r,
                    r
                );
                // 填充透明
                circle.Fill.ApplyNoFill();
                circle.Outline.Color.RGBAssign(0, 0, 0);
                shapesToGroup.Add(circle);
            }

            return shapesToGroup;
        }


        /// <summary>
        /// 创建艺术字文本
        /// </summary>
        /// <param name="layer">目标图层</param>
        /// <param name="content">文本内容</param>
        /// <param name="centerX">中心X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="size">字体大小的基准尺寸</param>
        /// <returns>创建的艺术字形状</returns>
        private static corel.Shape CreateArtisticText(
            corel.Layer layer,
            string content,
            double centerX,
            double y,
            int size
        )
        {
            corel.Shape text = layer.CreateArtisticText(
                Left: 0,
                Bottom: y + size * 0.04,
                Text: content,
                LanguageID: corel.cdrTextLanguage.cdrLanguageNone,
                CharSet: corel.cdrTextCharSet.cdrCharSetMixed,
                Font: "思源黑体 CN [嵌入]",
                Size: size * 0.3f,
                Bold: corel.cdrTriState.cdrFalse,
                Italic: corel.cdrTriState.cdrFalse,
                Underline: corel.cdrFontLine.cdrNoFontLine,
                Alignment: corel.cdrAlignment.cdrCenterAlignment
            );
            text.LeftX = centerX - text.SizeWidth / 2;
            return text;
        }

        /// <summary>
        /// 在指定位置创建二维码
        /// </summary>
        /// <param name="size">二维码的尺寸</param>
        /// <param name="content">二维码包含的内容</param>
        /// <param name="x">二维码左上角的X坐标</param>
        /// <param name="y">二维码左上角的Y坐标</param>
        /// <param name="document">目标文档对象，用于创建二维码</param>
        /// <param name="layer">目标图层对象，用于绘制和分组二维码相关内容</param>
        public static void PlaceQrCode(
            int size,
            string content,
            int x,
            int y,
            corel.Document document,
            corel.Layer layer
        )
        {
            // 生成边框
            corel.Shape rect = DrawBorderRectangle(layer, x, y, size);

            // 生成四个圆圈
            List<corel.Shape> circles = DrawCornerCircles(layer, rect, x, y, size);

            // 绘制二维码
            var qrShape = GenerateAndPlaceQrCode(
                document,
                layer,
                content,
                rect.CenterX,
                y,
                size
            );

            // 创建文本标签
            var text = CreateArtisticText(layer, content, rect.CenterX, y, size);

            // 把所有要分组的 Shape 放到一个数组里
            var shapesToGroup = new List<corel.Shape>();
            shapesToGroup.Add(rect);
            shapesToGroup.AddRange(circles);
            shapesToGroup.Add(qrShape);
            shapesToGroup.Add(text);

            corel.Shape[] shapeArr = shapesToGroup.ToArray();
            Array comArray = shapeArr;

            var shapeRange = document.CreateShapeRangeFromArray(comArray);

            shapeRange.Group();
        }
    }
}
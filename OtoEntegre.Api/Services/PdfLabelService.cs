using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf.IO;
using ZXing;
using ZXing.Common;

namespace OtoEntegre.Api.Services
{
    public class PdfLabelService
    {
        public class PdfLabelPositions
        {
            public double SiparisNoX { get; set; } = 40;
            public double SiparisNoY { get; set; } = 60;
            public double AdSoyadX { get; set; } = 40;
            public double AdSoyadY { get; set; } = 80;
            public double AdresX { get; set; } = 40;
            public double AdresY { get; set; } = 55;
            public double AdresWidth { get; set; } = 150;
            public double AdresHeight { get; set; } = 100;
            public double KargoTakipNumarasiX { get; set; } = 40;
            public double KargoTakipNumarasiY { get; set; } = 400;
            public double KargoBarkodX { get; set; } = 40;
            public double KargoBarkodY { get; set; } = 120;
            public double UrunBaslikX { get; set; } = 40;
            public double UrunBaslikY { get; set; } = 400;
            public double UrunSatirX { get; set; } = 40;
            public double UrunSatirStartY { get; set; } = 300;
            public double UrunSatirHeight { get; set; } = 14;
            public int MaxUrunSatir { get; set; } = 10;
            public string FontFamily { get; set; } = "Arial";
            public double FontSize { get; set; } = 10;
            public string FontBoldFamily { get; set; } = "Arial";
            public double FontBoldSize { get; set; } = 11;
            public double BarcodeX { get; set; } = 300;
            public double BarcodeY { get; set; } = 300;
            public double BarcodeWidth { get; set; } = 275;
            public double BarcodeHeight { get; set; } = 100;
            public double BarcodeTextX { get; set; } = 350;
            public double BarcodeTextY { get; set; } = 370;
        }

        public async Task<string> GenerateFromTemplateAsync(
            string templatePath,
            string outputDirectory,
            string siparisNo,
            string adSoyad,
            string adres,
            string kargoBarkod,
            string kargoBarkodNumarasi,
            string renk,
            string beden,
            string kargoTakipNumarasi,
            string urunTrendyolKod,
            IEnumerable<(string Ad, int Adet, string Renk, string Beden, string Barkod, string StokKodu)> urunler,
            PdfLabelPositions? positions = null)
        {
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("PDF şablonu bulunamadı", templatePath);

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            var outputFile = Path.Combine(outputDirectory, $"{adSoyad}.pdf");
            var pos = positions ?? new PdfLabelPositions();

            using (var document = PdfReader.Open(templatePath, PdfDocumentOpenMode.Modify))
            {
                var page = document.Pages[0];
                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    var font = new XFont(pos.FontFamily, pos.FontSize, XFontStyle.Regular);
                    var bold = new XFont(pos.FontBoldFamily, pos.FontBoldSize, XFontStyle.Bold);
                    var tf = new XTextFormatter(gfx);

                    // Başlık Bilgileri
                    gfx.DrawString($"{siparisNo}", bold, XBrushes.Black, new XPoint(pos.SiparisNoX, pos.SiparisNoY));
                    gfx.DrawString($"{adSoyad}", font, XBrushes.Black, new XPoint(pos.AdSoyadX, pos.AdSoyadY));

                    // Adres
                    var adresRect = new XRect(pos.AdresX, pos.AdresY, pos.AdresWidth, pos.AdresHeight);
                    tf.DrawString(adres ?? "", font, XBrushes.Black, adresRect, XStringFormats.TopLeft);

                    // Barkod
                    if (!string.IsNullOrWhiteSpace(kargoTakipNumarasi))
                    {
                        string tempPng = Path.Combine(Path.GetTempPath(), $"barcode_{Guid.NewGuid():N}.png");
                        try
                        {
                            var ms = GenerateCode128BarcodePng(kargoTakipNumarasi, (int)pos.BarcodeWidth, (int)pos.BarcodeHeight);
                            File.WriteAllBytes(tempPng, ms.ToArray());
                            using (var ximg = XImage.FromFile(tempPng))
                            {
                                gfx.DrawImage(ximg, pos.BarcodeX, pos.BarcodeY, pos.BarcodeWidth, pos.BarcodeHeight);
                                gfx.DrawString(kargoTakipNumarasi, font, XBrushes.Black,
                                    new XRect(pos.BarcodeX, pos.BarcodeY + pos.BarcodeHeight + 5, pos.BarcodeWidth, 20),
                                    XStringFormats.Center);
                            }
                        }
                        finally
                        {
                            if (File.Exists(tempPng))
                                File.Delete(tempPng);
                        }
                    }

                    // Ürün Tablosu
                    double y = pos.UrunBaslikY + 50;
                    double marginLeft = 15;
                    double tableWidth = page.Width - 2;
                    double rowHeight = pos.UrunSatirHeight;
                    string[] headers = new[] { "Ürün Adı", "Adet", "Renk", "Beden", "Barkod", "Ürün Kodu" };
                    double[] colWidths = new double[] { 155, 50, 60, 60, 140, 100 };
                    double[] colX = new double[colWidths.Length];
                    colX[0] = marginLeft;
                    for (int i = 1; i < colWidths.Length; i++)
                        colX[i] = colX[i - 1] + colWidths[i - 1];

                    // Tablo Başlıkları
                    for (int i = 0; i < headers.Length; i++)
                    {
                        gfx.DrawRectangle(XPens.Black, colX[i], y, colWidths[i], rowHeight);
                        gfx.DrawString(headers[i], bold, XBrushes.Black, new XRect(colX[i], y, colWidths[i], rowHeight), XStringFormats.Center);
                    }
                    y += rowHeight;

                    foreach (var u in urunler)
                    {
                        string[] cells = new[] { u.Ad, u.Adet.ToString(), u.Renk, u.Beden, u.Barkod, u.StokKodu };
                        double cellPadding = 4;
                        double maxCellHeight = rowHeight;
                        var tfCell = new XTextFormatter(gfx);

                        // Ürün Adı sütunu için dinamik yükseklik
                        double textWidth = colWidths[0] - 4;
                        double textHeight = gfx.MeasureString(u.Ad, font).Height;
                        int lines = (int)Math.Ceiling(gfx.MeasureString(u.Ad, font).Width / textWidth);
                        double dynamicHeight = lines * textHeight + cellPadding;
                        if (dynamicHeight > maxCellHeight)
                            maxCellHeight = dynamicHeight;

                        for (int i = 0; i < cells.Length; i++)
                        {
                            gfx.DrawRectangle(XPens.Gray, colX[i], y, colWidths[i], maxCellHeight);
                            var rect = new XRect(colX[i] + 2, y + 2, colWidths[i] - 4, maxCellHeight - 4);
                            if (i == 0)
                                tfCell.DrawString(cells[i], font, XBrushes.Black, rect, XStringFormats.TopLeft);
                            else
                                gfx.DrawString(cells[i], font, XBrushes.Black, rect, XStringFormats.Center);
                        }
                        y += maxCellHeight;
                        if (y > page.Height - 40)
                            break;
                    }
                }
                document.Save(outputFile);
            }
            await Task.CompletedTask;
            return outputFile;
        }

        [SupportedOSPlatform("windows")]
        public static MemoryStream GenerateCode128BarcodePng(string text, int width, int height)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 10
                }
            };
            var pixelData = writer.Write(text);
            using var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb);
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppRgb);
            System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
            bitmap.UnlockBits(bitmapData);
            var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            return ms;
        }
    }
}
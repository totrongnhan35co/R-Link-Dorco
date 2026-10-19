using BarcodeVerificationSystem.Services.THTrueMilk.RLinkMaster.Models;
using CommonVariable;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BarcodeVerificationSystem.View.THTrueMilkUI.Manufacturing
{
    public static class ProductImageHelper
    {
        private static readonly System.Net.Http.HttpClient _imgHttpClient = new System.Net.Http.HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static readonly ImageCodecInfo _jpegCodec = ImageCodecInfo.GetImageEncoders()
            .First(c => c.FormatID == ImageFormat.Jpeg.Guid);

        private static readonly EncoderParameters _jpegQuality = new EncoderParameters(1);
        private const int MaxImageSize = 400;

        public static readonly ConcurrentDictionary<string, Image> ImageCache
            = new ConcurrentDictionary<string, Image>();

        // Lưu original API URL TRƯỚC khi mutate product.Image
        private static readonly ConcurrentDictionary<string, string> _originalImageUrls
            = new ConcurrentDictionary<string, string>();

        // Lưu URL đã download thành công — để so sánh khi LÀM MỚI
        private static readonly ConcurrentDictionary<string, string> _downloadedImageUrls
            = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Download ảnh sản phẩm từ API.
        /// - So sánh URL mới vs URL đã download: giống thì skip, khác thì download mới + xóa cũ.
        /// - Chỉ xóa file cũ SAU khi download thành công.
        /// </summary>
        /// <returns>Số ảnh đã download thành công</returns>
        public static async Task<int> DownloadProductImagesAsync(
            List<ProductItem> products,
            bool forceRefresh = false)
        {
            string imageDir = CommVariables.PathProductImages;
            if (string.IsNullOrWhiteSpace(imageDir)) return 0;

            try
            {
                if (!Directory.Exists(imageDir))
                    Directory.CreateDirectory(imageDir);
            }
            catch { return 0; }

            string baseUrl = BarcodeVerificationSystem.Controller.Shared.Settings.ApiUrl?.TrimEnd('/') ?? "";
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                Debug.WriteLine("[ProductImage] SKIP: ApiUrl is empty");
                return 0;
            }

            var semaphore = new SemaphoreSlim(8);
            int downloaded = 0;

            var tasks = products.Select(async product =>
            {
                if (string.IsNullOrWhiteSpace(product.ProductId)) return;

                string localPath = Path.Combine(imageDir, $"{product.ProductId}.jpg");
                string tempPath = Path.Combine(imageDir, $"{product.ProductId}_new.jpg");

                // Lấy original API URL
                string imageUrl = product.Image;
                bool isLocalPath = !string.IsNullOrEmpty(imageUrl) && (imageUrl.Contains(@":\") || imageUrl.StartsWith("\\\\"));
                if (isLocalPath && _originalImageUrls.TryGetValue(product.ProductId, out var origUrl))
                    imageUrl = origUrl;

                // Validate URL
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    Debug.WriteLine($"[ProductImage] SKIP {product.ProductId}: imageUrl is empty");
                    return;
                }
                if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"[ProductImage] SKIP {product.ProductId}: imageUrl is HTTP URL");
                    return;
                }
                if (imageUrl.Contains(@":\") || imageUrl.StartsWith("\\\\"))
                {
                    Debug.WriteLine($"[ProductImage] SKIP {product.ProductId}: imageUrl is local path");
                    return;
                }

                // SO SÁNH URL từ metadata (persistent qua restart)
                string savedUrl = LoadSavedUrl(product.ProductId);

                // URL không đổi + đã có trong RAM cache → skip hoàn toàn (kể cả forceRefresh)
                if (savedUrl == imageUrl && ImageCache.ContainsKey(product.ProductId))
                {
                    TryCacheImage(product.ProductId, localPath);
                    Debug.WriteLine($"[ProductImage] SKIP {product.ProductId}: URL unchanged + in RAM cache");
                    return;
                }

                if (!forceRefresh && savedUrl == imageUrl && File.Exists(localPath))
                {
                    product.Image = localPath;
                    TryCacheImage(product.ProductId, localPath);
                    Debug.WriteLine($"[ProductImage] SKIP {product.ProductId}: URL unchanged (meta)");
                    return;
                }

                // Fallback: file < 24h và chưa có metadata → skip và ghi metadata
                if (!forceRefresh && File.Exists(localPath) && string.IsNullOrEmpty(savedUrl))
                {
                    var lastWrite = File.GetLastWriteTime(localPath);
                    if ((DateTime.Now - lastWrite).TotalHours < 24)
                    {
                        product.Image = localPath;
                        TryCacheImage(product.ProductId, localPath);
                        SaveImageMeta(product.ProductId, imageUrl);
                        Debug.WriteLine($"[ProductImage] SKIP {product.ProductId}: file < 24h");
                        return;
                    }
                }

                await semaphore.WaitAsync();
                try
                {
                    string fullUrl = baseUrl + "/" + imageUrl.TrimStart('/');
                    Debug.WriteLine($"[ProductImage] Downloading {product.ProductId}: {fullUrl}");

                    var bytes = await _imgHttpClient.GetByteArrayAsync(fullUrl);
                    if (bytes == null || bytes.Length == 0)
                    {
                        Debug.WriteLine($"[ProductImage] SKIP {product.ProductId}: empty bytes");
                        return;
                    }

                    Debug.WriteLine($"[ProductImage] Got {bytes.Length} bytes for {product.ProductId}");

                    // Resize ảnh về max 400px
                    SaveResizedImage(bytes, tempPath);

                    // Download thành công → XÓA FILE CŨ, rename file mới
                    if (File.Exists(localPath))
                        File.Delete(localPath);
                    File.Move(tempPath, localPath);

                    // Lưu metadata URL đã download (persistent qua restart)
                    SaveImageMeta(product.ProductId, imageUrl);

                    product.Image = localPath;

                    // Preload vào RAM NGAY
                    TryCacheImage(product.ProductId, localPath);

                    Interlocked.Increment(ref downloaded);
                    Debug.WriteLine($"[ProductImage] OK {product.ProductId} → {localPath}");
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    Debug.WriteLine($"[ProductImage] OFFLINE {product.ProductId}: using cache");
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                    if (File.Exists(localPath))
                        TryCacheImage(product.ProductId, localPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ProductImage] FAIL {product.ProductId}: {ex.Message}");
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                    if (File.Exists(localPath))
                        TryCacheImage(product.ProductId, localPath);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            Debug.WriteLine($"[ProductImage] DONE: {downloaded}/{products.Count} images downloaded");
            return downloaded;
        }

        /// <summary>
        /// Load ảnh từ RAM cache hoặc disk. Offline support — KHÔNG download HTTP.
        /// </summary>
        public static Image GetProductImage(string imagePath, string productId = "")
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return null;

            // Nhận diện URL — bỏ qua File.Exists(imagePath) vô nghĩa
            bool isUrl = imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                      || (!imagePath.Contains(@":\") && !imagePath.StartsWith("\\") && imagePath.Contains("/"));

            string cacheKey = productId ?? imagePath;

            // Tier 0: RAM cache — instant
            if (ImageCache.TryGetValue(cacheKey, out var cached) && cached != null)
                return cached;

            // Tier 1: File tại đường dẫn truyền vào (chỉ nếu là local path hợp lệ)
            if (!isUrl && File.Exists(imagePath))
            {
                var img = new Bitmap(imagePath);
                ImageCache[cacheKey] = img;
                return img;
            }

            // Tier 2: File trong thư mục cache theo productId
            if (!string.IsNullOrEmpty(productId))
            {
                string cachePath = Path.Combine(CommVariables.PathProductImages, $"{productId}.jpg");
                if (File.Exists(cachePath))
                {
                    var img = new Bitmap(cachePath);
                    ImageCache[cacheKey] = img;
                    return img;
                }
            }

            return null;
        }

        /// <summary>
        /// Preload TẤT CẢ ảnh từ disk vào RAM.
        /// </summary>
        public static void PreloadAllImages()
        {
            string dir = CommVariables.PathProductImages;
            if (!Directory.Exists(dir)) return;

            foreach (var f in Directory.GetFiles(dir, "*.jpg"))
            {
                string productId = Path.GetFileNameWithoutExtension(f);
                if (!string.IsNullOrEmpty(productId))
                    TryCacheImage(productId, f);
            }
        }

        /// <summary>
        /// Preload ảnh từ disk vào RAM cache + lưu original API URL.
        /// </summary>
        public static void PreloadImagesToCache(List<ProductItem> products)
        {
            string cacheDir = CommVariables.PathProductImages;
            if (string.IsNullOrWhiteSpace(cacheDir)) return;

            foreach (var p in products.Where(x => !string.IsNullOrEmpty(x.ProductId)))
            {
                // Lưu original API URL TRƯỚC khi mutate product.Image
                if (!string.IsNullOrEmpty(p.Image) && !p.Image.Contains(@":\") && !p.Image.StartsWith("\\\\"))
                    _originalImageUrls[p.ProductId] = p.Image;

                // Preload từ disk cache vào RAM
                string cachePath = Path.Combine(cacheDir, $"{p.ProductId}.jpg");
                if (File.Exists(cachePath))
                {
                    p.Image = cachePath;
                    TryCacheImage(p.ProductId, cachePath);
                }
            }
        }

        public static bool TryGetOriginalUrl(string productId, out string url)
            => _originalImageUrls.TryGetValue(productId, out url);

        // ── Metadata persistence: lưu URL đã download thành công ──
        private static string GetMetaPath(string productId)
        {
            string dir = CommVariables.PathProductImages;
            if (string.IsNullOrWhiteSpace(dir)) return null;
            return Path.Combine(dir, $"{productId}.json");
        }

        private static string LoadSavedUrl(string productId)
        {
            try
            {
                string metaPath = GetMetaPath(productId);
                if (metaPath == null || !File.Exists(metaPath)) return null;
                var json = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(metaPath));
                return json["originalUrl"]?.ToString();
            }
            catch { return null; }
        }

        private static void SaveImageMeta(string productId, string originalUrl)
        {
            try
            {
                string metaPath = GetMetaPath(productId);
                if (metaPath == null) return;
                var meta = new { originalUrl = originalUrl, downloadedAt = DateTime.Now };
                File.WriteAllText(metaPath, Newtonsoft.Json.JsonConvert.SerializeObject(meta));
            }
            catch { }
        }

        private static void TryCacheImage(string productId, string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;

                var img = new Bitmap(filePath);
                // Không dispose bitmap cũ — có thể UI control đang tham chiếu
                ImageCache[productId] = img;
            }
            catch { }
        }

        private static void SaveResizedImage(byte[] sourceBytes, string outputPath)
        {
            try
            {
                using (var srcMs = new MemoryStream(sourceBytes))
                using (var src = new Bitmap(srcMs))
                {
                    if (src.Width <= MaxImageSize && src.Height <= MaxImageSize)
                    {
                        File.WriteAllBytes(outputPath, sourceBytes);
                        return;
                    }

                    double ratio = Math.Min((double)MaxImageSize / src.Width, (double)MaxImageSize / src.Height);
                    int newW = (int)(src.Width * ratio);
                    int newH = (int)(src.Height * ratio);

                    _jpegQuality.Param[0] = new EncoderParameter(Encoder.Quality, 80L);

                    using (var resized = new Bitmap(newW, newH))
                    using (var g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(src, 0, 0, newW, newH);
                        resized.Save(outputPath, _jpegCodec, _jpegQuality);
                    }
                }
            }
            catch
            {
                try { File.WriteAllBytes(outputPath, sourceBytes); } catch { }
            }
        }
    }
}

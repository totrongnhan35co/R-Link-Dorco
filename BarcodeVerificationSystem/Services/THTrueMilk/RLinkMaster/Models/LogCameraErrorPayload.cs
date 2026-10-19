using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BarcodeVerificationSystem.Services.THTrueMilk.RLinkMaster.Models
{
    public class ErrorFrameItem
    {
        public string name { get; set; } = "";
        public string frame { get; set; } = "";
    }

    public class ImagePathItem
    {
        public string name { get; set; } = "";
        public string path { get; set; } = "";
    }

    /// <summary>
    /// Payload cho 1 dòng log lỗi camera (mỗi sản phẩm bị lỗi -> 1 record).
    /// </summary>
    public class LogCameraErrorPayload
    {
        public string line_id { get; set; } = "";
        public string rlink_name { get; set; } = "";
        public string job_name { get; set; } = "";
        public string batch { get; set; } = "";
        public string product_id { get; set; } = "";
        public string product_name { get; set; } = "";
        public string rlink_status { get; set; } = "";
        public string operator_user { get; set; } = "";

        /// <summary>Mã QR camera đọc được (rỗng nếu NotRead).</summary>
        public string qr_code { get; set; } = "";
        /// <summary>NSX từ camera (dd MM yy).</summary>
        public string error_manufactured_date { get; set; } = "";
        /// <summary>HSD từ camera (dd MM yy).</summary>
        public string error_expiry_date { get; set; } = "";
        /// <summary>NotRead | NotMatch | Duplicated | Invalid | Format | Other</summary>
        public string error_type { get; set; } = "";
        /// <summary>Đường dẫn ảnh lỗi (mảng object name/path).</summary>
        public List<ImagePathItem> image_path { get; set; } = new List<ImagePathItem>();
        /// <summary>Chuỗi dữ liệu Camera raw (mảng object name/frame).</summary>
        public List<ErrorFrameItem> error_frame_info { get; set; } = new List<ErrorFrameItem>();

        [JsonIgnore]
        public string image_path_raw { get; set; } = "";
        [JsonIgnore]
        public string error_frame_info_raw { get; set; } = "";

        public DateTime timestamp { get; set; } = DateTime.UtcNow;

        [JsonProperty("check_qr_code")]
        public bool CheckQrCode { get; set; } = true;
    }
}

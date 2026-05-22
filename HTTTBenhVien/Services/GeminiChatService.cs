using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using HTTTBenhVien.Models;

namespace HTTTBenhVien.Services
{
    public interface IGeminiChatService
    {
        Task<string> ChatWithAiAsync(string userMessage, List<ChatMessageDto> chatHistory);
    }

    public class ChatMessageDto
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } // "user" or "model"

        [JsonPropertyName("parts")]
        public List<ChatPartDto> Parts { get; set; } = new();
    }

    public class ChatPartDto
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class GeminiChatService : IGeminiChatService
    {
        private readonly HtttbenhvienContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly double _temperature;

        public GeminiChatService(HtttbenhvienContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            
            var settings = configuration.GetSection("GeminiSettings");
            _apiKey = settings["ApiKey"] ?? "";
            _modelName = settings["ModelName"] ?? "gemini-1.5-flash";
            
            if (double.TryParse(settings["Temperature"], out double temp))
            {
                _temperature = temp;
            }
            else
            {
                _temperature = 0.3;
            }
        }

        public async Task<string> ChatWithAiAsync(string userMessage, List<ChatMessageDto> chatHistory)
        {
            // 1. Kiểm tra cấu hình API Key
            bool isKeyConfigured = !string.IsNullOrWhiteSpace(_apiKey) && 
                                  !_apiKey.Contains("YOUR_GEMINI_API_KEY_HERE") && 
                                  _apiKey.Length > 10;

            if (!isKeyConfigured)
            {
                return await GetSmartOfflineResponseAsync(userMessage, "Hệ thống đang chạy ở chế độ giả lập offline do chưa cấu hình Gemini API Key thật.");
            }

            try
            {
                // 2. Thu thập ngữ cảnh động từ Database
                string hospitalContext = await BuildHospitalContextAsync();

                // 3. Chuẩn bị System Instruction
                string systemInstruction = "Bạn là trợ lý ảo chuyên trách giải đáp Câu hỏi thường gặp (FAQ) của Bệnh viện Đại học VinhUni (VinhUni HIS).\n" +
                                           "QUY TẮC PHẠM VI TRẢ LỜI BẮT BUỘC:\n" +
                                           "- Bạn CHỈ ĐƯỢC PHÉP trả lời các thông tin nằm trong dữ liệu bệnh viện được cung cấp dưới đây (khoa phòng, danh sách bác sĩ, lịch trực, hướng dẫn đặt lịch khám, quy trình cấp cứu).\n" +
                                           "- TUYỆT ĐỐI TỪ CHỐI trả lời bất kỳ câu hỏi nào khác ngoài phạm vi trên (bao gồm kiến thức y khoa chung, chẩn đoán bệnh từ xa, tư vấn sức khỏe tổng quát, kê đơn thuốc, dịch thuật, thơ ca hoặc các câu hỏi xã hội/chính trị khác).\n" +
                                           "- Nếu người dùng hỏi ngoài phạm vi bệnh viện, hãy trả lời lịch sự: 'Rất tiếc, tôi chỉ được thiết lập để hỗ trợ giải đáp các câu hỏi thường gặp về dịch vụ, lịch trực bác sĩ và hướng dẫn đặt lịch khám tại Bệnh viện Đại học VinhUni. Xin vui lòng đặt câu hỏi liên quan để tôi có thể hỗ trợ bạn tốt nhất!'\n\n" +
                                           "DƯỚI ĐÂY LÀ DỮ LIỆU THỰC TẾ HÔM NAY CỦA BỆNH VIỆN VINHUNI (Hãy dùng dữ liệu này để trả lời chính xác):\n" +
                                           hospitalContext + "\n\n" +
                                           "QUY TẮC CẢNH BÁO Y TẾ:\n" +
                                           "- Bạn không được tự ý kê đơn thuốc hoặc chẩn đoán bệnh chắc chắn.\n" +
                                           "- Luôn khuyên bệnh nhân đến gặp bác sĩ trực tiếp để có chẩn đoán chính xác.\n" +
                                           "- Với các trường hợp khẩn cấp, hướng dẫn họ chuyển ngay đến 'Khoa Cấp cứu' hoạt động 24/7 tại Tầng trệt.";

                // 4. Định hình Payload gửi sang Gemini API (Chuẩn API Gemini 1.5)
                var contents = new List<object>();

                // Thêm lịch sử trò chuyện
                if (chatHistory != null)
                {
                    foreach (var chat in chatHistory)
                    {
                        contents.Add(new
                        {
                            role = chat.Role == "model" ? "model" : "user",
                            parts = chat.Parts.Select(p => new { text = p.Text }).ToList()
                        });
                    }
                }

                // Thêm tin nhắn hiện tại
                contents.Add(new
                {
                    role = "user",
                    parts = new List<object> { new { text = userMessage } }
                });

                var payload = new
                {
                    contents = contents,
                    systemInstruction = new
                    {
                        parts = new List<object> { new { text = systemInstruction } }
                    },
                    generationConfig = new
                    {
                        temperature = _temperature,
                        maxOutputTokens = 4000
                    }
                };

                // 5. Gửi Request sang Gemini API với cơ chế tự động thử lại (Retry) khi bị giới hạn tần suất (429)
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";
                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                string jsonPayload = JsonSerializer.Serialize(payload, jsonOptions);
                
                HttpResponseMessage response = null;
                int maxRetries = 3;
                int delaySeconds = 2;

                for (int i = 0; i < maxRetries; i++)
                {
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    response = await _httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        Console.WriteLine($"Gemini API rate limited (429). Thử lại sau {delaySeconds} giây... (Lần {i + 1}/{maxRetries})");
                        await Task.Delay(delaySeconds * 1000);
                        delaySeconds *= 2; // Tăng gấp đôi thời gian chờ
                        continue;
                    }
                    
                    break;
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    string errContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Gemini API Error: {response.StatusCode} - {errContent}");
                    return await GetSmartOfflineResponseAsync(userMessage, $"Lỗi kết nối Gemini API ({response.StatusCode}). Tự động kích hoạt cơ chế xử lý ngoại lệ.");
                }

                string responseString = await response.Content.ReadAsStringAsync();
                
                // 6. Parse kết quả trả về từ Gemini
                using var doc = JsonDocument.Parse(responseString);
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();
                    
                    return text ?? "Không nhận được phản hồi từ trợ lý ảo.";
                }

                return "Trợ lý ảo hiện đang bận, vui lòng thử lại sau.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GeminiChatService: {ex.Message}");
                return await GetSmartOfflineResponseAsync(userMessage, $"Hệ thống gặp sự cố kết nối mạng ({ex.Message}). Tự động kích hoạt cơ chế ngoại lệ.");
            }
        }

        // Tạo ngữ cảnh động từ Database của Bệnh viện
        private async Task<string> BuildHospitalContextAsync()
        {
            try
            {
                var sb = new StringBuilder();

                // Lấy thông tin Khoa phòng
                var departments = await _context.Departments.AsNoTracking().ToListAsync();
                sb.AppendLine("1. DANH SÁCH CÁC KHOA LÂM SÀNG & CẬN LÂM SÀNG ĐANG HOẠT ĐỘNG:");
                if (departments.Any())
                {
                    foreach (var d in departments)
                    {
                        sb.AppendLine($"- {d.DeptName} (Mã: {d.DeptId}): {d.Description ?? "Chuyên khoa điều trị và chăm sóc y tế."}");
                    }
                }
                else
                {
                    sb.AppendLine("- Khoa Cấp cứu (Tầng trệt, hoạt động 24/7)\n- Khoa Nội\n- Khoa Ngoại\n- Khoa Nhi\n- Khoa Sản\n- Khoa Tai Mũi Họng\n- Khoa Răng Hàm Mặt");
                }
                sb.AppendLine();

                // Lấy danh sách Bác sĩ
                var doctors = await _context.Employees.AsNoTracking()
                    .Where(e => e.JobTitle != null && (e.JobTitle.Contains("Bác sĩ") || e.JobTitle.Contains("Doctor")))
                    .ToListAsync();
                sb.AppendLine("2. DANH SÁCH BÁC SĨ CHUYÊN KHOA ĐANG CÔNG TÁC TẠI VIỆN:");
                if (doctors.Any())
                {
                    foreach (var doc in doctors)
                    {
                        sb.AppendLine($"- Bác sĩ: {doc.FullName} | Chuyên khoa: {doc.Specialty ?? "Chung"} | Học vị: {doc.AcademicTitle ?? "Bác sĩ"} | SĐT liên hệ: {doc.Phone ?? "N/A"}");
                    }
                }
                else
                {
                    sb.AppendLine("- Bác sĩ Cấp cứu (Khoa Cấp cứu)\n- BS. Nguyễn Văn Minh (Khoa Nội)");
                }
                sb.AppendLine();

                // Lấy lịch trình hôm nay
                var schedules = await _context.DoctorSchedules
                    .Include(ds => ds.Doctor)
                    .Where(s => s.WorkDate >= DateTime.Today)
                    .AsNoTracking()
                    .ToListAsync();
                sb.AppendLine("3. LỊCH KHÁM & PHÂN CÔNG CỦA BÁC SĨ:");
                if (schedules.Any())
                {
                    foreach (var s in schedules)
                    {
                        if (s.Doctor != null)
                        {
                            string isAvailable = s.IsAvailable == true ? "Sẵn sàng nhận bệnh" : "Đã kín lịch";
                            sb.AppendLine($"- Bác sĩ: {s.Doctor.FullName} | Ngày khám: {s.WorkDate.ToString("dd/MM/yyyy")} | Ca trực: {s.ShiftType ?? "Cả ngày"} | Trạng thái: {isAvailable}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("- Lịch khám linh động từ Thứ 2 đến Thứ 7 hàng tuần (Sáng từ 07:00 - 11:30, Chiều từ 13:30 - 17:00). Chủ nhật trực cấp cứu.");
                }
                sb.AppendLine();

                // Hướng dẫn quy trình đặt lịch & cấp cứu
                sb.AppendLine("4. THÔNG TIN TIỆN ÍCH DỊCH VỤ:");
                sb.AppendLine("- Địa chỉ bệnh viện: Số 192 Lê Duẩn, Thành phố Vinh, Nghệ An (Bên trong khuôn viên Đại học Vinh).");
                sb.AppendLine("- Hotline đặt lịch: 1900-123-456");
                sb.AppendLine("- Hướng dẫn đặt lịch trực tuyến: Bệnh nhân truy cập menu 'Đặt lịch khám' trên thanh Topbar, chọn 'Đặt lịch cho chính mình' hoặc 'Đặt lịch cho người thân', điền đầy đủ thông tin hành chính, chọn chuyên khoa/bác sĩ và giờ hẹn, sau đó bấm xác nhận. Khi đến viện, hệ thống Auto-Checkin sẽ giúp bệnh nhân vào thẳng phòng khám không cần qua quầy Lễ tân xếp hàng đóng phí trước.");
                sb.AppendLine("- Quy trình Cấp cứu: Bệnh nhân cấp cứu được đưa thẳng vào Khoa Cấp cứu, được kíp trực triage phân loại (5 cấp độ) và thực hiện hồi sức, chỉ định xét nghiệm, cấp thuốc lập tức mà không cần thanh toán trước. Viện phí sẽ được tích lũy nợ cấp cứu và thanh toán sau gộp khi bệnh nhân đã ổn định.");
                sb.AppendLine("- Phí khám lâm sàng mặc định: 150.000 VND (chỉ thu 1 lần duy nhất trong suốt lượt khám).");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Không thể truy xuất dữ liệu bệnh viện do lỗi kết nối: {ex.Message}";
            }
        }

        // Chế độ Giả lập / Xử lý Ngoại lệ thông minh dựa trên Database thực tế
        private async Task<string> GetSmartOfflineResponseAsync(string userMessage, string notice)
        {
            string msg = userMessage.ToLower();
            var sb = new StringBuilder();
            sb.AppendLine("🤖 **[Trợ lý ảo VinhUni HIS - Chế độ Giả lập]**");
            sb.AppendLine($"*(Lưu ý: {notice} Dữ liệu dưới đây được truy vấn trực tiếp từ Cơ sở dữ liệu thực tế của hệ thống)*\n");

            if (msg.Contains("bác sĩ") || msg.Contains("bac si") || msg.Contains("ai khám") || msg.Contains("ai kham") || msg.Contains("lịch trực") || msg.Contains("lich truc"))
            {
                var doctors = await _context.Employees.AsNoTracking()
                    .Where(e => e.JobTitle != null && (e.JobTitle.Contains("Bác sĩ") || e.JobTitle.Contains("Doctor")))
                    .ToListAsync();
                sb.AppendLine("### 🩺 Danh sách Bác sĩ chuyên khoa tại viện:");
                foreach (var doc in doctors)
                {
                    sb.AppendLine($"- **BS. {doc.FullName}** | Chuyên khoa: {doc.Specialty ?? "Chung"} | Học vị: {doc.AcademicTitle ?? "Bác sĩ"}");
                }
                sb.AppendLine("\nBạn có thể vào mục **'Đặt lịch khám'** ở thanh điều hướng phía trên để chọn lịch hẹn trực tiếp với các bác sĩ!");
            }
            else if (msg.Contains("khoa") || msg.Contains("phòng") || msg.Contains("chuyên khoa"))
            {
                var departments = await _context.Departments.AsNoTracking().ToListAsync();
                sb.AppendLine("### 🏢 Các chuyên khoa đang hoạt động:");
                foreach (var d in departments)
                {
                    sb.AppendLine($"- **{d.DeptName}**: {d.Description ?? "Khoa chuyên môn điều trị."}");
                }
            }
            else if (msg.Contains("đặt lịch") || msg.Contains("dat lich") || msg.Contains("hẹn khám") || msg.Contains("hen kham") || msg.Contains("đăng ký") || msg.Contains("dang ky"))
            {
                sb.AppendLine("### 📅 Hướng dẫn Đăng ký khám trực tuyến tại VinhUni HIS:");
                sb.AppendLine("1. Nhấp vào nút **Đặt lịch khám** nổi bật trên thanh tiêu đề (Topbar).");
                sb.AppendLine("2. Chọn **Đặt lịch cho chính mình** hoặc **Đặt lịch cho người thân** (con cái, bố mẹ...).");
                sb.AppendLine("3. Điền Số CCCD, Mã BHYT và thông tin hành chính cần thiết.");
                sb.AppendLine("4. Chọn Chuyên khoa, Bác sĩ muốn khám và Khung giờ trống.");
                sb.AppendLine("5. Xác nhận đặt lịch thành công. Bạn sẽ nhận được thông báo mã vạch và chỉ đường trên hệ thống.");
                sb.AppendLine("⚡ *Đặc biệt: Hệ thống tự động kích hoạt chế độ **Auto Check-In**. Bạn chỉ cần đến thẳng phòng khám của bác sĩ theo đúng giờ hẹn mà không cần qua quầy lễ tân đóng tiền trước!*");
            }
            else if (msg.Contains("cấp cứu") || msg.Contains("cap cuu") || msg.Contains("nguy kịch") || msg.Contains("tai nạn"))
            {
                sb.AppendLine("### 🚨 Quy trình Cấp cứu khẩn cấp:");
                sb.AppendLine("- **Khoa Cấp cứu** của Bệnh viện Đại học VinhUni hoạt động **24/7** tại tầng trệt.");
                sb.AppendLine("- **Bypass viện phí:** Mọi chỉ định cận lâm sàng và cấp thuốc khẩn cấp cho ca cấp cứu đều được ưu tiên thực hiện lập tức mà không cần thanh toán trước.");
                sb.AppendLine("- **Triage phân loại:** Bệnh nhân được tiếp nhận và xếp vào 5 cấp độ ưu tiên y khoa.");
                sb.AppendLine("- **Thanh toán tích lũy:** Toàn bộ chi phí sẽ được cộng dồn vào hóa đơn nợ cấp cứu. Người nhà có thể đóng gộp tại Quầy Thu ngân sau khi bệnh nhân qua cơn nguy kịch.");
            }
            else
            {
                sb.AppendLine("Chào bạn! Tôi là Trợ lý ảo Y khoa thông minh VinhUni HIS.");
                sb.AppendLine("Tôi có thể giúp bạn giải đáp các thông tin về:");
                sb.AppendLine("- 🩺 Danh sách bác sĩ chuyên khoa đang trực tại viện.");
                sb.AppendLine("- 🏢 Các khoa điều trị lâm sàng & cận lâm sàng.");
                sb.AppendLine("- 📅 Hướng dẫn chi tiết quy trình đăng ký khám trực tuyến thông minh (Auto Check-In).");
                sb.AppendLine("- 🚨 Quy trình tiếp nhận cấp cứu khẩn cấp.");
                sb.AppendLine("\nBạn vui lòng nhập câu hỏi liên quan hoặc bấm chọn các gợi ý nhanh phía dưới khung chat để tôi hỗ trợ nhé!");
            }

            return sb.ToString();
        }
    }
}

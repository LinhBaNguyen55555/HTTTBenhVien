using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HTTTBenhVien.Services;

namespace HTTTBenhVien.Controllers
{
    public class AiChatController : Controller
    {
        private readonly IGeminiChatService _geminiChatService;

        public AiChatController(IGeminiChatService geminiChatService)
        {
            _geminiChatService = geminiChatService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequestModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { success = false, reply = "Nội dung tin nhắn không hợp lệ." });
            }

            try
            {

                string reply = await _geminiChatService.ChatWithAiAsync(request.Message, request.History);
                
                return Json(new { success = true, reply = reply });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, reply = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }
    }

    public class ChatRequestModel
    {
        public string Message { get; set; }
        public List<ChatMessageDto> History { get; set; } = new();
    }
}

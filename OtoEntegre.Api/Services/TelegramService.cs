using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.IO;
using System.Collections.Generic;
using System;
using System.Text;
using OtoEntegre.Api.Services;

public class TelegramService
{
    private readonly HttpClient _httpClient;
    private readonly UserService _userService;

    public TelegramService(HttpClient httpClient, UserService userService)
    {
        _httpClient = httpClient;
        _userService = userService;
    }

    // Foto + mesaj gönderme
    public async Task<bool> SendOrderMessageAsync(Guid? userId, string message, string? imageUrl = null)

    {
        Console.WriteLine("SendOrderMessageAsync başladaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        try
        {
            string? botToken = null;
            string? chatId = null;
            if (userId.HasValue)
            {
                var user = await _userService.GetByIdAsync(userId.Value);
                botToken = user?.Telegram_Token;
                chatId = user?.Telegram_Chat;
                Console.WriteLine($"SendOrderMessageAsync {botToken}");
                Console.WriteLine($"SendOrderMessageAsync {chatId}");

            }
            else
            {
                return false; // userId zorunlu
            }
            if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
                return false;

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                try
                {
                    var url = $"https://api.telegram.org/bot{botToken}/sendPhoto";
                    var data = new MultipartFormDataContent
                    {
                        { new StringContent(chatId), "chat_id" },
                        { new StringContent(message), "caption" }
                    };

                    var bytes = await _httpClient.GetByteArrayAsync(imageUrl);
                    var fileContent = new ByteArrayContent(bytes);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                    data.Add(fileContent, "photo", "urun.jpg");

                    var response = await _httpClient.PostAsync(url, data);
                    var respContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Telegram Response ({url}): {respContent}");
                    return response.IsSuccessStatusCode;

                }
                catch
                {
                    var fallbackResponse = await _httpClient.PostAsync(
                        $"https://api.telegram.org/bot{botToken}/sendMessage",
                        new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            ["chat_id"] = chatId,
                            ["text"] = message
                        })
                    );
                    return fallbackResponse.IsSuccessStatusCode;
                }
            }
            else
            {
                var response = await _httpClient.PostAsync(
                    $"https://api.telegram.org/bot{botToken}/sendMessage",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["chat_id"] = chatId,
                        ["text"] = message
                    })
                );
                return response.IsSuccessStatusCode;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("UserID111 " + userId);
            Console.WriteLine("Telegram gönderim hatası: " + ex.Message);
            return false;
        }
    }

    // Yeni: Foto veya mesaj gönder ve Telegram'dan dönen message_id/chat id bilgisini al
    public async Task<(bool success, int? messageId, string? chatId)> SendOrderMessageWithResultAsync(Guid? userId, string message, string? imageUrl = null)
    {
        try
        {
            string? botToken = null;
            string? chatId = null;
            if (userId.HasValue)
            {
                var user = await _userService.GetByIdAsync(userId.Value);
                botToken = user?.Telegram_Token;
                chatId = user?.Telegram_Chat;
            }
            else
            {
                return (false, null, null);
            }
            if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
                return (false, null, null);

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                try
                {
                    var url = $"https://api.telegram.org/bot{botToken}/sendPhoto";
                    var data = new MultipartFormDataContent
                    {
                        { new StringContent(chatId), "chat_id" },
                        { new StringContent(message), "caption" }
                    };

                    var bytes = await _httpClient.GetByteArrayAsync(imageUrl);
                    var fileContent = new ByteArrayContent(bytes);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                    data.Add(fileContent, "photo", "urun.jpg");

                    var response = await _httpClient.PostAsync(url, data);
                    var respContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Telegram Response ({url}): {respContent}");

                    if (!response.IsSuccessStatusCode)
                        return (false, null, null);

                    try
                    {
                        using var doc = JsonDocument.Parse(respContent);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("result", out var result))
                        {
                            int? msgId = null;
                            string? returnedChatId = null;
                            if (result.TryGetProperty("message_id", out var m)) msgId = m.GetInt32();
                            if (result.TryGetProperty("chat", out var chat))
                            {
                                if (chat.TryGetProperty("id", out var cid)) returnedChatId = cid.ToString();
                            }
                            return (true, msgId, returnedChatId ?? chatId);
                        }
                    }
                    catch { }

                    return (response.IsSuccessStatusCode, null, chatId);
                }
                catch
                {
                    // fallback to text message
                    var fallbackResponse = await _httpClient.PostAsync(
                        $"https://api.telegram.org/bot{botToken}/sendMessage",
                        new FormUrlEncodedContent(new Dictionary<string, string>
                        {
                            ["chat_id"] = chatId,
                            ["text"] = message
                        })
                    );

                    var respContent = await fallbackResponse.Content.ReadAsStringAsync();
                    try
                    {
                        using var doc = JsonDocument.Parse(respContent);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("result", out var result))
                        {
                            int? msgId = null;
                            string? returnedChatId = null;
                            if (result.TryGetProperty("message_id", out var m)) msgId = m.GetInt32();
                            if (result.TryGetProperty("chat", out var chat))
                            {
                                if (chat.TryGetProperty("id", out var cid)) returnedChatId = cid.ToString();
                            }
                            return (true, msgId, returnedChatId ?? chatId);
                        }
                    }
                    catch { }

                    return (fallbackResponse.IsSuccessStatusCode, null, chatId);
                }
            }
            else
            {
                var response = await _httpClient.PostAsync(
                    $"https://api.telegram.org/bot{botToken}/sendMessage",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["chat_id"] = chatId,
                        ["text"] = message
                    })
                );

                var respContent = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(respContent);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("result", out var result))
                    {
                        int? msgId = null;
                        string? returnedChatId = null;
                        if (result.TryGetProperty("message_id", out var m)) msgId = m.GetInt32();
                        if (result.TryGetProperty("chat", out var chat))
                        {
                            if (chat.TryGetProperty("id", out var cid)) returnedChatId = cid.ToString();
                        }
                        return (true, msgId, returnedChatId ?? chatId);
                    }
                }
                catch { }

                return (response.IsSuccessStatusCode, null, chatId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Telegram gönderim hatası (with result): " + ex.Message);
            return (false, null, null);
        }
    }

    // Yeni: var olan mesaja reply olarak metin gönder
    public async Task<bool> SendReplyMessageAsync(Guid? userId, string text, int replyToMessageId)
    {
        try
        {
            string? botToken = null;
            string? chatId = null;
            if (userId.HasValue)
            {
                var user = await _userService.GetByIdAsync(userId.Value);
                botToken = user?.Telegram_Token;
                chatId = user?.Telegram_Chat;
            }
            else
            {
                return false; // userId zorunlu
            }
            if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
                return false;

            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var data = new Dictionary<string, string>
            {
                ["chat_id"] = chatId,
                ["text"] = text,
                ["reply_to_message_id"] = replyToMessageId.ToString()
            };

            var response = await _httpClient.PostAsync(url, new FormUrlEncodedContent(data));
            var respContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Telegram Reply Response ({url}): {respContent}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Telegram reply gönderim hatası: " + ex.Message);
            return false;
        }
    }

    // PDF/Belge gönderme
  public async Task<bool> SendDocumentAsync(string? caption, byte[] fileBytes, Guid? userId, string fileName = "document.pdf")
{
    Console.WriteLine($"send document başladı {caption}");
    try
    {
        if (fileBytes == null || fileBytes.Length == 0)
        {
            Console.WriteLine("Byte array boş!");
            return false;
        }

        string? botToken = null;
        string? chatId = null;

        if (userId.HasValue)
        {
            var user = await _userService.GetByIdAsync(userId.Value);
            botToken = user?.Telegram_Token;
            chatId = user?.Telegram_Chat;
        }
        else return false;

        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
            return false;

        var url = $"https://api.telegram.org/bot{botToken}/sendDocument";

        using var multipart = new MultipartFormDataContent();

        multipart.Add(new StringContent(chatId), "chat_id");

        if (!string.IsNullOrWhiteSpace(caption))
            multipart.Add(new StringContent(caption), "caption");

        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        // ❗❗ EN ÖNEMLİ KISIM
        var safeFileName = ToSafeFileName(fileName);
if (!safeFileName.EndsWith(".pdf"))
    safeFileName += ".pdf";

        multipart.Add(fileContent, "document", safeFileName);

        var response = await _httpClient.PostAsync(url, multipart);

        var resp = await response.Content.ReadAsStringAsync();
        Console.WriteLine("PDF Response: " + resp);

        return response.IsSuccessStatusCode;
    }
    catch (Exception ex)
    {
        Console.WriteLine("Telegram belge gönderim hatası: " + ex.Message);
        return false;
    }
}
public static string ToSafeFileName(string name)
{
    // Türkçe karakterleri ASCII'ye çevir
    string normalized = name
        .Replace("İ", "I").Replace("ı", "i")
        .Replace("Ş", "S").Replace("ş", "s")
        .Replace("Ğ", "G").Replace("ğ", "g")
        .Replace("Ü", "U").Replace("ü", "u")
        .Replace("Ö", "O").Replace("ö", "o")
        .Replace("Ç", "C").Replace("ç", "c");

    // Geçersiz karakterleri kaldır
    foreach (char c in Path.GetInvalidFileNameChars())
        normalized = normalized.Replace(c.ToString(), "");

    // Boşlukları _ yap
    normalized = normalized.Replace(" ", "_");

    return normalized;
}

    // Türkçe karakterleri dönüştüren helper metot
    private static string ReplaceTurkishCharacters(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return input
            .Replace('ç', 'c').Replace('Ç', 'C')
            .Replace('ğ', 'g').Replace('Ğ', 'G')
            .Replace('ı', 'i').Replace('İ', 'I')
            .Replace('ö', 'o').Replace('Ö', 'O')
            .Replace('ş', 's').Replace('Ş', 'S')
            .Replace('ü', 'u').Replace('Ü', 'U');
    }


}

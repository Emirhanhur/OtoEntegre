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

    // PDF/Belge gönderme
    public async Task<bool> SendDocumentAsync(string? caption, string filePath, Guid? userId)
    {
        Console.WriteLine($"send document başladı {caption}");
        try
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Console.WriteLine("Belge yolu bulunamadı veya dosya yok: " + filePath);
                return false;
            }
            string? botToken = null;
            string? chatId = null;
            if (userId.HasValue)
            {
                var user = await _userService.GetByIdAsync(userId.Value);
                Console.WriteLine(botToken);
                botToken = user?.Telegram_Token;
                chatId = user?.Telegram_Chat;
                Console.WriteLine($"user = = = {user}");
                Console.WriteLine($"botToken = = = {botToken}");
                Console.WriteLine($"chatId =  = = {chatId}");

            }
            else
            {
                return false; // userId zorunlu
            }
            if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
                return false;

            var url = $"https://api.telegram.org/bot{botToken}/sendDocument";
            using var multipart = new MultipartFormDataContent();
            var bytes = await File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);

            string safeFileName = ReplaceTurkishCharacters(fileName);
            string safeCaption = ReplaceTurkishCharacters(caption ?? "");

            var docContent = new ByteArrayContent(bytes);
            docContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            docContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"document\"",
                FileName = $"\"{safeFileName}\""
            };

            multipart.Add(docContent);
            multipart.Add(new StringContent(chatId), "chat_id");

            if (!string.IsNullOrWhiteSpace(safeCaption))
            {
                multipart.Add(new StringContent(safeCaption), "caption");
            }

            var response = await _httpClient.PostAsync(url, multipart);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine("UserID2222 " + userId);

                Console.WriteLine("Telegram gönderim hatası: " + error);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Telegram belge gönderim hatası: " + ex.Message);
            return false;
        }
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

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DaerahRawanBanjir;

public class GeminiNlpService
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    // GANTI dengan API Key Gemini kamu
    private const string ApiKey = "AQ.Ab8RN6Li6Fx5KzofJgILjgVJpSbRA-94Vw81tPX0iorBRYI2DA";

    private const string ModelName = "gemini-2.5-flash";

    private static readonly string Endpoint =
        $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent";

    public async Task<string> AskAsync(
        string userQuestion,
        double? curahHujan = null,
        double? elevasi = null,
        string? status = null)
    {
        if (string.IsNullOrWhiteSpace(ApiKey) || ApiKey.Contains("ISI_API_KEY"))
        {
            return "API Key Gemini belum diisi. Silakan isi API Key terlebih dahulu pada file GeminiNlpService.cs.";
        }

        string prompt = $"""
        Kamu adalah asisten NLP untuk aplikasi:
        SISTEM ANALISIS DAERAH RISIKO RAWAN BANJIR.

        Tugas kamu:
        1. Menjawab pertanyaan pengguna tentang risiko banjir.
        2. Menjelaskan arti curah hujan, elevasi wilayah, status aman, waspada, dan bahaya ketika ditanya.
        3. Memberikan saran pencegahan banjir secara singkat dan mudah dipahami.
        4. Gunakan bahasa Indonesia yang jelas.
        5. Jawaban jangan terlalu panjang.

        Aturan format jawaban:
        Jangan gunakan format markdown.
        Jangan gunakan tanda **.
        Jangan gunakan tanda *.
        Jangan gunakan tanda #.
        Jangan gunakan bullet markdown.
        Jangan gunakan heading markdown.
        Jawab dengan teks biasa saja.

        Data sistem saat ini:
        - Curah hujan: {(curahHujan.HasValue ? $"{curahHujan:F2} mm" : "belum diisi")}
        - Elevasi wilayah: {(elevasi.HasValue ? $"{elevasi:F2} meter" : "belum diisi")}
        - Status analisis: {(string.IsNullOrWhiteSpace(status) ? "belum dianalisis" : status)}

        Pertanyaan pengguna:
        {userQuestion}
        """;

        var requestBody = new GeminiRequest
        {
            Contents =
            [
                new GeminiContent
                {
                    Parts =
                    [
                        new GeminiPart
                        {
                            Text = prompt
                        }
                    ]
                }
            ]
        };

        string json = JsonSerializer.Serialize(
            requestBody,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Add("x-goog-api-key", ApiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Gemini API error: {(int)response.StatusCode}\n{responseJson}";
            }

            var result = JsonSerializer.Deserialize<GeminiResponse>(
                responseJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            string? answer = result?
                .Candidates?
                .FirstOrDefault()?
                .Content?
                .Parts?
                .FirstOrDefault()?
                .Text;

            return string.IsNullOrWhiteSpace(answer)
                ? "Gemini tidak mengembalikan jawaban."
                : CleanGeminiText(answer);
        }
        catch (TaskCanceledException)
        {
            return "Request ke Gemini terlalu lama. Periksa koneksi internet atau coba lagi.";
        }
        catch (Exception ex)
        {
            return $"Terjadi kesalahan saat menghubungi Gemini: {ex.Message}";
        }
    }

    private static string CleanGeminiText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string cleaned = text;

        // Menghapus format bold dan italic markdown
        cleaned = cleaned.Replace("**", "");
        cleaned = cleaned.Replace("__", "");

        // Menghapus heading markdown
        cleaned = cleaned.Replace("###", "");
        cleaned = cleaned.Replace("##", "");
        cleaned = cleaned.Replace("#", "");

        // Menghapus bullet markdown di awal baris
        cleaned = Regex.Replace(cleaned, @"^\s*[\*\-]\s+", "", RegexOptions.Multiline);

        // Menghapus sisa tanda bintang yang berdiri sendiri
        cleaned = cleaned.Replace("*", "");

        // Merapikan spasi dan enter berlebih
        cleaned = Regex.Replace(cleaned, @"[ \t]+", " ");
        cleaned = Regex.Replace(cleaned, @"\n{3,}", "\n\n");

        return cleaned.Trim();
    }

    private class GeminiRequest
    {
        public GeminiContent[] Contents { get; set; } = [];
    }

    private class GeminiContent
    {
        public GeminiPart[] Parts { get; set; } = [];
    }

    private class GeminiPart
    {
        public string Text { get; set; } = string.Empty;
    }

    private class GeminiResponse
    {
        public GeminiCandidate[]? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContentResponse? Content { get; set; }
    }

    private class GeminiContentResponse
    {
        public GeminiPartResponse[]? Parts { get; set; }
    }

    private class GeminiPartResponse
    {
        public string? Text { get; set; }
    }
}
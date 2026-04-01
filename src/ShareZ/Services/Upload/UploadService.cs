using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using ShareZ.Models;

namespace ShareZ.Services.Upload;

public class UploadResult
{
    public string Url { get; set; } = string.Empty;
    public string? DeletionUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UploadService
{
    private readonly AppSettings _settings;
    private readonly HttpClient _httpClient;

    public event EventHandler<int>? UploadProgress;
    public event EventHandler<UploadResult>? UploadCompleted;

    public UploadService(AppSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ShareZ/1.0");
    }

    public async Task<UploadResult?> UploadAsync(string filePath)
    {
        var destination = _settings.UploadDestinations.FirstOrDefault(d => d.IsDefault)
            ?? _settings.UploadDestinations.FirstOrDefault();

        if (destination == null)
        {
            return new UploadResult { IsSuccess = false, ErrorMessage = "No upload destination configured" };
        }

        return destination.Type switch
        {
            "Imgur" => await UploadToImgurAsync(filePath, destination),
            "Custom" => await UploadToCustomAsync(filePath, destination),
            _ => new UploadResult { IsSuccess = false, ErrorMessage = $"Unknown upload type: {destination.Type}" }
        };
    }

    public async Task<UploadResult> UploadToImgurAsync(string filePath, UploadDestination destination)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var imageBytes = await File.ReadAllBytesAsync(filePath);
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(
                Helpers.ImageFormatHelper.GetMimeType(App.Settings.DefaultImageFormat));
            content.Add(imageContent, "image", Path.GetFileName(filePath));

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Client-ID", destination.ApiKey);

            var response = await _httpClient.PostAsync(destination.ApiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var json = JsonDocument.Parse(responseBody);
                var data = json.RootElement.GetProperty("data");

                var result = new UploadResult
                {
                    IsSuccess = true,
                    Url = data.GetProperty("link").GetString() ?? string.Empty,
                    DeletionUrl = data.TryGetProperty("deletehash", out var deleteHash)
                        ? $"https://imgur.com/delete/{deleteHash.GetString()}"
                        : null
                };

                UploadCompleted?.Invoke(this, result);
                return result;
            }

            return new UploadResult
            {
                IsSuccess = false,
                ErrorMessage = $"Upload failed: {response.StatusCode} - {responseBody}"
            };
        }
        catch (Exception ex)
        {
            return new UploadResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<UploadResult> UploadToCustomAsync(string filePath, UploadDestination destination)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var imageBytes = await File.ReadAllBytesAsync(filePath);
            var imageContent = new ByteArrayContent(imageBytes);
            content.Add(imageContent, "file", Path.GetFileName(filePath));

            var request = new HttpRequestMessage(HttpMethod.Post, destination.ApiUrl)
            {
                Content = content
            };

            foreach (var header in destination.Headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                string url = responseBody;
                string? deletionUrl = null;

                // Try to parse response patterns
                if (!string.IsNullOrEmpty(destination.ResponseUrlPattern))
                {
                    try
                    {
                        var json = JsonDocument.Parse(responseBody);
                        url = ExtractFromJson(json.RootElement, destination.ResponseUrlPattern) ?? responseBody;

                        if (!string.IsNullOrEmpty(destination.ResponseDeleteUrlPattern))
                        {
                            deletionUrl = ExtractFromJson(json.RootElement, destination.ResponseDeleteUrlPattern);
                        }
                    }
                    catch
                    {
                        url = responseBody.Trim();
                    }
                }

                var result = new UploadResult
                {
                    IsSuccess = true,
                    Url = url,
                    DeletionUrl = deletionUrl
                };

                UploadCompleted?.Invoke(this, result);
                return result;
            }

            return new UploadResult
            {
                IsSuccess = false,
                ErrorMessage = $"Upload failed: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new UploadResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    private static string? ExtractFromJson(JsonElement element, string path)
    {
        var parts = path.Split('.');
        var current = element;

        foreach (var part in parts)
        {
            if (current.TryGetProperty(part, out var next))
            {
                current = next;
            }
            else
            {
                return null;
            }
        }

        return current.GetString();
    }
}

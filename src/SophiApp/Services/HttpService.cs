// <copyright file="HttpService.cs" company="Team Sophia">
// Copyright (c) Team Sophia. All rights reserved.
// </copyright>

namespace SophiApp.Services
{
    using SophiApp.Contracts.Services;
    using SophiApp.Extensions;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <inheritdoc/>
    public class HttpService : IHttpService
    {
        private readonly Regex hrefPattern = new (@"(?inx)
<a \s [^>]*
    href \s* = \s*
        (?<q> ['""] )
            (?<url> [^""]+ )
        \k<q>
[^>]* >");

        /// <inheritdoc/>
        public void DownloadFile(string url, string saveTo)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(saveTo) !);
            using var client = new HttpClient();
            using var urlStream = client.GetStreamAsync(url).Result;
            using var fileStream = new FileStream(saveTo, FileMode.Create);
            urlStream.CopyTo(fileStream);
        }

        /// <inheritdoc/>
        public void DownloadOneDrive(string saveTo)
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://g.live.com/1rewlive5skydrive/OneDriveProductionV2");
            using var response = client.SendAsync(request).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            var xml = new XmlDocument();
            xml.LoadXml(result);
            var url = xml?.DocumentElement?.SelectSingleNode("update/amd64binary")?.Attributes?.GetNamedItem("url")?.InnerText ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(url))
            {
                using var urlStream = client.GetStreamAsync(url).Result;
                using var fileStream = new FileStream(saveTo, FileMode.Create);
                urlStream.CopyTo(fileStream);
            }
        }

        /// <inheritdoc/>
        public async Task DownloadHEVCAppxAsync(string fileName)
        {
            #pragma warning disable S6608 // Prefer indexing instead of "Enumerable" methods on types implementing "IList"

            var content = new List<KeyValuePair<string, string>>
            {
                new ("type", "url"), new ("url", "https://apps.microsoft.com/detail/9N4WGH0Z6VHQ"), new ("ring", "Retail"), new ("lang", "en-US"),
            };
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://store.rg-adguard.net/api/GetFiles");
            request.Content = new FormUrlEncodedContent(content);
            using var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            var appxLink = hrefPattern.Matches(result).Last().Value.Replace("<a href=\"", null).Replace("\" rel=\"noreferrer\">", null);
            using var stream = await client.GetStreamAsync(appxLink);
            using var file = File.Create(fileName);
            await stream.CopyToAsync(file);

            #pragma warning restore S6608 // Prefer indexing instead of "Enumerable" methods on types implementing "IList"
        }

        /// <inheritdoc/>
        public async Task<T> GetFromJsonAsync<T>(string url, double timeout)
            where T : class
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(timeout);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            var result = await Json.ToObjectAsync<T>(content);
            return result;
        }

        /// <inheritdoc/>
        public async Task OpenUrlAsync(string? url)
        {
            await Task.Run(() =>
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    Process.Start("explorer.exe", url);
                    App.Logger.LogOpenedUrl(url);
                }
            });
        }

        /// <inheritdoc/>
        public bool UrlIsAvailable(string url)
        {
            try
            {
                using var client = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = client.Send(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}

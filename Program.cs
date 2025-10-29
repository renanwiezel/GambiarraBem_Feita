using System;
using System.Net;

namespace GambiarraBem_Feita
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Adiciona serviços
            builder.Services.AddSingleton<Request>();

            var app = builder.Build();

            // Endpoint de health check (obrigatório para Render)
            app.MapGet("/", () => Results.Ok(new
            {
                status = "ok",
                message = "Scraper API está funcionando!",
                timestamp = DateTime.UtcNow
            }));

            // Endpoint para fazer scraping
            app.MapGet("/scrape", async (string url, Request request) =>
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    return Results.BadRequest(new { error = "URL é obrigatória. Use ?url=https://exemplo.com" });
                }

                try
                {
                    Console.WriteLine($"🔍 Scraping: {url}");
                    string html = await request.GetHtmlAsync(url);

                    return Results.Ok(new
                    {
                        success = true,
                        url,
                        htmlLength = html.Length,
                        preview = html.Length > 500 ? html[..500] : html,
                        fullHtml = html
                    });
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"❌ Erro HTTP: {ex.Message}");
                    return Results.Problem(detail: ex.Message, statusCode: 500);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erro: {ex.Message}");
                    return Results.Problem(detail: ex.Message, statusCode: 500);
                }
            });

            // Endpoint de teste com URL pré-definida
            app.MapGet("/test", async (Request request) =>
            {
                string url = "https://www.rcfinancasmobile.com/teste/index.php";
                try
                {
                    Console.WriteLine($"🧪 Teste com: {url}");
                    string html = await request.GetHtmlAsync(url);

                    return Results.Ok(new
                    {
                        success = true,
                        url,
                        htmlLength = html.Length,
                        html
                    });
                }
                catch (Exception ex)
                {
                    return Results.Problem(detail: ex.Message, statusCode: 500);
                }
            });

            // Configurar porta dinamicamente (Render usa variável PORT)
            var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
            app.Run($"http://0.0.0.0:{port}");
        }
    }

    public class Request
    {
        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip |
                                     DecompressionMethods.Deflate |
                                     DecompressionMethods.Brotli
            };

            var client = new HttpClient(handler);

            //Headers gerais e User Agent
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            client.DefaultRequestHeaders.Add("Referer", "https://www.uol.com.br/");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            return client;
        }

        public async Task<string> GetHtmlAsync(string url)
        {
            using var client = CreateClient();

            await Task.Delay(Random.Shared.Next(500, 1500));

            using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                var preview = body.Length > 500 ? body[..500] : body;
                throw new HttpRequestException($"Erro ao acessar {url}. Status Code: {resp.StatusCode}. Body: {preview}");
            }

            return await resp.Content.ReadAsStringAsync();
        }
    }
}
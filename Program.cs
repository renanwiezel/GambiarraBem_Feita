using System;
using System.Net;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace GambiarraBem_Feita
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Adiciona serviços
            builder.Services.AddSingleton<Request>();
            builder.Services.AddSingleton<HtmlParser>();
            
            // 🆕 ADICIONA CORS para permitir requisições do navegador
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // 🆕 HABILITA CORS (DEVE VIR ANTES DE UseStaticFiles)
            app.UseCors();

            // 🆕 HABILITA ARQUIVOS ESTÁTICOS (HTML, CSS, JS)
            app.UseDefaultFiles(); // Procura por index.html automaticamente
            app.UseStaticFiles();  // Serve arquivos da pasta wwwroot

            // API Endpoint - health check
            app.MapGet("/api", () => Results.Ok(new
            {
                status = "ok",
                message = "Scraper API está funcionando!",
                timestamp = DateTime.UtcNow
            }));

            // Endpoint para fazer scraping
            app.MapGet("/api/scrape", async (string url, Request request) =>
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

            // 🆕 Endpoint para PARSEAR NOTÍCIAS (URL FIXA)
            app.MapGet("/api/news", async (Request request, HtmlParser parser) =>
            {
                string url = "https://www.rcfinancasmobile.com/teste/index.php";

                try
                {
                    Console.WriteLine($"📰 Extraindo notícias de: {url}");
                    string html = await request.GetHtmlAsync(url);
                    var noticias = parser.ExtractNews(html, url);

                    return Results.Ok(new
                    {
                        success = true,
                        url,
                        totalNoticias = noticias.Count,
                        noticias
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
            app.MapGet("/api/test", async (Request request) =>
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

    public class HtmlParser
    {
        public List<Noticia> ExtractNews(string html, string baseUrl)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var noticias = new List<Noticia>();

            // Seleciona os links <a> dentro de divs com classe "thumbnails-item"
            var newsNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'thumbnails-item')]//a[@href]");

            if (newsNodes == null || newsNodes.Count == 0)
            {
                Console.WriteLine("⚠️ Nenhuma notícia encontrada com o padrão especificado!");
                return noticias;
            }

            Console.WriteLine($"✅ Encontradas {newsNodes.Count} notícias.");

            foreach (var linkNode in newsNodes)
            {
                try
                {
                    // Extrai o link do próprio nó <a>
                    var link = linkNode.GetAttributeValue("href", string.Empty);

                    // Converte URL relativa em absoluta
                    if (!string.IsNullOrWhiteSpace(link))
                    {
                        link = MakeAbsoluteUrl(baseUrl, link);
                    }

                    // Extrai o título (h3.thumb-title dentro do link)
                    var titleNode = linkNode.SelectSingleNode(".//h3[contains(@class, 'thumb-title')]");
                    var titulo = titleNode != null ? CleanText(titleNode.InnerText) : string.Empty;

                    // Só adiciona se tiver pelo menos o título
                    if (!string.IsNullOrWhiteSpace(titulo))
                    {
                        noticias.Add(new Noticia
                        {
                            Titulo = titulo,
                            Link = link
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Erro ao processar item: {ex.Message}");
                    // Continua processando os outros itens
                }
            }

            return noticias;
        }

        // Métodos auxiliares privados

        private string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove espaços extras, tabs e quebras de linha
            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }

        private string MakeAbsoluteUrl(string baseUrl, string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return string.Empty;

            if (Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
                return relativeUrl;

            if (Uri.TryCreate(new Uri(baseUrl), relativeUrl, out var absoluteUri))
                return absoluteUri.ToString();

            return relativeUrl;
        }
    }

    public class Noticia
    {
        public string Titulo { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
    }
}

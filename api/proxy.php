<?php
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');

error_reporting(E_ALL);
ini_set('display_errors', 0);

$url = $_GET['url'] ?? '';

if (empty($url)) {
    http_response_code(400);
    echo json_encode([
        'success' => false,
        'error' => 'Parâmetro "url" é obrigatório',
        'usage' => 'proxy.php?url=https://exemplo.com/...'
    ]);
    exit;
}

// 🔥 REMOVIDO: validação que só permitia UOL
// Agora aceita QUALQUER URL HTTP/HTTPS

// Valida apenas se é uma URL válida (segurança básica)
if (!filter_var($url, FILTER_VALIDATE_URL) || !preg_match('#^https?://#i', $url)) {
    http_response_code(400);
    echo json_encode([
        'success' => false,
        'error' => 'URL inválida. Deve começar com http:// ou https://'
    ]);
    exit;
}

$cookiesFile = __DIR__ . '/cookies_scraper.txt';
$ch = curl_init($url);

curl_setopt_array($ch, [
    CURLOPT_RETURNTRANSFER => true,
    CURLOPT_FOLLOWLOCATION => true,
    CURLOPT_MAXREDIRS      => 5,
    CURLOPT_TIMEOUT        => 30,
    CURLOPT_CONNECTTIMEOUT => 15,
    CURLOPT_SSL_VERIFYPEER => true,
    CURLOPT_SSL_VERIFYHOST => 2,
    CURLOPT_SSLVERSION     => CURL_SSLVERSION_TLSv1_2,
    CURLOPT_PROTOCOLS      => CURLPROTO_HTTPS | CURLPROTO_HTTP,
    CURLOPT_REDIR_PROTOCOLS=> CURLPROTO_HTTPS | CURLPROTO_HTTP,
    CURLOPT_HTTP_VERSION   => CURL_HTTP_VERSION_2TLS,
    CURLOPT_ENCODING       => '',
    CURLOPT_USERAGENT      => 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36',
    CURLOPT_COOKIEJAR      => $cookiesFile,
    CURLOPT_COOKIEFILE     => $cookiesFile,
    CURLOPT_HTTPHEADER     => [
        'Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8',
        'Accept-Language: pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7',
        'Accept-Encoding: gzip, deflate, br',
        'Connection: keep-alive',
        'Upgrade-Insecure-Requests: 1',
        'Referer: https://www.google.com/', // Referer genérico
        'Sec-Fetch-Site: cross-site',
        'Sec-Fetch-Mode: navigate',
        'Sec-Fetch-User: ?1',
        'Sec-Fetch-Dest: document',
        'Sec-Ch-Ua: "Google Chrome";v="131", "Chromium";v="131", "Not_A Brand";v="24"',
        'Sec-Ch-Ua-Mobile: ?0',
        'Sec-Ch-Ua-Platform: "macOS"',
        'Cache-Control: max-age=0',
    ],
]);

// Delay aleatório (evita detecção de bot)
usleep(random_int(500000, 1500000));

$html = curl_exec($ch);
$httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
$effectiveUrl = curl_getinfo($ch, CURLINFO_EFFECTIVE_URL);
$error = curl_error($ch);
curl_close($ch);

if ($html === false || $httpCode !== 200) {
    http_response_code($httpCode ?: 500);
    echo json_encode([
        'success' => false,
        'httpCode' => $httpCode,
        'error' => $error ?: 'Falha ao acessar URL',
        'url' => $url,
        'effectiveUrl' => $effectiveUrl
    ]);
} else {
    http_response_code(200);
    echo json_encode([
        'success' => true,
        'httpCode' => $httpCode,
        'url' => $url,
        'effectiveUrl' => $effectiveUrl,
        'htmlLength' => strlen($html),
        'html' => $html
    ]);
}
?>
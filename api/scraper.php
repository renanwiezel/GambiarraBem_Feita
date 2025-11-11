<?php
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');

error_reporting(0);
ini_set('display_errors', 0);

$url = $_GET['url'] ?? '';

if (empty($url)) {
    http_response_code(400);
    die(json_encode(['success' => false, 'error' => 'URL obrigatória']));
}

if (!filter_var($url, FILTER_VALIDATE_URL)) {
    http_response_code(400);
    die(json_encode(['success' => false, 'error' => 'URL inválida']));
}

$ch = curl_init($url);
curl_setopt_array($ch, [
    CURLOPT_RETURNTRANSFER => true,
    CURLOPT_FOLLOWLOCATION => true,
    CURLOPT_MAXREDIRS => 5,
    CURLOPT_TIMEOUT => 30,
    CURLOPT_SSL_VERIFYPEER => false,
    CURLOPT_SSL_VERIFYHOST => 0,
    CURLOPT_ENCODING => '',
    CURLOPT_USERAGENT => 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36',
    CURLOPT_HTTPHEADER => [
        'Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
        'Accept-Language: pt-BR,pt;q=0.9',
        'Referer: https://www.google.com/',
    ],
]);

usleep(random_int(300000, 800000));

$html = curl_exec($ch);
$code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
$error = curl_error($ch);
curl_close($ch);

http_response_code($html && $code == 200 ? 200 : 500);
echo json_encode([
    'success' => $html && $code == 200,
    'httpCode' => $code,
    'url' => $url,
    'htmlLength' => $html ? strlen($html) : 0,
    'html' => $html ?: '',
    'error' => $error ?: null
]);
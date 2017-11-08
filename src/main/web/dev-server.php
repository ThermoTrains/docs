<?php
$requestUri = $_SERVER["REQUEST_URI"];

if ($requestUri === '/') {
  include(__DIR__ . '/index.html');
  return;
}

if ($requestUri === '/api/artifact') {
  include(__DIR__ . '/api/artifact/index.php');
  return;
}

if (file_exists(__DIR__ . '/' . $requestUri)) {
  readfile(__DIR__ . '/' . $requestUri);
  return;
}

echo '404 - Not Found';

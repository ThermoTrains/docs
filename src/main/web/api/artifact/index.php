<?php

$artifactfolder = __DIR__ . '/../../artifacts';
$files = array_diff(scandir($artifactfolder), array('..', '.', '.gitkeep'));
arsort($files);

$grouped = [];

foreach ($files as $file) {
  $atpos = strrpos($file, '@');
  $date = substr($file, 0, $atpos);
  $time = substr($file, $atpos + 1, 8);
  $grouped[$date][$time][] = [
    'filename' => 'artifacts/' . $file,
    'name' => substr($file, $atpos + 10),
    'size' => filesize($artifactfolder . '/' . $file)
  ];
  $filepath = 'artifacts/' . $file;
}

header('Content-Type: application/json');
echo json_encode($grouped, JSON_UNESCAPED_SLASHES);

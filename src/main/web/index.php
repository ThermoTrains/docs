<?php

$files = array_diff(scandir('artifacts'), array('..', '.'));;
arsort($files);

foreach($files as $file){
  $filepath = 'artifacts/'.$file;
  echo '<a href="'.$filepath.'">'.$file.' '.getFileSize($filepath).'</a><br>';
}


function getFileSize($file) {
  $bytes = filesize($file);

  if ($bytes < 1024){
    return formatFileSize($bytes, ' Bytes');
  }

  $kilobytes = $bytes / 1024;

  if ($kilobytes < 1024){
    return formatFileSize($kilobytes, ' KB');
  }

  $megabytes = $kilobytes / 1024;

  return formatFileSize($megabytes, ' MB');
}

function formatFileSize($number, $unit){
  return strval(round($number, 2)) . ' ' . $unit;
}

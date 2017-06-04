const exec = require('child_process').exec;

exec(getCommandLine() + ' ' + process.argv[2]);

function getCommandLine() {
  switch (process.platform) {
    case 'darwin':
      return 'open';
    case 'win32':
    case 'win64':
      return 'start';
    default:
      return 'xdg-open';
  }
}

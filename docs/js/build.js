const nunjucks = require('nunjucks');
const fs = require('fs');

const template = nunjucks.render(__dirname + '/../index.html');
const buildDir = __dirname + '/../build';

if (!fs.existsSync(buildDir)) {
    fs.mkdirSync(buildDir);
}

fs.writeFile(buildDir + '/index.html', template, function (err) {
    if (err) {
        return console.log(err);
    }

    console.log("HTML compiled and saved to build/index.html");
}); 

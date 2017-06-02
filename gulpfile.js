const fs = require('fs');

const gulp = require('gulp');
const gutil = require('gulp-util');
const gls = require('gulp-live-server');
const nunjucksRender = require('gulp-nunjucks-render');
const sass = require('gulp-sass');

const runSequence = require('run-sequence');
const source = require('vinyl-source-stream');
const buffer = require('vinyl-buffer');
const del = require('del');
const moment = require('moment');

const pkg = JSON.parse(fs.readFileSync('./package.json'));

gulp.task('default', ['root', 'docs', 'slides']);

gulp.task('root', () => gulp.src('index.html').pipe(gulp.dest('build')));
gulp.task('docs', () => runSequence('clean:docs', ['docs:nunjucks', 'docs:js', 'docs:style', 'docs:images', 'docs:deps']));
gulp.task('slides', () => runSequence('clean:slides', ['slides:nunjucks', 'slides:js', 'slides:style', 'slides:images', 'slides:deps', 'slides:demos']));

gulp.task('clean', ['clean:docs', 'clean:slides']);
gulp.task('clean:docs', () => del(['build/docs/**/*']));
gulp.task('clean:slides', () => del(['build/slides/**/*']));

gulp.task('docs:nunjucks', function () {
  gulp.src('docs/index.html')
    .pipe(nunjucksRender({
      path: ['docs/'],
      data: {
        documentVersion: pkg.version,
        buildDate: moment().format('DD.MM.YYYY')
      }
    }))
    .on('error', (error) => {
      gutil.log(gutil.colors.red('Error (' + error.plugin + '): ' + error.message));
      this.emit('end');
    })
    .pipe(gulp.dest('./build/docs'));
});

gulp.task('docs:js', () => {
  gulp.src('docs/js/**/*.js')
    .pipe(gulp.dest('build/docs'));
});

gulp.task('docs:style', () => {
  gulp.src('docs/styles/main.scss')
    .pipe(sass().on('error', sass.logError))
    .pipe(gulp.dest('build/docs'));
});

gulp.task('docs:deps', () => {
  gulp.src('{node_modules/jquery/dist/jquery.min.js,node_modules/normalize.css/normalize.css}')
    .pipe(gulp.dest('build/docs'));
});

gulp.task('docs:images', () => {
  gulp.src('docs/images/**/*.{png,jpg,gif,svg}')
    .pipe(gulp.dest('build/docs/images'));
});

gulp.task('serve:docs', ['docs'], () => {
  const server = gls.static('build/docs', 3000);
  server.start();

  // Restart the server when file changes
  gulp.watch('docs/styles/**/*.scss', (file) => {
    runSequence('docs:style', () => server.notify.apply(server, [file]));
  });
  gulp.watch('docs/**/*.html', (file) => {
    runSequence('docs:nunjucks', () => server.notify.apply(server, [file]));
  });
  gulp.watch('docs/js/**/*.js', (file) => {
    runSequence('docs:js', () => server.notify.apply(server, [file]));
  });
  gulp.watch('docs/images/**/*.{png,jpg,gif,svg}', (file) => {
    runSequence('docs:images', () => server.notify.apply(server, [file]));
  });
});

gulp.task('slides:nunjucks', function () {
  gulp.src('slides/index.html')
    .pipe(nunjucksRender({path: ['slides/']}))
    .on('error', (error) => {
      gutil.log(gutil.colors.red('Error (' + error.plugin + '): ' + error.message));
      this.emit('end');
    })
    .pipe(gulp.dest('./build/slides'));
});

gulp.task('slides:js', () => {
  gulp.src('slides/**/*.js')
    .pipe(gulp.dest('build/slides'));
});

gulp.task('slides:style', () => {
  gulp.src('slides/styles/main.scss')
    .pipe(sass().on('error', sass.logError))
    .pipe(gulp.dest('build/slides'));
});

gulp.task('slides:deps', ['slides:deps:styles'], () => {
  gulp.src('{node_modules/reveal.js/lib/js/head.min.js,' +
    'node_modules/reveal.js/plugin/**/*.js,' +
    'node_modules/reveal.js/plugin/**/*.html,' +
    'node_modules/reveal.js/js/reveal.js,' +
    'node_modules/reveal.js/**/*.{eot,ttf,woff}}')
    .pipe(gulp.dest('build/slides'));
});

gulp.task('slides:deps:styles', () => {
  gulp.src('{node_modules/reveal.js/css/reveal.scss,node_modules/reveal.js/**/*.css}')
    .pipe(sass().on('error', sass.logError))
    .pipe(gulp.dest('build/slides'));
});

gulp.task('slides:images', () => {
  gulp.src('slides/images/**/*.{png,jpg,gif,svg,mp4}')
    .pipe(gulp.dest('build/slides/images'));
});

gulp.task('slides:demos', () => {
  gulp.src('slides/demos/**/*.*')
    .pipe(gulp.dest('build/slides/demos'));
});

gulp.task('serve:slides', ['slides'], () => {
  const server = gls.static('build/slides', 3000);
  server.start();

  // Restart the server when file changes
  gulp.watch('slides/styles/**/*.scss', (file) => {
    runSequence('slides:style', () => server.notify.apply(server, [file]));
  });
  gulp.watch('slides/**/*.html', (file) => {
    runSequence('slides:nunjucks', () => setTimeout(() => server.notify.apply(server, [file]), 100));
  });
  gulp.watch('slides/js/**/*.js', (file) => {
    runSequence('slides:js', () => server.notify.apply(server, [file]));
  });
  gulp.watch('slides/images/**/*.{png,jpg,gif,svg,mp4}', (file) => {
    runSequence('slides:images', () => server.notify.apply(server, [file]));
  });
  gulp.watch('slides/demos/**/*.*', (file) => {
    runSequence('slides:demos', () => server.notify.apply(server, [file]));
  });
});

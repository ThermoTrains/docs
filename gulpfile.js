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

gulp.task('default', ['root', 'docs']);

gulp.task('root', () => gulp.src('index.html').pipe(gulp.dest('build')));
gulp.task('docs', () => runSequence('clean:docs', ['docs:nunjucks', 'docs:js', 'docs:style', 'docs:images', 'docs:deps']));

gulp.task('clean', ['clean:docs']);
gulp.task('clean:docs', () => del(['build/**/*']));

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
    .pipe(gulp.dest('./build'));
});

gulp.task('docs:js', () => {
  gulp.src('docs/js/**/*.js')
    .pipe(gulp.dest('build'));
});

gulp.task('docs:style', () => {
  gulp.src('docs/styles/main.scss')
    .pipe(sass().on('error', sass.logError))
    .pipe(gulp.dest('build'));
});

gulp.task('docs:deps', () => {
  gulp.src('{node_modules/jquery/dist/jquery.min.js,node_modules/normalize.css/normalize.css}')
    .pipe(gulp.dest('build'));
});

gulp.task('docs:images', () => {
  gulp.src('docs/images/**/*.{png,jpg,gif,svg}')
    .pipe(gulp.dest('build/images'));
});

gulp.task('serve:docs', ['docs'], () => {
  const server = gls.static('build', 3003);
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

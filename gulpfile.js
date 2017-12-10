const fs = require('fs');

const gulp = require('gulp');
const gutil = require('gulp-util');
const gls = require('gulp-live-server');
const nunjucksRender = require('gulp-nunjucks-render');
const sass = require('gulp-sass');

const runSequence = require('run-sequence');
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

let server;
gulp.task('watch', () => {
  server = gls.static('build', 3003);
  server.start();

  gulp.watch('docs/styles/**/*.scss', ['docs:style']);
  gulp.watch('docs/**/*.html', ['docs:nunjucks']);
  gulp.watch('docs/js/**/*.js', ['docs:js']);
  gulp.watch('docs/images/**/*.{png,jpg,gif,svg}', ['docs:images']);
});

gulp.task('livereload', () => {
  // Refresh browser when artifacts change
  // Activate after a timeout because sometimes there are conflicts from the build task
  setTimeout(() => gulp.watch('build/**/*', (file) => server.notify.apply(server, [file])), 1000);
});

gulp.task('serve:docs', () => runSequence('docs', ['watch', 'livereload']));


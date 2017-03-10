const gulp = require('gulp');
const gutil = require('gulp-util');
const runSequence = require('run-sequence');
const nunjucksRender = require('gulp-nunjucks-render');
const gls = require('gulp-live-server');
const source = require('vinyl-source-stream');
const buffer = require('vinyl-buffer');
const del = require('del');

gulp.task('default', function () {
    runSequence('clean', ['nunjucks', 'js', 'styles', 'images']);
});

gulp.task('clean', function () {
    return del(['build/**/*']);
});

gulp.task('nunjucks', function () {
    //nunjucksRender.nunjucks.configure(['app/templates/'], {watch: false});
    return gulp.src('index.html')
        .pipe(nunjucksRender())
        .on('error', function (error) {
            gutil.log(gutil.colors.red('Error (' + error.plugin + '): ' + error.message));
            this.emit('end');
        })
        .pipe(gulp.dest('./build'));
});

gulp.task('js', function () {
    gulp.src('{js/**/*.js,node_modules/jquery/dist/jquery.min.js}')
        .pipe(gulp.dest('build/'));
});

gulp.task('styles', function () {
    gulp.src('{styles/**/*.css,node_modules/normalize.css/normalize.css}')
        .pipe(gulp.dest('build/'));
});

gulp.task('images', function () {
    gulp.src('images/**/*.{png,jpg,gif,svg}')
        .pipe(gulp.dest('build/images'));
});

gulp.task('serve', ['default'], function () {
    var server = gls.static('build', 3000);
    server.start();

    // Restart the server when file changes
    gulp.watch(['styles/**/*.css'], function (file) {
        runSequence('sass', function () {
            server.notify.apply(server, [file]);
        });
    });
    gulp.watch(['**/*.html'], function (file) {
        runSequence('nunjucks', function () {
            server.notify.apply(server, [file]);
        });
    });
    gulp.watch(['js/**/*.js'], function (file) {
        runSequence('js', function () {
            server.notify.apply(server, [file]);
        });
    });
    gulp.watch(['images/**/*.{png,jpg,gif,svg}'], function (file) {
        runSequence('images', function () {
            server.notify.apply(server, [file]);
        });
    });
});

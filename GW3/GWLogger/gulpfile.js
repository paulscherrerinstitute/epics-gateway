/// <binding ProjectOpened='default' />
"use strict";

var gulp = require('gulp');
var ts = require("gulp-typescript");
var less = require("gulp-less");
var sourcemaps = require('gulp-sourcemaps');
var plumber = require('gulp-plumber');
var uglify = require('gulp-uglify');

// Config

var typescriptFileMatchers = [
    "./Frontend/**/*.ts",
    "./Scripts/typings/**/*.d.ts"
];

var tsCompiler = ts.createProject({
    out: "main.js",
    module: "system",
    target: "es5",
    lib: [
        "dom",
        "es2015"
    ],
    experimentalDecorators: true,
    allowJs: true,
});

var polyfills = [
    "./node_modules/es6-promise/dist/es6-promise.auto.min.js",
    "./node_modules/es6-promise/dist/es6-promise.min.js"
];

var lessFileMatchers = [
    "./Less/main.less"
];

// Gulp tasks

function buildTs(cb) {
    return gulp.src(typescriptFileMatchers.concat(polyfills))
        .pipe(plumber())
        .pipe(sourcemaps.init({}))
        .pipe(sourcemaps.identityMap())
        .pipe(tsCompiler(ts.reporter.defaultReporter()))
        //.pipe(uglify({}))
        .pipe(sourcemaps.write("."))
        .pipe(gulp.dest("."));
}

function buildLess(cb) {
    return gulp.src(lessFileMatchers)
        .pipe(plumber())
        .pipe(less({}))
        .pipe(gulp.dest("./Less"));
}

function startTsWatcher(cb) {
    return gulp.watch(typescriptFileMatchers, buildTs);
}

function startLessWatcher(cb) {
    return gulp.watch(lessFileMatchers, buildLess);
}

gulp.task("default", gulp.series(buildTs, buildLess, gulp.parallel(startTsWatcher, startLessWatcher)));
gulp.task("build:ts", buildTs);
gulp.task("build:less", buildLess);
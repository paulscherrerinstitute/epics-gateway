"use strict";

var gulp = require('gulp');
var ts = require("gulp-typescript");
var less = require("gulp-less");
var sourcemaps = require('gulp-sourcemaps');

// Config

var typescriptOutDir = ".";
var typescriptOutFile = "main.js";

var typescriptFileMatchers = [
    "./Frontend/**/*.ts",
    "./Scripts/typings/**/*.d.ts"
];

var tsCompiler = ts.createProject({
    out: typescriptOutFile,
    module: "system",
    target: "es5",
    experimentalDecorators: true
});

var lessOutDir = "./Less";

var lessFileMatchers = [
    "./Less/main.less"
];


// Gulp tasks

function buildTs(cb) {
    return gulp.src(typescriptFileMatchers)
        .pipe(sourcemaps.init())
        .pipe(sourcemaps.identityMap())
        .pipe(tsCompiler(ts.reporter.defaultReporter()))
        .pipe(sourcemaps.write())
        .pipe(gulp.dest(typescriptOutDir));
}

function buildLess(cb) {
    return gulp.src(lessFileMatchers)
        .pipe(less({}))
        .pipe(gulp.dest(lessOutDir));
}

gulp.task("default", gulp.series(buildTs, buildLess));
gulp.task("build:ts", buildTs);
gulp.task("build:less", buildLess);

// Gulp watchers

gulp.watch(typescriptFileMatchers, buildTs);
gulp.watch(lessFileMatchers, buildLess);

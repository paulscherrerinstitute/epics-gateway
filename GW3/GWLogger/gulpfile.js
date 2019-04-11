/// <binding Clean='clean:all' ProjectOpened='default' />
"use strict";
/*
This file is the main entry point for defining Gulp tasks and using Gulp plugins.
Click here to learn more. https://go.microsoft.com/fwlink/?LinkId=518007
*/

var gulp = require('gulp');
var rimraf = require("rimraf");
var typescript = require("gulp-typescript");
var less = require("gulp-less");
var util = require("gulp-util");
var sourcemaps = require('gulp-sourcemaps');

function CompileFrontend()
{
    process.chdir(__dirname);

    /*tsMain = typescript.createProject({
        out: "main.js",
        module: "system",
        target: "es5",
        experimentalDecorators: true,
        sourceMap: true
    });*/

    return gulp.src(['./Frontend/**/*.ts', './Scripts/typings/**/*.d.ts', './node_modules/es6-promise/dist/es6-promise.auto.min.js', './node_modules/es6-promise/dist/es6-promise.min.js'])
        .pipe(sourcemaps.init())
        .pipe(sourcemaps.identityMap())
        .pipe(tsMain())
        .pipe(sourcemaps.write(".", {
            includeContent: false, addComment: true
        }))
        .pipe(gulp.dest("."))
        .on('error', swallowError)
        .on('finish', function ()
        {
            util.log(util.colors.cyan("Frontend compilation complete"));
        });
}

function clean(path)
{
    var fs = require("fs");
    if (fs.existsSync(path))
        fs.unlinkSync(path);
}

function swallowError(error)
{
    // If you want details of the error in the console
    console.log("ERROR!");
    console.log(error.toString());
    this.emit('end');
}

var tsMain = typescript.createProject({
    out: "main.js",
    module: "system",
    target: "es5",
    experimentalDecorators: true,
    sourceMap: true,
    allowJs: true});

gulp.task("watcher", function ()
{
    process.chdir(__dirname);
    console.log("I will watch for you all the TS and LESS files and compile them as needed.");
    console.log(" ");
    //gulp.watch("Frontend/**/*.ts", gulp.series(["compile:frontend"]));
    gulp.watch("Frontend/**/*.ts", CompileFrontend);
    gulp.watch("**/*.less", gulp.series(["compile:less"]));
});

gulp.task("default", gulp.series(["watcher"], function ()
{
}));

gulp.task("compile:less", function ()
{
    process.chdir(__dirname);
    return gulp.src(['./Less/main.less']).on('error', swallowError).on('finish', function ()
    {
        util.log(util.colors.cyan("Less compilation complete"));
    }).pipe(less({})).pipe(gulp.dest('Less'));
});

gulp.task("clean:css", function (cb)
{
    process.chdir(__dirname);
    clean('Less/main.css');
    cb();
});

gulp.task("clean:js", function (cb)
{
    process.chdir(__dirname);
    clean('./main.js');
    clean('./main.js.map');
    cb();
});

gulp.task('compile:frontend', CompileFrontend);

gulp.task("compile:all", gulp.series(["compile:less", "compile:frontend"]));
gulp.task("clean:all", gulp.series(["clean:css", "clean:js"]));

/// <binding BeforeBuild='compile:all' Clean='clean:all' ProjectOpened='default' />
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
    sourceMap: true
});

gulp.task("default", function ()
{
    process.chdir(__dirname);
    console.log("I will watch for you all the TS and LESS files and compile them as needed.");
    console.log(" ");
    gulp.watch("Frontend/**/*.ts", ["compile:frontend"]);
    gulp.watch("**/*.less", ["compile:less"]);
});

gulp.task("compile:less", function ()
{
    process.chdir(__dirname);
    gulp.src(['./Less/main.less']).on('error', swallowError).on('finish', function ()
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

gulp.task("compile:all", ["compile:less", "compile:frontend"]);
gulp.task("clean:all", ["clean:css", "clean:js"]);

gulp.task('compile:frontend', function ()
{
    process.chdir(__dirname);

    gulp.src(['./Frontend/**/*.ts'])
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
});
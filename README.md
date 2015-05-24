Cyotek Spriter Quick Pack
=========================

A bare bones tool that you can use to pack all images in a given directory into a single graphic, and optionally generate the sufficient CSS required to use the sprite sheet in HTML pages.

If you need more functionality than this tool offers, you can always check out [Cyotek Spriter](http://www.cyotek.com/cyotek-spriter) which has a great deal more functionality. 

### Options

The following list details the arguments that `sprpack.exe` accepts. All are optional.

* `path` - specifies the path to process. Defaults to the current directory
* `mask` - comma separated list of file masks to search. Defaults to `*.png`
* `out` - the file name of the sprite sheet graphic. Defaults to `sheet.png`
* `css` - the file name where the CSS will be written. If not specified, CSS will not be generated
* `class` - the base CSS class name. Ignored if `/css` is not set

As you can see, it is a very simple affair!

> **Note!** The tool will overwrite output files without prompting
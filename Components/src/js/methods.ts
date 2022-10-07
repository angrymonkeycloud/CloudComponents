//import { CoreSideBar } from "./sidebar";
//import { CoreMain } from "./main";

//$(window).resize(function () {

//    if ($(this).width() !== CoreSideBar.WindowsWidth) {

//        CoreSideBar.WindowsWidth = $(this).width();

//        if (CoreSideBar.isOpened)
//            CoreSideBar.close();
//    }
//});

//$(window).on("load", function () {

//    if (CoreSideBar.isOpened)
//        CoreSideBar.close();
//});

//// Dock Links

//$(document).on('click', 'a[href*="#"]:not([href="#"])', function () {

//    if (location.pathname.replace(/^\//, '') === this.pathname.replace(/^\//, '') && location.hostname === this.hostname) {

//        let target = $(this.hash);
//        target = target.length ? target : $('[name=' + this.hash.slice(1) + ']');

//        if (target.length)
//            CoreMain.scrollTo(target[0], 200);
//        return false;
//    }
//});
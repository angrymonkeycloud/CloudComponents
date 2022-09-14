var exports = {};
var loadImageObserver = new IntersectionObserver(function (entries) {
    entries.forEach(function (entry) {
        if (entry.isIntersecting && entry.target.getAttribute('data-src') !== undefined) {
            var src = entry.target.getAttribute('data-src');
            entry.target.setAttribute('src', src);
            entry.target.removeAttribute('data-src');
            loadImageObserver.unobserve(entry.target);
        }
    });
});
var CoreMain = /** @class */ (function () {
    function CoreMain() {
    }
    CoreMain.reinitializeLazyLoadingImages = function () {
        document.querySelectorAll('img[data-src]').forEach(function (element) {
            loadImageObserver.unobserve(element);
            loadImageObserver.observe(element);
        });
    };
    CoreMain.preventScrolling = function () {
        var width = document.body.offsetWidth;
        document.querySelector('html').classList.add('_noscroll');
        var scrollWidth = document.body.offsetWidth - width;
        if (scrollWidth > 0) {
            document.body.style.marginRight = scrollWidth + 'px';
            var header = document.querySelector('body > header');
            if (header !== null)
                header.style.right = scrollWidth + 'px';
        }
    };
    CoreMain.allowScrolling = function () {
        document.querySelector('html').classList.remove('_noscroll');
        document.body.style.marginRight = null;
        var header = document.querySelector('body > header');
        if (header !== null)
            header.style.right = null;
    };
    CoreMain.scrollTo = function (scroll, scrollDuration) {
        if (typeof scroll === 'object')
            scroll = window.pageYOffset + scroll.getBoundingClientRect().y;
        var scrollTo = scroll;
        // Declarations
        var cosParameter = (window.pageYOffset - scrollTo) / 2;
        var scrollCount = 0;
        var oldTimestamp = window.performance.now();
        function step(newTimestamp) {
            var tsDiff = newTimestamp - oldTimestamp;
            // Performance.now() polyfill loads late so passed-in timestamp is a larger offset
            // on the first go-through than we want so I'm adjusting the difference down here.
            // Regardless, we would rather have a slightly slower animation than a big jump so a good
            // safeguard, even if we're not using the polyfill.
            if (tsDiff > 100)
                tsDiff = 30;
            scrollCount += Math.PI / (scrollDuration / tsDiff);
            // As soon as we cross over Pi, we're about where we need to be
            if (scrollCount >= Math.PI)
                return;
            var moveStep = Math.round(scrollTo + cosParameter + cosParameter * Math.cos(scrollCount));
            window.scrollTo(0, moveStep);
            oldTimestamp = newTimestamp;
            window.requestAnimationFrame(step);
        }
        window.requestAnimationFrame(step);
    };
    return CoreMain;
}());
{ CoreMain };
CoreMain.reinitializeLazyLoadingImages();


var exports = {};
// Dock Links
document.querySelectorAll('a[href*="#"]:not([href="#"])').forEach(function (item) {
    item.addEventListener('click', function () {
        if (location.pathname.replace(/^\//, '') === this.pathname.replace(/^\//, '') && location.hostname === this.hostname) {
            var target = $(this.hash);
            target = target.length ? target : $('[name=' + this.hash.slice(1) + ']');
            if (target.length)
                CoreMain.scrollTo(target[0], 200);
            return false;
        }
    });
});


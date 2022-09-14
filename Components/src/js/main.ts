
const loadImageObserver = new IntersectionObserver((entries): void => {

    entries.forEach(entry => {

        if (entry.isIntersecting && entry.target.getAttribute('data-src') !== undefined) {

            const src = entry.target.getAttribute('data-src');

            entry.target.setAttribute('src', src);
            entry.target.removeAttribute('data-src');

            loadImageObserver.unobserve(entry.target);
        }
    });

});

export class CoreMain {

    static reinitializeLazyLoadingImages() {

        document.querySelectorAll('img[data-src]').forEach((element) => {

            loadImageObserver.unobserve(element);
            loadImageObserver.observe(element);
        });
    }

    static preventScrolling(): void {

        const width = document.body.offsetWidth;
        $('html').addClass('_noscroll');
        const scrollWidth = document.body.offsetWidth - width;

        if (scrollWidth > 0) {
            document.body.style.marginRight = scrollWidth + 'px';

            const header: HTMLElement = document.querySelector('body > header');

            if (header !== null)
                header.style.right = scrollWidth + 'px';
        }
    }

    static allowScrolling(): void {

        $('html').removeClass('_noscroll');
        document.body.style.marginRight = null;

        const header: HTMLElement = document.querySelector('body > header');
        if (header !== null)
            header.style.right = null;
    }

    static scrollTo(scroll: Element | number, scrollDuration: number): void {

        if (typeof scroll === 'object')
            scroll = window.pageYOffset + (scroll as Element).getBoundingClientRect().y;

        const scrollTo: number = scroll as number;
        
        // Declarations

    const cosParameter = (window.pageYOffset - scrollTo) / 2;
    let scrollCount = 0;
    let oldTimestamp = window.performance.now();

        function step(newTimestamp: number): void {

            let tsDiff = newTimestamp - oldTimestamp;

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

            const moveStep = Math.round(scrollTo + cosParameter + cosParameter * Math.cos(scrollCount));
            window.scrollTo(0, moveStep);
            oldTimestamp = newTimestamp;
            window.requestAnimationFrame(step);
        }

        window.requestAnimationFrame(step);
    }
}

CoreMain.reinitializeLazyLoadingImages();